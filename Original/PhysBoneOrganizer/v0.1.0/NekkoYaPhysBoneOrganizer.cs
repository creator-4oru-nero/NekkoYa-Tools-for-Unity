#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NekkoYa.EditorTools
{
    public sealed class PhysBoneOrganizer : EditorWindow
    {
        private const string PhysBoneTypeName = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone";
        [SerializeField] private Transform armature;
        [SerializeField] private Transform avatar;
        private Transform[] candidates = Array.Empty<Transform>();
        private Vector2 scroll;
        private Component[] preview = Array.Empty<Component>();

        [MenuItem("Tools/NekkoYa/PhysBone Organizer")]
        private static void Open()
        {
            GetWindow<PhysBoneOrganizer>("PhysBone Organizer");
        }

        private static Type FindPhysBoneType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(PhysBoneTypeName, false))
                .FirstOrDefault(t => t != null);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("PhysBoneをPBにまとめる", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("アバター本体を指定すると、中のアーマチュアを自動検出します。ボーンに付いているPhysBoneを、アバター直下のPBにまとめます。", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            avatar = (Transform)EditorGUILayout.ObjectField("アバター本体", avatar, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) DetectArmatures();
            using (new EditorGUI.DisabledScope(!avatar))
                if (GUILayout.Button("アーマチュアを再検出")) DetectArmatures();
            if (avatar && candidates.Length == 0)
                EditorGUILayout.HelpBox("アーマチュアが見つかりません。下の欄に手動で指定してください。", MessageType.Info);
            if (candidates.Length > 1)
            {
                EditorGUILayout.HelpBox("候補が複数あります。今回まとめるアーマチュアを選んでください。", MessageType.Info);
                int current = Array.IndexOf(candidates, armature);
                var labels = new[] { "候補を選択…" }.Concat(candidates.Select(t => t ? AnimationUtility.CalculateTransformPath(t, avatar) : "（削除済み）")).ToArray();
                EditorGUI.BeginChangeCheck();
                int selected = EditorGUILayout.Popup("検出した候補", current + 1, labels);
                if (EditorGUI.EndChangeCheck())
                {
                    armature = selected > 0 ? candidates[selected - 1] : null;
                    preview = Array.Empty<Component>();
                }
            }
            EditorGUI.BeginChangeCheck();
            armature = (Transform)EditorGUILayout.ObjectField("対象アーマチュア", armature, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) preview = Array.Empty<Component>();
            EditorGUILayout.HelpBox("作成する構成: アバター本体 / PB / 元のオブジェクト名\n既存のPBがある場合はPB (1)などを新規作成します。Root Transformが空なら元のボーンを設定し、設定済みならそのまま維持します。", MessageType.None);
            EditorGUILayout.HelpBox("ボーンのON/OFFやAnimationClipによる制御は自動で引き継ぎません。これらを使用している場合は整理後に確認してください。", MessageType.Warning);

            string issue = Validate();
            if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            using (new EditorGUI.DisabledScope(issue != null))
            {
                if (GUILayout.Button("対象を確認")) preview = Collect();
                if (GUILayout.Button("PhysBoneをPBにまとめる（Undo対応）")) Organize();
            }
            EditorGUILayout.LabelField("確認対象: " + preview.Length + " 個");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var component in preview)
            {
                if (!component) continue;
                EditorGUILayout.ObjectField(component, typeof(Component), true);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DetectArmatures()
        {
            preview = Array.Empty<Component>();
            candidates = avatar ? avatar.GetComponentsInChildren<Transform>(true)
                .Where(t => t != avatar && System.Text.RegularExpressions.Regex.IsMatch(
                    t.name, @"^(Armature|アーマチュア)([._ -]?\d+| \(\d+\))?$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                .ToArray() : Array.Empty<Transform>();
            armature = candidates.Length == 1 ? candidates[0] : null;
        }

        private string Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Play Modeを終了してください。";
            if (FindPhysBoneType() == null) return "VRCPhysBoneが見つかりません。VRChat SDK Avatarsを導入してください。";
            if (!avatar) return "Hierarchyからアバター本体を指定してください。";
            if (!armature) return "対象アーマチュアを指定してください。";
            if (EditorUtility.IsPersistent(armature) || EditorUtility.IsPersistent(avatar)) return "Project内のPrefabではなくHierarchy上のオブジェクトを指定してください。";
            if (!armature.gameObject.scene.IsValid() || !armature.gameObject.scene.isLoaded ||
                armature.gameObject.scene != avatar.gameObject.scene) return "同じ開いているシーン内のオブジェクトを指定してください。";
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(armature.gameObject.scene)) return "Prefab Modeでは実行できません。通常のシーンで使用してください。";
            if (avatar == armature || !armature.IsChildOf(avatar)) return "アバター本体の中にあるアーマチュアを指定してください。";
            if (!avatar.gameObject.activeInHierarchy) return "アバター本体をアクティブにしてください。";
            return null;
        }

        private Component[] Collect()
        {
            var type = FindPhysBoneType();
            return armature && type != null ? armature.GetComponentsInChildren(type, true) : Array.Empty<Component>();
        }

        private void Organize()
        {
            var issue = Validate();
            if (issue != null) { EditorUtility.DisplayDialog("実行できません", issue, "OK"); return; }
            var sources = Collect();
            preview = sources;
            if (sources.Length == 0) { EditorUtility.DisplayDialog("PhysBone Organizer", "対象のPhysBoneがありません。", "OK"); return; }

            // Validate every source before making any changes.
            foreach (var source in sources)
            {
                var serialized = new SerializedObject(source);
                var root = serialized.FindProperty("rootTransform");
                if (root == null || root.propertyType != SerializedPropertyType.ObjectReference)
                {
                    EditorUtility.DisplayDialog("実行できません", "rootTransformを読み取れないPhysBoneがあります。SDKの互換性を確認してください。", "OK");
                    return;
                }
                if ((source.hideFlags & HideFlags.NotEditable) != 0 || PrefabUtility.IsPartOfImmutablePrefab(source))
                {
                    EditorUtility.DisplayDialog("実行できません", "編集できないPrefabまたはコンポーネントが含まれています。", "OK");
                    return;
                }
            }
            if (!EditorUtility.DisplayDialog("PhysBoneをPBにまとめる", sources.Length + " 個のPhysBoneをPB配下の専用オブジェクトにまとめます。\n元のボーンからはPhysBoneコンポーネントだけを外し、ボーンの階層と設定は維持します。\nシーンは自動保存しません。", "まとめる", "キャンセル")) return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize PhysBones");
            try
            {
                var folder = CreateObject(UniqueName(avatar, "PB"), avatar);
                var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                foreach (var source in sources)
                {
                    var holder = CreateObject(UniqueName(folder.transform, source.gameObject.name), folder.transform);
                    // Preserve the source's current effective activation state.
                    holder.SetActive(source.gameObject.activeInHierarchy);
                    var copy = Undo.AddComponent(holder, source.GetType());
                    if (!copy) throw new InvalidOperationException("PhysBoneを追加できませんでした。");
                    Undo.RecordObject(copy, "Copy PhysBone settings");
                    EditorUtility.CopySerialized(source, copy);
                    var serialized = new SerializedObject(copy);
                    var root = serialized.FindProperty("rootTransform");
                    if (!root.objectReferenceValue) root.objectReferenceValue = source.transform;
                    serialized.ApplyModifiedProperties();
                    replacements.Add(source, copy);
                }

                // Redirect serialized component references in loaded scenes before deletion.
                // Assets and animation paths are intentionally outside this operation.
                for (int s = 0; s < SceneManager.sceneCount; s++)
                {
                    var scene = SceneManager.GetSceneAt(s);
                    if (!scene.isLoaded || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene)) continue;
                    foreach (var rootObject in scene.GetRootGameObjects())
                    foreach (var component in rootObject.GetComponentsInChildren<Component>(true))
                    {
                        if (!component || replacements.ContainsKey(component)) continue;
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        bool changed = false;
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var reference = property.objectReferenceValue;
                            if (!reference || !replacements.TryGetValue(reference, out var replacement)) continue;
                            if (!property.editable || (component.hideFlags & HideFlags.NotEditable) != 0)
                                throw new InvalidOperationException("更新できないPhysBone参照があります: " + component.name);
                            if (!changed) Undo.RecordObject(component, "Remap PhysBone reference");
                            property.objectReferenceValue = replacement;
                            changed = true;
                        }
                        if (changed)
                        {
                            serialized.ApplyModifiedProperties();
                            if (PrefabUtility.IsPartOfPrefabInstance(component))
                                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        }
                    }
                }
                foreach (var source in sources) Undo.DestroyObjectImmediate(source);
                Undo.CollapseUndoOperations(group);
                preview = Array.Empty<Component>();
                EditorUtility.DisplayDialog("完了", sources.Length + " 個のPhysBoneを " + avatar.name + "/" + folder.name + " にまとめました。\nCtrl+Zで戻せます。確認後にシーンを保存してください。", "OK");
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("整理を中止しました", "変更をUndoで取り消しました。\n" + exception.Message, "OK");
            }
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create PhysBone container");
            Undo.SetTransformParent(created.transform, parent, "Parent PhysBone container");
            Undo.RecordObject(created.transform, "Reset PhysBone container transform");
            created.transform.localPosition = Vector3.zero;
            created.transform.localRotation = Quaternion.identity;
            created.transform.localScale = Vector3.one;
            return created;
        }

        private static string UniqueName(Transform parent, string original)
        {
            var names = new HashSet<string>();
            foreach (Transform child in parent) names.Add(child.name);
            string candidate = original;
            for (int i = 1; names.Contains(candidate); i++) candidate = original + " (" + i + ")";
            return candidate;
        }
    }
}
#endif
