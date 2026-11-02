using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal static class FolderPresetRepository
    {
        public const string DefaultDirectory = "Assets/Editor/NekkoYa/AssetOrganizer/Presets";

        public static List<FolderPreset> LoadAll()
        {
            return AssetDatabase.FindAssets("t:FolderPreset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FolderPreset>)
                .Where(preset => preset != null)
                .OrderBy(preset => preset.name)
                .ToList();
        }

        public static FolderPreset Create(string path, IEnumerable<FolderNode> roots)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException(L10n.Text("新しい.assetファイルをAssets内に指定してください。既存ファイルは上書きしません。"));
            var preset = ScriptableObject.CreateInstance<FolderPreset>();
            preset.Roots = FolderUtility.CloneTree(roots);
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            return preset;
        }

        public static string AskForPresetPath(string defaultName)
        {
            EnsureDefaultDirectory();
            return EditorUtility.SaveFilePanelInProject(
                OrganizerLocalization.Text("Save Folder Preset", L10n.Text("フォルダープリセットを保存")), defaultName, "asset",
                OrganizerLocalization.Text("Choose a project location for the portable preset.",
                    L10n.Text("持ち運び可能なプリセットの保存先をProject内から選択してください。")), DefaultDirectory);
        }

        private static void EnsureDefaultDirectory()
        {
            string current = "Assets";
            string[] parts = DefaultDirectory.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
