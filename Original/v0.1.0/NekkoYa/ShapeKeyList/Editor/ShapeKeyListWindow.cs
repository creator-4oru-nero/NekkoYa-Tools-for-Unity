using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sakut.EditorTools
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

        [MenuItem("Tools/NekkoYa/シェイプキー一覧")]
        private static void Open()
        {
            var window = GetWindow<ShapeKeyListWindow>("シェイプキー一覧");
            window.minSize = new Vector2(420, 300);
        }

        private void OnEnable()
        {
            Rebuild();
            EditorApplication.projectChanged += Rebuild;
            EditorApplication.hierarchyChanged += Rebuild;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Rebuild;
            EditorApplication.hierarchyChanged -= Rebuild;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Prefabを下の欄にドラッグしてください。非アクティブな子も含めて表示します。\n" +
                "一覧内の文字は選択してコピーできます。シーン上のオブジェクトも指定できます。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            target = (GameObject)EditorGUILayout.ObjectField("Prefab / オブジェクト", target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                scroll = Vector2.zero;
                Rebuild();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(target == null))
                {
                    if (GUILayout.Button("再読み込み", GUILayout.Width(110))) Rebuild();
                }
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(entries.Count == 0))
                {
                    if (GUILayout.Button("すべてコピー", GUILayout.Width(120))) Copy(allText);
                }
            }

            if (target == null) return;
            EditorGUILayout.LabelField($"メッシュ {entries.Count} 件 / シェイプキー {shapeCount} 個（合計）");
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("メッシュが見つかりませんでした。", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var entry in entries)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("メッシュ名", GUILayout.Width(75));
                        var meshLabel = entry.Name + " (" + entry.Count + " 個)";
                        EditorGUILayout.TextField(meshLabel);
                        if (GUILayout.Button("名前をコピー", GUILayout.Width(100))) Copy(meshLabel);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("シェイプキー  (" + entry.Count + " 個)", EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(entry.Count == 0))
                        {
                            if (GUILayout.Button("コピー", GUILayout.Width(70))) Copy(entry.Text);
                        }
                    }
                    EditorGUILayout.LabelField(new GUIContent(entry.Path, entry.Path), EditorStyles.miniLabel);
                    if (entry.Count == 0)
                        EditorGUILayout.LabelField("（シェイプキーなし）", EditorStyles.miniLabel);
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
            output.AppendLine("メッシュ名: " + mesh.name);
            output.AppendLine("オブジェクト: " + path);
            output.AppendLine("────────────");
            output.AppendLine(entry.Count == 0 ? "（シェイプキーなし）" : entry.Text);
            output.AppendLine("────────────").AppendLine();
        }

        private void Copy(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent("コピーしました"));
        }
    }
}
