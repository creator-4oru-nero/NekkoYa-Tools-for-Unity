#if UNITY_EDITOR
using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NekkoYa.PrefabFullSync
{
    public sealed class PrefabFullSyncWindow : EditorWindow
    {
        [SerializeField] private GameObject source;
        [SerializeField] private GameObject destination;
        private Snapshot sourcePreview, destinationPreview;
        private readonly List<string> differences = new List<string>();
        private Vector2 scroll;
        private string problem, dependencyHash, lastBackup;
        private bool previewReady;

        [MenuItem("NekkoYa/Prefab Full Sync")]
        private static void Open() => GetWindow<PrefabFullSyncWindow>("Prefab Full Sync");

        private void OnGUI()
        {
            EditorGUILayout.LabelField(L10n.Text("Hierarchy → Prefab 全差分反映"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            source = (GameObject)EditorGUILayout.ObjectField(L10n.Text("反映元（Hierarchy）"), source, typeof(GameObject), true);
            destination = (GameObject)EditorGUILayout.ObjectField(L10n.Text("反映先（Project）"), destination, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck()) previewReady = false;
            EditorGUILayout.HelpBox(L10n.Text("子階層・コンポーネント・保存可能な全プロパティを置き換えます。反映先だけにある要素は削除します。ルートの名前・ローカルTransformも反映します。ネストとVariantの継承は展開し、通常のPrefabとして保存します。"), MessageType.Warning);
            EditorGUILayout.HelpBox(L10n.Text("参照先のMaterial・Mesh等のアセット自体は編集しません。同名オブジェクトや同種コンポーネントが複数ある場合、Unityの上書き保存で内部fileIDが変わり、他のアセットからの参照に影響する可能性があります。"), MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                if (GUILayout.Button(L10n.Text("差分をプレビュー"))) BuildPreview();
                using (new EditorGUI.DisabledScope(!previewReady))
                    if (GUILayout.Button(L10n.Text("バックアップを作成して全差分を反映"))) Apply();
            }
            if (!string.IsNullOrEmpty(problem)) EditorGUILayout.HelpBox(problem, MessageType.Error);
            if (previewReady)
            {
                EditorGUILayout.LabelField(string.Format(L10n.Text("差分 {0} 件（継承構造の展開は常に実行）"), differences.Count));
                scroll = EditorGUILayout.BeginScrollView(scroll);
                foreach (string item in differences) EditorGUILayout.SelectableLabel(item, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(38));
                EditorGUILayout.EndScrollView();
            }
            if (!string.IsNullOrEmpty(lastBackup) && GUILayout.Button(L10n.Text("直近のバックアップフォルダを開く")))
                EditorUtility.RevealInFinder(lastBackup);
        }

        private string Validate()
        {
            if (!source || EditorUtility.IsPersistent(source) || !source.scene.IsValid() || EditorSceneManager.IsPreviewScene(source.scene))
                throw new InvalidOperationException(L10n.Text("反映元には通常のHierarchy内のGameObjectを指定してください。"));
            if (PrefabStageUtility.GetPrefabStage(source) != null)
                throw new InvalidOperationException(L10n.Text("Prefab Modeではなく通常のシーンのオブジェクトを指定してください。"));
            string path = AssetDatabase.GetAssetPath(destination);
            if (!destination || !EditorUtility.IsPersistent(destination) || !path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                || AssetDatabase.LoadAssetAtPath<GameObject>(path) != destination)
                throw new InvalidOperationException(L10n.Text("反映先にはAssets内のPrefabアセットのルートを指定してください。"));
            if (!AssetDatabase.IsOpenForEdit(path) || (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
                throw new InvalidOperationException(L10n.Text("反映先が書き込み可能ではありません。"));
            if (EditorUtility.IsDirty(destination))
                throw new InvalidOperationException(L10n.Text("反映先Prefabに未保存の変更があります。先に保存してください。"));
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == path)
                throw new InvalidOperationException(L10n.Text("反映先のPrefab Modeを閉じてから実行してください。"));
            return path;
        }

        private void BuildPreview()
        {
            previewReady = false;
            problem = null;
            try
            {
                string path = Validate();
                sourcePreview = Snapshot.Capture(source, true);
                destinationPreview = Snapshot.Capture(destination, false);
                dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString();
                differences.Clear();
                foreach (string key in sourcePreview.Values.Keys.Union(destinationPreview.Values.Keys).OrderBy(k => k, StringComparer.Ordinal))
                {
                    bool before = destinationPreview.Values.TryGetValue(key, out string oldValue);
                    bool after = sourcePreview.Values.TryGetValue(key, out string newValue);
                    if (!before) differences.Add(L10n.Text("追加 ") + key + "\n+ " + newValue);
                    else if (!after) differences.Add(L10n.Text("削除 ") + key + "\n− " + oldValue);
                    else if (oldValue != newValue) differences.Add(L10n.Text("変更 ") + key + "\n" + oldValue + " → " + newValue);
                }
                previewReady = true;
            }
            catch (Exception e) { problem = e.Message; }
        }

        private void Apply()
        {
            string path = null, backupFile = null;
            bool savingStarted = false;
            Scene temporaryScene = default;
            try
            {
                path = Validate();
                if (!sourcePreview.SameAs(Snapshot.Capture(source, true)) || !destinationPreview.SameAs(Snapshot.Capture(destination, false))
                    || dependencyHash != AssetDatabase.GetAssetDependencyHash(path).ToString())
                {
                    BuildPreview();
                    EditorUtility.DisplayDialog(L10n.Text("プレビューを更新しました"), L10n.Text("前回のプレビュー以降に変更がありました。差分を確認して再実行してください。"), "OK");
                    return;
                }
                if (!EditorUtility.DisplayDialog(L10n.Text("Prefabを全差分反映"), path + L10n.Text(" を上書きします。\nバックアップを先に作成します。通常のUndoでは元に戻せません。\nネストとVariantは展開されます。"), L10n.Text("バックアップして反映"), L10n.Text("キャンセル"))) return;

                // Back up the exact original bytes before any prefab write. The .meta preserves its GUID.
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string folder = Path.Combine(projectRoot, "PrefabFullSyncBackups", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(folder);
                backupFile = Path.Combine(folder, Path.GetFileName(path));
                File.Copy(path, backupFile, false);
                File.Copy(path + ".meta", backupFile + ".meta", false);
                File.WriteAllText(Path.Combine(folder, "Restore.txt"), "Original asset: " + path + "\nClose Prefab Mode before restoring. Copy both the .prefab and .prefab.meta files back to the original path, then refresh Unity.\n\nPreview:\n" + string.Join("\n", differences));
                lastBackup = folder;

                temporaryScene = EditorSceneManager.NewPreviewScene();
                // Instantiate into the preview scene so the source itself is never unpacked or edited.
                var holder = new GameObject("PrefabFullSync_Temporary");
                SceneManager.MoveGameObjectToScene(holder, temporaryScene);
                GameObject clone = Object.Instantiate(source, holder.transform, false);
                clone.name = source.name;
                clone.transform.SetParent(null, false);
                clone.transform.localPosition = source.transform.localPosition;
                clone.transform.localRotation = source.transform.localRotation;
                clone.transform.localScale = source.transform.localScale;
                foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                        PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                // ExecuteAlways callbacks must not silently change what the user reviewed.
                if (!sourcePreview.SameAs(Snapshot.Capture(clone, true)))
                    throw new InvalidOperationException(L10n.Text("複製時に値が変化しました（ExecuteAlways等）。上書きせず停止しました。"));
                savingStarted = true;
                PrefabUtility.SaveAsPrefabAsset(clone, path, out bool success);
                if (!success) throw new IOException(L10n.Text("Prefab保存に失敗しました。"));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                destination = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!destination || !sourcePreview.SameAs(Snapshot.Capture(destination, false)) || PrefabUtility.GetPrefabAssetType(destination) != PrefabAssetType.Regular)
                    throw new IOException(L10n.Text("保存結果がプレビューと一致しません。元のPrefabに戻します。"));
                savingStarted = false;
                BuildPreview();
                EditorUtility.DisplayDialog(L10n.Text("反映完了"), L10n.Text("Prefabを更新しました。\nバックアップ: ") + folder, "OK");
            }
            catch (Exception e)
            {
                problem = e.Message;
                if (savingStarted && backupFile != null)
                {
                    try
                    {
                        File.Copy(backupFile, path, true);
                        File.Copy(backupFile + ".meta", path + ".meta", true);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                        problem += L10n.Text("\n更新前のファイルに復元しました。");
                    }
                    catch (Exception restore) { problem += L10n.Text("\n自動復元に失敗しました: ") + restore.Message + L10n.Text("\nバックアップ: ") + backupFile; }
                }
                previewReady = false;
                Debug.LogError("[NekkoYa][PrefabFullSync] " + e.Message);
            }
            finally
            {
                if (temporaryScene.IsValid()) EditorSceneManager.ClosePreviewScene(temporaryScene);
            }
        }

        private sealed class Snapshot
        {
            public readonly SortedDictionary<string, string> Values = new SortedDictionary<string, string>(StringComparer.Ordinal);
            private readonly Dictionary<Object, string> identities = new Dictionary<Object, string>();
            private static readonly HashSet<string> Plumbing = new HashSet<string>
            {
                "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_GameObject",
                "m_Father", "m_Children", "m_RootOrder", "m_Component"
            };

            public bool SameAs(Snapshot other) => Values.Count == other.Values.Count && Values.All(p => other.Values.TryGetValue(p.Key, out string value) && value == p.Value);

            public static Snapshot Capture(GameObject root, bool rejectSceneReferences)
            {
                var result = new Snapshot();
                result.Index(root.transform, "Root");
                foreach (var pair in result.identities)
                {
                    var serialized = new SerializedObject(pair.Key);
                    var property = serialized.GetIterator();
                    var managedReferences = new Dictionary<long, string>();
                    bool descend = true;
                    while (property.Next(descend))
                    {
                        descend = true;
                        if (Plumbing.Contains(property.propertyPath)) { descend = false; continue; }
                        if (property.propertyType == SerializedPropertyType.Generic && property.hasChildren) continue;
                        if (property.propertyType == SerializedPropertyType.ManagedReference)
                        {
                            // Traverse SerializeReference fields, but stop at repeated/cyclic references.
                            long referenceId = property.managedReferenceId;
                            bool seen = managedReferences.TryGetValue(referenceId, out string firstPath);
                            if (!seen) managedReferences[referenceId] = firstPath = property.propertyPath;
                            result.Values[pair.Value + "/" + property.propertyPath] = property.managedReferenceFullTypename + " @ " + firstPath;
                            descend = !seen && property.hasChildren;
                            continue;
                        }
                        result.Values[pair.Value + "/" + property.propertyPath] = result.Value(property, rejectSceneReferences);
                        descend = false;
                    }
                }
                return result;
            }

            private void Index(Transform transform, string key)
            {
                identities.Add(transform.gameObject, key + "/GameObject");
                Values[key + "/$name"] = transform.name;
                Values[key + "/$children"] = transform.childCount.ToString();
                var components = transform.GetComponents<Component>();
                Values[key + "/$componentOrder"] = string.Join(" | ", components.Select(c => c ? c.GetType().AssemblyQualifiedName : "Missing"));
                var counts = new Dictionary<Type, int>();
                foreach (Component component in components)
                {
                    if (!component) throw new InvalidOperationException(key + L10n.Text(" にMissing Scriptがあります。先に修正してください。"));
                    Type type = component.GetType();
                    counts.TryGetValue(type, out int index);
                    counts[type] = index + 1;
                    identities.Add(component, key + "/" + type.FullName + "[" + index + "]");
                }
                for (int i = 0; i < transform.childCount; i++) Index(transform.GetChild(i), key + "/Child[" + i + "]");
            }

            private string Value(SerializedProperty p, bool rejectSceneReferences)
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                    case SerializedPropertyType.ExposedReference:
                        Object value = p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue : p.exposedReferenceValue;
                        if (!value)
                        {
                            if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceInstanceIDValue != 0)
                                throw new InvalidOperationException(p.propertyPath + L10n.Text(" に解決できない参照があります。"));
                            return "null";
                        }
                        if (identities.TryGetValue(value, out string id)) return L10n.Text("内部: ") + id;
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId) && EditorUtility.IsPersistent(value))
                            return L10n.Text("アセット: ") + AssetDatabase.GetAssetPath(value) + " [" + guid + ":" + localId + "]";
                        if (rejectSceneReferences) throw new InvalidOperationException(p.propertyPath + L10n.Text(" が反映元の外部を参照しています: ") + value.name + L10n.Text("。Prefabには保存できないため停止しました。"));
                        return L10n.Text("外部: ") + value.GetInstanceID();
                    case SerializedPropertyType.Integer: return p.longValue.ToString(CultureInfo.InvariantCulture);
                    case SerializedPropertyType.Boolean: return p.boolValue.ToString();
                    case SerializedPropertyType.Float: return p.doubleValue.ToString("R", CultureInfo.InvariantCulture);
                    case SerializedPropertyType.String: return p.stringValue;
                    case SerializedPropertyType.Enum: return p.intValue.ToString(CultureInfo.InvariantCulture);
                    case SerializedPropertyType.LayerMask: return p.intValue.ToString(CultureInfo.InvariantCulture);
                    case SerializedPropertyType.ArraySize: return p.intValue.ToString(CultureInfo.InvariantCulture);
                    case SerializedPropertyType.Character: return p.intValue.ToString(CultureInfo.InvariantCulture);
                    case SerializedPropertyType.Color: return JsonUtility.ToJson(new ColorValue { value = p.colorValue });
                    case SerializedPropertyType.Vector2: return JsonUtility.ToJson(new Vector2Value { value = p.vector2Value });
                    case SerializedPropertyType.Vector3: return JsonUtility.ToJson(new Vector3Value { value = p.vector3Value });
                    case SerializedPropertyType.Vector4: return JsonUtility.ToJson(new Vector4Value { value = p.vector4Value });
                    case SerializedPropertyType.Quaternion: return JsonUtility.ToJson(new QuaternionValue { value = p.quaternionValue });
                    case SerializedPropertyType.Rect: return JsonUtility.ToJson(new RectValue { value = p.rectValue });
                    case SerializedPropertyType.Bounds: return JsonUtility.ToJson(new BoundsValue { value = p.boundsValue });
                    case SerializedPropertyType.Vector2Int: return p.vector2IntValue.ToString();
                    case SerializedPropertyType.Vector3Int: return p.vector3IntValue.ToString();
                    case SerializedPropertyType.RectInt: return p.rectIntValue.ToString();
                    case SerializedPropertyType.BoundsInt: return p.boundsIntValue.ToString();
                    case SerializedPropertyType.AnimationCurve: return JsonUtility.ToJson(new CurveValue { value = p.animationCurveValue });
                    case SerializedPropertyType.Gradient:
                        var getter = typeof(SerializedProperty).GetProperty("gradientValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (getter == null) throw new NotSupportedException(L10n.Text("Gradientの読み取りに対応していないUnityです。"));
                        return JsonUtility.ToJson(new GradientValue { value = (Gradient)getter.GetValue(p) });
                    case SerializedPropertyType.Hash128: return p.hash128Value.ToString();
                    case SerializedPropertyType.ManagedReference: return p.managedReferenceFullTypename;
                    case SerializedPropertyType.Generic: return p.type + " (empty)";
                    default: throw new NotSupportedException(L10n.Text("未対応の保存形式: ") + p.propertyType + " / " + p.propertyPath);
                }
            }

            [Serializable] private struct ColorValue { public Color value; }
            [Serializable] private struct Vector2Value { public Vector2 value; }
            [Serializable] private struct Vector3Value { public Vector3 value; }
            [Serializable] private struct Vector4Value { public Vector4 value; }
            [Serializable] private struct QuaternionValue { public Quaternion value; }
            [Serializable] private struct RectValue { public Rect value; }
            [Serializable] private struct BoundsValue { public Bounds value; }
            [Serializable] private struct CurveValue { public AnimationCurve value; }
            [Serializable] private struct GradientValue { public Gradient value; }
        }
    }
}
#endif
