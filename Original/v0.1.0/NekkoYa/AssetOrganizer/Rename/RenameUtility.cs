using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal sealed class RenamePlan
    {
        public UnityEngine.Object Asset;
        public string OldPath;
        public string OldName;
        public string NewName;
        public string Extension;
        public string NewPath;
    }

    internal sealed class RenameResult
    {
        public int SuccessCount;
        public readonly List<string> Errors = new List<string>();
    }

    internal static class RenameUtility
    {
        private static readonly Regex NumberToken = new Regex(@"\{(0+)\}", RegexOptions.Compiled);

        public static bool UsesNameToken(string template)
        {
            return !string.IsNullOrEmpty(template) && template.Contains("{Name}");
        }

        public static List<RenamePlan> BuildPlans(IReadOnlyList<RenameItem> items, string template,
            int startNumber, List<string> errors)
        {
            var plans = new List<RenamePlan>();
            if (string.IsNullOrWhiteSpace(template))
            {
                errors.Add(L("Template is empty.", "テンプレートが空です。"));
                return plans;
            }

            if (!NumberToken.IsMatch(template))
                errors.Add(L("Template must contain a number token such as {00}.",
                    "テンプレートに {00} などの連番Tokenを含めてください。"));

            bool usesName = UsesNameToken(template);
            var assets = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < items.Count; i++)
            {
                RenameItem item = items[i];
                if (item == null || item.Asset == null)
                {
                    errors.Add(L("Asset row " + (i + 1) + " is empty.",
                        "アセットの" + (i + 1) + "行目が空です。"));
                    continue;
                }

                if (!assets.Add(item.Asset))
                {
                    errors.Add(L("Asset row " + (i + 1) + " is duplicated.",
                        "アセットの" + (i + 1) + "行目が重複しています。"));
                    continue;
                }

                string oldPath = AssetDatabase.GetAssetPath(item.Asset);
                if (string.IsNullOrEmpty(oldPath) || AssetDatabase.IsValidFolder(oldPath))
                {
                    errors.Add(L("Row " + (i + 1) + " is not a renameable Project asset.",
                        (i + 1) + "行目はリネーム可能なProjectアセットではありません。"));
                    continue;
                }

                string newName = ExpandTemplate(template, startNumber + i, item.CustomName);
                string nameError = AssetNameValidator.Validate(newName,
                    L("New name at row " + (i + 1), (i + 1) + "行目の変更後の名前"));
                if (nameError != null)
                    errors.Add(nameError);
                if (usesName && string.IsNullOrWhiteSpace(item.CustomName))
                    errors.Add(L("{Name} at row " + (i + 1) + " is empty.",
                        (i + 1) + "行目の{Name}が空です。"));

                string extension = Path.GetExtension(oldPath);
                string directory = GetAssetDirectory(oldPath);
                plans.Add(new RenamePlan
                {
                    Asset = item.Asset,
                    OldPath = oldPath,
                    OldName = Path.GetFileNameWithoutExtension(oldPath),
                    NewName = newName,
                    Extension = extension,
                    NewPath = directory + "/" + newName + extension
                });
            }

            ValidateDestinations(plans, errors);
            return plans;
        }

        public static string ExpandTemplate(string template, int number, string customName)
        {
            string expanded = NumberToken.Replace(template ?? string.Empty,
                match => number.ToString(new string('0', match.Groups[1].Value.Length)));
            return expanded.Replace("{Name}", customName ?? string.Empty);
        }

        public static void ValidateDestinations(IReadOnlyList<RenamePlan> plans, List<string> errors)
        {
            var sourcePaths = new HashSet<string>(plans.Select(plan => plan.OldPath), StringComparer.OrdinalIgnoreCase);
            var destinationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RenamePlan plan in plans)
            {
                if (!destinationPaths.Add(plan.NewPath))
                    errors.Add(L("Multiple assets would become '" + plan.NewPath + "'.",
                        "複数のアセットが同じパス「" + plan.NewPath + "」になります。"));

                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(plan.NewPath);
                if (existing != null && !sourcePaths.Contains(plan.NewPath))
                    errors.Add(L("An asset already exists at '" + plan.NewPath + "'.",
                        "変更先「" + plan.NewPath + "」には既にアセットがあります。"));
            }
        }

        public static RenameResult Execute(IReadOnlyList<RenamePlan> plans)
        {
            var result = new RenameResult();
            if (plans.Count == 0)
            {
                result.Errors.Add(L("There are no assets to rename.", "リネームするアセットがありません。"));
                return result;
            }

            var originalNames = plans.ToDictionary(plan => plan.Asset, plan => plan.OldName);
            try
            {
                MoveAllToTemporaryNames(plans.Select(plan => plan.Asset));
                foreach (RenamePlan plan in plans)
                {
                    string currentPath = AssetDatabase.GetAssetPath(plan.Asset);
                    string error = AssetDatabase.RenameAsset(currentPath, plan.NewName);
                    if (!string.IsNullOrEmpty(error))
                        throw new InvalidOperationException(plan.OldPath + " -> " + plan.NewPath + ": " + error);
                    result.SuccessCount++;
                    Debug.Log("[NekkoYa Asset Organizer] Renamed: " + plan.OldPath + " -> " + plan.NewPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                result.Errors.Add(exception.Message);
                result.SuccessCount = 0;
                RollBack(plans.Select(plan => plan.Asset), originalNames, result.Errors);
            }

            return result;
        }

        public static List<RenamePlan> BuildRevertPlans(IReadOnlyList<RenamePlan> history, List<string> errors)
        {
            var plans = new List<RenamePlan>();
            foreach (RenamePlan oldPlan in history)
            {
                if (oldPlan.Asset == null)
                {
                    errors.Add(L("A previously renamed asset no longer exists.",
                        "直前にリネームしたアセットが存在しません。"));
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(oldPlan.Asset);
                string currentName = Path.GetFileNameWithoutExtension(path);
                if (!string.Equals(currentName, oldPlan.NewName, StringComparison.Ordinal))
                {
                    errors.Add(L("Cannot revert '" + path + "' because it was renamed again.",
                        "「" + path + "」は再度名前が変更されているため元に戻せません。"));
                    continue;
                }

                plans.Add(new RenamePlan
                {
                    Asset = oldPlan.Asset,
                    OldPath = path,
                    OldName = currentName,
                    NewName = oldPlan.OldName,
                    Extension = Path.GetExtension(path),
                    NewPath = GetAssetDirectory(path) + "/" + oldPlan.OldName + Path.GetExtension(path)
                });
            }

            ValidateDestinations(plans, errors);
            return plans;
        }

        private static void MoveAllToTemporaryNames(IEnumerable<UnityEngine.Object> assets)
        {
            foreach (UnityEngine.Object asset in assets)
            {
                string currentPath = AssetDatabase.GetAssetPath(asset);
                string extension = Path.GetExtension(currentPath);
                string directory = GetAssetDirectory(currentPath);
                string candidate = AssetDatabase.GenerateUniqueAssetPath(
                    directory + "/__NekkoYaRenameTemp_" + Guid.NewGuid().ToString("N") + extension);
                string temporaryName = Path.GetFileNameWithoutExtension(candidate);
                string error = AssetDatabase.RenameAsset(currentPath, temporaryName);
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException(L(
                        "Could not create a temporary rename for '" + currentPath + "': " + error,
                        "「" + currentPath + "」の安全な一時リネームに失敗しました: " + error));
            }
        }

        private static void RollBack(IEnumerable<UnityEngine.Object> assets,
            IReadOnlyDictionary<UnityEngine.Object, string> originalNames, List<string> errors)
        {
            try
            {
                UnityEngine.Object[] assetArray = assets.Where(asset => asset != null).ToArray();
                MoveAllToTemporaryNames(assetArray);
                foreach (UnityEngine.Object asset in assetArray)
                {
                    string currentPath = AssetDatabase.GetAssetPath(asset);
                    string error = AssetDatabase.RenameAsset(currentPath, originalNames[asset]);
                    if (!string.IsNullOrEmpty(error))
                        errors.Add(L("Rollback failed for '" + currentPath + "': " + error,
                            "「" + currentPath + "」のロールバックに失敗しました: " + error));
                }
            }
            catch (Exception exception)
            {
                errors.Add(L("Rollback failed: ", "ロールバックに失敗しました: ") + exception.Message);
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static string GetAssetDirectory(string path)
        {
            int separator = path.LastIndexOf('/');
            return separator < 0 ? "Assets" : path.Substring(0, separator);
        }

        private static string L(string english, string japanese)
        {
            return OrganizerLocalization.Text(english, japanese);
        }
    }
}
