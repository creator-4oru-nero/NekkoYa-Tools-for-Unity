using L10n = NekkoYa.Editor.NekkoYaLocalization;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal sealed class NekkoYaAssetOrganizerWindow : EditorWindow
    {
        private enum Tab
        {
            Rename,
            FolderGenerator
        }

        [SerializeField] private Tab selectedTab;
        [SerializeField] private RenameTab renameTab = new RenameTab();
        [SerializeField] private FolderGeneratorTab folderGeneratorTab = new FolderGeneratorTab();

        [MenuItem("NekkoYa/Asset Organizer")]
        private static void Open()
        {
            var window = GetWindow<NekkoYaAssetOrganizerWindow>();
            window.titleContent = new GUIContent("Asset Organizer");
            window.minSize = new Vector2(560f, 540f);
            window.Show();
        }

        private void OnEnable()
        {
            if (renameTab == null)
                renameTab = new RenameTab();
            if (folderGeneratorTab == null)
                folderGeneratorTab = new FolderGeneratorTab();
            renameTab.Initialize(this);
            folderGeneratorTab.Initialize(this);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset Organizer", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            selectedTab = (Tab)GUILayout.Toolbar((int)selectedTab, new[]
            {
                OrganizerLocalization.Text("Rename", L10n.Text("リネーム")),
                OrganizerLocalization.Text("Folder Generator", L10n.Text("フォルダー生成"))
            });
            EditorGUILayout.Space(6f);

            switch (selectedTab)
            {
                case Tab.Rename:
                    renameTab.OnGUI();
                    break;
                case Tab.FolderGenerator:
                    folderGeneratorTab.OnGUI();
                    break;
            }
        }
    }
}
