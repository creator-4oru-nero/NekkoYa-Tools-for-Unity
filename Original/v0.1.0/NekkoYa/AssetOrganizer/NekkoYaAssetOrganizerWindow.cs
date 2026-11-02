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

        [MenuItem("Tools/NekkoYa/一括リネーム・フォルダー作成")]
        private static void Open()
        {
            var window = GetWindow<NekkoYaAssetOrganizerWindow>();
            window.titleContent = new GUIContent("一括リネーム・フォルダー作成");
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
            EditorGUILayout.LabelField("一括リネーム・フォルダー作成", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            OrganizerLanguage language = OrganizerLocalization.Language;
            OrganizerLanguage newLanguage = (OrganizerLanguage)GUILayout.Toolbar((int)language,
                new[] { "EN", "JP" }, GUILayout.Width(92f));
            if (newLanguage != language)
            {
                OrganizerLocalization.Language = newLanguage;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            selectedTab = (Tab)GUILayout.Toolbar((int)selectedTab, new[]
            {
                OrganizerLocalization.Text("Rename", "リネーム"),
                OrganizerLocalization.Text("Folder Generator", "フォルダー生成")
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
