using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    internal sealed class TextPromptWindow : EditorWindow
    {
        private string label;
        private string value;
        private Action<string> accepted;

        public static void Open(string title, string label, string initialValue, Action<string> accepted)
        {
            var window = CreateInstance<TextPromptWindow>();
            window.titleContent = new GUIContent("Asset Organizer");
            window.label = label;
            window.value = initialValue;
            window.accepted = accepted;
            window.minSize = new Vector2(340f, 82f);
            window.maxSize = new Vector2(700f, 82f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("PromptValue");
            value = EditorGUILayout.TextField(label, value);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(OrganizerLocalization.Text("Cancel", L10n.Text("キャンセル"))))
                Close();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(value)))
            {
                if (GUILayout.Button("OK"))
                {
                    accepted?.Invoke(value.Trim());
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Close();
        }
    }
}
