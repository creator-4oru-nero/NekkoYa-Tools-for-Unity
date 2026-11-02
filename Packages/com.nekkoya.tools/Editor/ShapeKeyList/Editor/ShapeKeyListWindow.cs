using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.ShapeKeyList
{
    public sealed class ShapeKeyListWindow : EditorWindow
    {
        private sealed class MeshEntry
        {
            public string Name;
            public string Path;
            public string Text;
            public int Count;
        }

        [SerializeField] private GameObject target;
        private readonly List<MeshEntry> entries = new List<MeshEntry>();
        private Vector2 scroll;
        private string allText = string.Empty;
        private int shapeCount;

        [MenuItem("NekkoYa/Shape Key List")]
        private static void Open()
        {
            var window = GetWindow<ShapeKeyListWindow>("Shape Key List");
            window.minSize = new Vector2(420, 300);
        }

        private void OnEnable()
        {
            L10n.LanguageChanged += Rebuild;
            Rebuild();
            EditorApplication.projectChanged += Rebuild;
            EditorApplication.hierarchyChanged += Rebuild;
        }

        private void OnDisable()
        {
            L10n.LanguageChanged -= Rebuild;
            EditorApplication.projectChanged -= Rebuild;
            EditorApplication.hierarchyChanged -= Rebuild;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                L10n.Text("Prefabを下の欄にドラッグしてください。非アクティブな子も含めて表示します。\n") +
                L10n.Text("一覧内の文字は選択してコピーできます。シーン上のオブジェクトも指定できます。"),
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            target = (GameObject)EditorGUILayout.ObjectField(L10n.Text("Prefab / オブジェクト"), target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                scroll = Vector2.zero;
                Rebuild();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(target == null))
                {
                    if (GUILayout.Button(L10n.Text("再読み込み"), GUILayout.Width(110))) Rebuild();
                }
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(entries.Count == 0))
                {
                    if (GUILayout.Button(L10n.Text("すべてコピー"), GUILayout.Width(120))) Copy(allText);
                }
            }

            if (target == null) return;
            EditorGUILayout.LabelField(string.Format(L10n.Text("メッシュ {0} 件 / シェイプキー {1} 個（合計）"), entries.Count, shapeCount));
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(L10n.Text("メッシュが見つかりませんでした。"), MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var entry in entries)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(L10n.Text("メッシュ名"), GUILayout.Width(75));
                        var meshLabel = entry.Name + " (" + entry.Count + L10n.Text(" 個)");
                        EditorGUILayout.TextField(meshLabel);
                        if (GUILayout.Button(L10n.Text("名前をコピー"), GUILayout.Width(100))) Copy(meshLabel);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(L10n.Text("シェイプキー  (") + entry.Count + L10n.Text(" 個)"), EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(entry.Count == 0))
                        {
                            if (GUILayout.Button(L10n.Text("コピー"), GUILayout.Width(70))) Copy(entry.Text);
                        }
                    }
                    EditorGUILayout.LabelField(new GUIContent(entry.Path, entry.Path), EditorStyles.miniLabel);
                    if (entry.Count == 0)
                        EditorGUILayout.LabelField(L10n.Text("（シェイプキーなし）"), EditorStyles.miniLabel);
                    else
                    {
                        // Ignore edits so typing never changes the generated list or the source asset.
                        EditorGUILayout.TextArea(entry.Text, GUILayout.MinHeight(54));
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void Rebuild()
        {
            entries.Clear();
            shapeCount = 0;
            var output = new StringBuilder();
            if (target != null)
            {
                foreach (var transform in target.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var renderer in transform.GetComponents<SkinnedMeshRenderer>())
                        AddMesh(renderer.sharedMesh, transform, output);
                    foreach (var filter in transform.GetComponents<MeshFilter>())
                        AddMesh(filter.sharedMesh, transform, output);
                }
            }
            allText = output.ToString().TrimEnd('\r', '\n');
            Repaint();
        }

        private void AddMesh(Mesh mesh, Transform owner, StringBuilder output)
        {
            if (mesh == null) return;
            var relativePath = AnimationUtility.CalculateTransformPath(owner, target.transform);
            var path = target.name + (relativePath.Length > 0 ? "/" + relativePath : "");
            var text = new StringBuilder();
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                if (i > 0) text.AppendLine();
                text.Append(mesh.GetBlendShapeName(i));
            }
            var entry = new MeshEntry
            {
                Name = mesh.name,
                Path = path,
                Count = mesh.blendShapeCount,
                Text = text.ToString()
            };
            entries.Add(entry);
            shapeCount += entry.Count;
            output.AppendLine(L10n.Text("メッシュ名: ") + mesh.name);
            output.AppendLine(L10n.Text("オブジェクト: ") + path);
            output.AppendLine("────────────");
            output.AppendLine(entry.Count == 0 ? L10n.Text("（シェイプキーなし）") : entry.Text);
            output.AppendLine("────────────").AppendLine();
        }

        private void Copy(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent(L10n.Text("コピーしました")));
        }
    }
}
