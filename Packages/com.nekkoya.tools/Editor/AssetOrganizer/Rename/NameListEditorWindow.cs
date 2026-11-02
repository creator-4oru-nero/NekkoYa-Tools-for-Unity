using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal sealed class NameListEditorWindow : EditorWindow
    {
        private IReadOnlyList<RenameItem> items;
        private Action applied;
        private string text;
        private Vector2 scroll;

        public static void Open(IReadOnlyList<RenameItem> items, Action applied)
        {
            var window = CreateInstance<NameListEditorWindow>();
            window.titleContent = new GUIContent("Edit Name List");
            window.items = items;
            window.applied = applied;
            window.text = string.Join("\n", items.Select(item => item.CustomName));
            window.minSize = new Vector2(360f, 300f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L("One {Name} value per asset, in list order.",
                L10n.Text("アセット一覧の順番どおりに、1行につき1つの{Name}を入力してください。")), EditorStyles.wordWrappedLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            text = EditorGUILayout.TextArea(text ?? string.Empty, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            string[] names = SplitLines(text ?? string.Empty);
            bool matches = items != null && names.Length == items.Count;
            if (!matches)
                EditorGUILayout.HelpBox(L(
                    "Name count (" + names.Length + ") must match asset count (" + (items == null ? 0 : items.Count) + ").",
                    L10n.Text("Name数（") + names.Length + L10n.Text("）とアセット数（") + (items == null ? 0 : items.Count) + L10n.Text("）を一致させてください。")),
                    MessageType.Warning);

            using (new EditorGUI.DisabledScope(!matches))
            {
                if (GUILayout.Button(L("Apply", L10n.Text("適用"))))
                {
                    for (int i = 0; i < items.Count; i++)
                        items[i].CustomName = names[i];
                    applied?.Invoke();
                    Close();
                }
            }
        }

        private static string[] SplitLines(string value)
        {
            string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        private static string L(string english, string japanese)
        {
            return OrganizerLocalization.Text(english, japanese);
        }
    }
}
