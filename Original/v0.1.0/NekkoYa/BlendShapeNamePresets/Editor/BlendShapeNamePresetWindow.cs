using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LocalTools.BlendShapeNamePresets
{
    public sealed class BlendShapeNamePresetWindow : EditorWindow
    {
        [Serializable]
        private sealed class ShapeValue
        {
            public string name;
            public float weight;
        }

        [Serializable]
        private sealed class RendererValues
        {
            // Segments avoid confusing a literal slash in a GameObject name with a separator.
            public string[] path;
            public List<ShapeValue> shapes = new List<ShapeValue>();
        }

        [Serializable]
        private sealed class Preset
        {
            public string format;
            public int version;
            public string rootName;
            public string savedUtc;
            public List<RendererValues> renderers = new List<RendererValues>();
        }

        private sealed class Write
        {
            public SkinnedMeshRenderer renderer;
            public int index;
            public float value;
        }

        private sealed class Preview
        {
            public readonly List<Write> writes = new List<Write>();
            public readonly List<string> lines = new List<string>();
            public readonly List<string> errors = new List<string>();
            public int matched;
            public int added;
            public int removed;
        }

        private const string Format = "BlendShapeNamePreset";
        [SerializeField] private GameObject root;
        [SerializeField] private string presetJson;
        [SerializeField] private string presetPath;
        [SerializeField] private bool zeroNewShapes = true;
        private Vector2 scroll;
        private Preview preview;
        private string notice;

        [MenuItem("Tools/NekkoYa/シェイプキー保存・復元")]
        private static void Open()
        {
            var window = GetWindow<BlendShapeNamePresetWindow>("シェイプキー保存・復元");
            window.minSize = new Vector2(520, 400);
            if (window.root == null && Selection.activeGameObject != null &&
                !EditorUtility.IsPersistent(Selection.activeGameObject))
                window.root = Selection.activeGameObject;
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Invalidate;
            EditorApplication.projectChanged += Invalidate;
            EditorApplication.hierarchyChanged += Invalidate;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Invalidate;
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.hierarchyChanged -= Invalidate;
        }

        private void Invalidate()
        {
            preview = null;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("FBX更新前に保存 → FBX更新 → 差分を確認 → 名前で復元。\n非アクティブの子も含め、0の値も保存します。", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            root = (GameObject)EditorGUILayout.ObjectField("対象ルート（アバター）", root, typeof(GameObject), true);
            zeroNewShapes = EditorGUILayout.ToggleLeft("新しく追加されたキーを0にする（保存済みRendererのみ）", zeroNewShapes);
            if (EditorGUI.EndChangeCheck()) Invalidate();

            string problem = RootProblem();
            if (problem != null) EditorGUILayout.HelpBox(problem, MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(problem != null))
                    if (GUILayout.Button("現在の値をJSONに保存…")) Run(SaveSnapshot);
                if (GUILayout.Button("保存したJSONを開く…")) Run(LoadSnapshot);
            }
            if (!string.IsNullOrEmpty(notice)) EditorGUILayout.HelpBox(notice, MessageType.Info);
            if (string.IsNullOrEmpty(presetJson)) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("プリセット", presetPath ?? "", EditorStyles.wordWrappedLabel);
            if (problem != null) return;
            if (preview == null) Run(() => preview = BuildPreview(ReadPreset(presetJson)));
            if (preview == null) return;
            EditorGUILayout.LabelField($"一致 {preview.matched} / 追加 {preview.added} / 削除 {preview.removed} / 値の変更 {preview.writes.Count}");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("差分を更新")) Run(() => preview = BuildPreview(ReadPreset(presetJson)));
                using (new EditorGUI.DisabledScope(preview.errors.Count > 0))
                    if (GUILayout.Button("名前で復元（Undo対応）")) Run(Restore);
            }
            if (preview == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string error in preview.errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            foreach (string line in preview.lines) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private string RootProblem()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Play Modeを終了してから使用してください。";
            if (AnimationMode.InAnimationMode()) return "Animationのプレビュー／記録を終了してから使用してください。";
            if (root == null) return "Hierarchyからアバターなどのルートを指定してください。";
            if (EditorUtility.IsPersistent(root) || !root.scene.IsValid() || !root.scene.isLoaded)
                return "シーン上のオブジェクト、またはPrefab Modeで開いたオブジェクトを指定してください。";
            return null;
        }

        private void Run(Action action)
        {
            try { action(); }
            catch (Exception e)
            {
                notice = "処理できませんでした: " + e.Message;
                Debug.LogWarning("[BlendShape Name Presets] " + e.Message);
            }
        }

        private static string Label(string[] path)
        {
            return path.Length == 0 ? "（ルート）" : string.Join(" / ", path);
        }

        private string[] PathFor(Transform target)
        {
            var segments = new List<string>();
            while (target != root.transform)
            {
                if (target.parent == null) throw new InvalidOperationException("対象ルートの外にあるRendererです。");
                int sameNames = 0;
                foreach (Transform sibling in target.parent)
                    if (sibling.name == target.name) sameNames++;
                if (sameNames != 1)
                    throw new InvalidOperationException($"「{target.name}」の同名兄弟があり、Rendererを一意に保存できません。");
                segments.Add(target.name);
                target = target.parent;
            }
            segments.Reverse();
            return segments.ToArray();
        }

        private SkinnedMeshRenderer Resolve(string[] path)
        {
            Transform current = root.transform;
            foreach (string segment in path)
            {
                Transform next = null;
                foreach (Transform child in current)
                {
                    if (child.name != segment) continue;
                    if (next != null) throw new InvalidOperationException($"{Label(path)}: 同名兄弟があり対応を特定できません。");
                    next = child;
                }
                if (next == null) throw new InvalidOperationException($"{Label(path)}: 保存したRendererの階層が見つかりません。");
                current = next;
            }
            var renderers = current.GetComponents<SkinnedMeshRenderer>();
            if (renderers.Length != 1 || renderers[0].sharedMesh == null)
                throw new InvalidOperationException($"{Label(path)}: Mesh付きのSkinnedMeshRendererが1つ必要です。");
            return renderers[0];
        }

        private static Dictionary<string, int> ShapeIndices(Mesh mesh)
        {
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                if (indices.ContainsKey(name)) throw new InvalidOperationException($"Mesh「{mesh.name}」に同名キー「{name}」が複数あります。");
                indices.Add(name, i);
            }
            return indices;
        }

        private void SaveSnapshot()
        {
            if (RootProblem() != null) throw new InvalidOperationException(RootProblem());
            var preset = new Preset { format = Format, version = 1, rootName = root.name, savedUtc = DateTime.UtcNow.ToString("o") };
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0) continue;
                var entry = new RendererValues { path = PathFor(renderer.transform) };
                if (Resolve(entry.path) != renderer) throw new InvalidOperationException("Rendererの対応を特定できません。");
                foreach (var pair in ShapeIndices(mesh))
                    entry.shapes.Add(new ShapeValue { name = pair.Key, weight = renderer.GetBlendShapeWeight(pair.Value) });
                preset.renderers.Add(entry);
            }
            if (preset.renderers.Count == 0) throw new InvalidOperationException("保存できるシェイプキーがありません。");
            string json = JsonUtility.ToJson(preset, true);
            ReadPreset(json); // Validate before writing or replacing a known-good snapshot.
            string path = EditorUtility.SaveFilePanel("FBX更新前の値を保存", Application.dataPath, "BlendShapes-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), "json");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, json, new UTF8Encoding(false));
            presetJson = json;
            presetPath = path;
            preview = null;
            notice = $"{preset.renderers.Count}個のRendererを保存しました。FBX更新後、このJSONを使って復元してください。";
        }

        private void LoadSnapshot()
        {
            string path = EditorUtility.OpenFilePanel("シェイプキープリセットを開く", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            string json = File.ReadAllText(path, Encoding.UTF8);
            var preset = ReadPreset(json);
            presetJson = json;
            presetPath = path;
            preview = null;
            notice = $"保存元: {preset.rootName} / 保存日時 (UTC): {preset.savedUtc}";
        }

        private static Preset ReadPreset(string json)
        {
            var preset = JsonUtility.FromJson<Preset>(json);
            if (preset == null || preset.format != Format || preset.version != 1 || preset.renderers == null || preset.renderers.Count == 0)
                throw new InvalidDataException("対応するプリセット（version 1）ではありません。");
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var renderer in preset.renderers)
            {
                if (renderer == null || renderer.path == null || renderer.path.Any(p => p == null) || renderer.shapes == null)
                    throw new InvalidDataException("Rendererの保存データが不正です。");
                // Length prefixes make the identity unambiguous even when names contain slashes.
                string key = string.Concat(renderer.path.Select(p => p.Length + ":" + p));
                if (!paths.Add(key)) throw new InvalidDataException("同じRendererが重複しています。");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var shape in renderer.shapes)
                    if (shape == null || shape.name == null || !names.Add(shape.name) || float.IsNaN(shape.weight) || float.IsInfinity(shape.weight))
                        throw new InvalidDataException("シェイプキーの名前または値が不正です。");
            }
            return preset;
        }

        private Preview BuildPreview(Preset preset)
        {
            var result = new Preview();
            var visited = new HashSet<SkinnedMeshRenderer>();
            foreach (var entry in preset.renderers)
            {
                try
                {
                    var renderer = Resolve(entry.path);
                    var indices = ShapeIndices(renderer.sharedMesh);
                    visited.Add(renderer);
                    string label = Label(entry.path);
                    result.lines.Add("■ " + label);
                    var savedNames = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var shape in entry.shapes)
                    {
                        savedNames.Add(shape.name);
                        if (!indices.TryGetValue(shape.name, out int index))
                        {
                            result.removed++;
                            result.lines.Add($"  削除: {shape.name}（保存値 {shape.weight:G9}）— スキップ");
                            continue;
                        }
                        result.matched++;
                        AddWrite(result, renderer, index, shape.weight, shape.name);
                    }
                    foreach (var pair in indices)
                    {
                        if (savedNames.Contains(pair.Key)) continue;
                        result.added++;
                        result.lines.Add($"  追加: {pair.Key} — " + (zeroNewShapes ? "0に設定" : "現在値を維持"));
                        if (zeroNewShapes) AddWrite(result, renderer, pair.Value, 0f, pair.Key);
                    }
                }
                catch (InvalidOperationException e) { result.errors.Add(e.Message); }
            }
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (!visited.Contains(renderer) && renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                    result.lines.Add($"未保存のRenderer: {renderer.name} — 変更しません");
            if (result.errors.Count > 0) result.lines.Add("階層・Rendererの問題を解消すると復元できます。まだ値は変更されていません。");
            return result;
        }

        private static void AddWrite(Preview result, SkinnedMeshRenderer renderer, int index, float value, string name)
        {
            float current = renderer.GetBlendShapeWeight(index);
            if (current == value) return;
            result.writes.Add(new Write { renderer = renderer, index = index, value = value });
            result.lines.Add($"  {name}: {current:G9} → {value:G9}");
        }

        private void Restore()
        {
            if (RootProblem() != null) throw new InvalidOperationException(RootProblem());
            // Re-resolve all names immediately before writing: never reuse pre-import indices.
            preview = BuildPreview(ReadPreset(presetJson));
            if (preview.errors.Count > 0) throw new InvalidOperationException("対応できないRendererがあります。差分のエラーを確認してください。");
            if (preview.writes.Count == 0) { notice = "適用可能なキーに値の変更はありません。追加・削除キーは差分表示を確認してください。"; return; }
            var renderers = preview.writes.Select(w => w.renderer).Distinct().ToArray();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Restore Blend Shapes By Name");
            Undo.RegisterCompleteObjectUndo(renderers, "Restore Blend Shapes By Name");
            try
            {
                foreach (var write in preview.writes) write.renderer.SetBlendShapeWeight(write.index, write.value);
                foreach (var renderer in renderers)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                    EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
                }
                Undo.CollapseUndoOperations(group);
                notice = $"{renderers.Length}個のRenderer、{preview.writes.Count}個の値を復元しました。Ctrl+Zで戻せます。必要に応じてシーン／Prefabを保存してください。";
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
            finally { preview = null; }
            SceneView.RepaintAll();
        }
    }
}
