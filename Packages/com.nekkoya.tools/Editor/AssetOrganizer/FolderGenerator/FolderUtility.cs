using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal sealed class FolderPreviewEntry
    {
        public string RelativePath;
        public string AssetPath;
        public bool Exists;
        public int Depth;
    }

    internal sealed class FolderCreateResult
    {
        public int CreatedCount;
        public int ExistingCount;
        public readonly List<string> Errors = new List<string>();
    }

    internal static class FolderUtility
    {
        public static List<FolderNode> CloneTree(IEnumerable<FolderNode> roots)
        {
            return roots == null ? new List<FolderNode>() : roots.Select(node => node.Clone()).ToList();
        }

        public static List<string> Validate(string targetPath, IReadOnlyList<FolderNode> roots)
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(targetPath) || !(targetPath == "Assets" || targetPath.StartsWith("Assets/", StringComparison.Ordinal)) ||
                !AssetDatabase.IsValidFolder(targetPath))
                errors.Add(L("Select an existing folder inside Assets as the target.",
                    L10n.Text("Assets内の既存フォルダーを作成先として選択してください。")));
            if (roots == null || roots.Count == 0)
                errors.Add(L("Add at least one root folder.", L10n.Text("ルートフォルダーを1つ以上追加してください。")));
            else
                ValidateLevel(roots, "root", errors);

            if (errors.Count == 0)
            {
                foreach (FolderPreviewEntry entry in BuildPreview(targetPath, roots))
                {
                    UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(entry.AssetPath);
                    if (existing != null && !entry.Exists)
                        errors.Add(L("An asset already exists where a folder is required: " + entry.AssetPath,
                            L10n.Text("フォルダー作成先に同名アセットが存在します: ") + entry.AssetPath));
                }
            }
            return errors;
        }

        public static List<FolderPreviewEntry> BuildPreview(string targetPath, IReadOnlyList<FolderNode> roots)
        {
            var result = new List<FolderPreviewEntry>();
            if (string.IsNullOrEmpty(targetPath) || roots == null)
                return result;
            foreach (FolderNode root in roots)
                AddPreview(root, targetPath, string.Empty, 0, result);
            return result;
        }

        public static FolderCreateResult CreateFolders(string targetPath, IReadOnlyList<FolderNode> roots)
        {
            var result = new FolderCreateResult();
            result.Errors.AddRange(Validate(targetPath, roots));
            if (result.Errors.Count > 0)
                return result;

            var createdPaths = new List<string>();
            try
            {
                foreach (FolderPreviewEntry entry in BuildPreview(targetPath, roots))
                {
                    if (AssetDatabase.IsValidFolder(entry.AssetPath))
                    {
                        result.ExistingCount++;
                        continue;
                    }

                    int separator = entry.AssetPath.LastIndexOf('/');
                    string parentPath = entry.AssetPath.Substring(0, separator);
                    string folderName = entry.AssetPath.Substring(separator + 1);
                    string guid = AssetDatabase.CreateFolder(parentPath, folderName);
                    string actualPath = string.IsNullOrEmpty(guid) ? string.Empty : AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(actualPath) && AssetDatabase.IsValidFolder(actualPath))
                    {
                        createdPaths.Add(actualPath);
                        result.CreatedCount++;
                    }
                    if (actualPath != entry.AssetPath || !AssetDatabase.IsValidFolder(entry.AssetPath))
                        throw new InvalidOperationException(L("Unity could not create folder '" + entry.AssetPath + "'.",
                            L10n.Text("Unityでフォルダー「") + entry.AssetPath + L10n.Text("」を作成できませんでした。")));

                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                result.Errors.Add(exception.Message);
                // 今回作成した空フォルダーだけを子から親の順に戻す。
                for (int i = createdPaths.Count - 1; i >= 0; i--)
                {
                    string path = createdPaths[i];
                    try
                    {
                        if (!Directory.Exists(path)) { result.CreatedCount--; continue; }
                        if (Directory.GetFileSystemEntries(path).Length == 0 && AssetDatabase.DeleteAsset(path))
                            result.CreatedCount--;
                        else
                            result.Errors.Add(L10n.Text("新規フォルダーを自動で戻せませんでした。内容を確認してください: ") + path);
                    }
                    catch (Exception rollback)
                    {
                        result.Errors.Add(L10n.Text("ロールバックに失敗しました: ") + path + ": " + rollback.Message);
                    }
                }
            }
            return result;
        }

        private static void ValidateLevel(IReadOnlyList<FolderNode> nodes, string location, List<string> errors)
        {
            AssetNameValidator.ValidateSiblingNames(nodes, node => node.Name,
                location == "root" ? L("root", L10n.Text("ルート階層")) : location, errors);
            foreach (FolderNode node in nodes)
                ValidateLevel(node.Children, location + "/" + node.Name, errors);
        }

        private static void AddPreview(FolderNode node, string targetPath, string parentRelativePath,
            int depth, List<FolderPreviewEntry> result)
        {
            string relativePath = string.IsNullOrEmpty(parentRelativePath)
                ? node.Name
                : parentRelativePath + "/" + node.Name;
            string assetPath = targetPath.TrimEnd('/') + "/" + relativePath;
            result.Add(new FolderPreviewEntry
            {
                RelativePath = relativePath,
                AssetPath = assetPath,
                Exists = AssetDatabase.IsValidFolder(assetPath),
                Depth = depth
            });
            foreach (FolderNode child in node.Children)
                AddPreview(child, targetPath, relativePath, depth + 1, result);
        }

        private static string L(string english, string japanese)
        {
            return OrganizerLocalization.Text(english, japanese);
        }
    }
}
