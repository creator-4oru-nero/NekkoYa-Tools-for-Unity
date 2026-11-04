using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using L10n = NekkoYa.Editor.NekkoYaLocalization;
using Object = UnityEngine.Object;

namespace NekkoYa.PhysBoneOrganizer.Editor
{
    internal static class PhysBoneOrganizerOperations
    {
        internal const string PhysBoneTypeName = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone";

        internal sealed class Plan
        {
            internal Transform Avatar;
            internal Transform Armature;
            internal Type ComponentType;
            internal Component[] Sources;
            internal readonly Dictionary<Component, string> Snapshots = new Dictionary<Component, string>();
            internal readonly List<ReferenceUpdate> References = new List<ReferenceUpdate>();
        }

        internal sealed class ReferenceUpdate
        {
            internal Component Owner;
            internal string PropertyPath;
            internal Component Source;
        }

        internal static Type FindPhysBoneType()
        {
            // SDKをコンパイル時参照せず、未導入でも他のツールを利用可能にする。
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(PhysBoneTypeName, false))
                .FirstOrDefault(type => type != null && typeof(Component).IsAssignableFrom(type));
        }

        internal static Transform[] DetectArmatures(Transform avatar)
        {
            return avatar ? avatar.GetComponentsInChildren<Transform>(true)
                .Where(t => t != avatar && Regex.IsMatch(t.name,
                    @"^(Armature|アーマチュア)([._ -]?\d+| \(\d+\))?$", RegexOptions.IgnoreCase))
                .ToArray() : Array.Empty<Transform>();
        }

        internal static string Validate(Transform avatar, Transform armature, Type componentType)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return L10n.Text("Play Modeを終了してください。");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return L10n.Text("コンパイルとImportの終了を待ってから実行してください。");
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
                return L10n.Text("VRCPhysBoneが見つかりません。VRChat SDK Avatarsを導入してください。");
            if (!avatar) return L10n.Text("Hierarchyからアバター本体を指定してください。");
            if (!armature) return L10n.Text("対象アーマチュアを指定してください。");
            if (EditorUtility.IsPersistent(armature) || EditorUtility.IsPersistent(avatar))
                return L10n.Text("Project内のPrefabではなくHierarchy上のオブジェクトを指定してください。");
            var scene = avatar.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || scene != armature.gameObject.scene)
                return L10n.Text("同じ開いているシーン内のオブジェクトを指定してください。");
            if (EditorSceneManager.IsPreviewScene(scene))
                return L10n.Text("Prefab Modeでは実行できません。通常のシーンで使用してください。");
            if (avatar == armature || !armature.IsChildOf(avatar))
                return L10n.Text("アバター本体の中にあるアーマチュアを指定してください。");
            if (!avatar.gameObject.activeInHierarchy) return L10n.Text("アバター本体をアクティブにしてください。");
            if (!IsEditable(avatar) || !IsEditable(armature) || !CanEditScene(scene))
                return L10n.Text("対象が読み取り専用です。シーンとPrefabの編集権限を確認してください。");
            return null;
        }

        internal static Component[] Collect(Transform armature, Type componentType)
        {
            return armature && componentType != null ? armature.GetComponentsInChildren(componentType, true) : Array.Empty<Component>();
        }

        internal static Plan BuildPlan(Transform avatar, Transform armature, Type componentType)
        {
            string issue = Validate(avatar, armature, componentType);
            if (issue != null) throw new InvalidOperationException(issue);
            var plan = new Plan { Avatar = avatar, Armature = armature, ComponentType = componentType, Sources = Collect(armature, componentType) };
            if (plan.Sources.Length == 0) throw new InvalidOperationException(L10n.Text("対象のPhysBoneがありません。"));
            foreach (var source in plan.Sources)
            {
                if (!IsEditable(source)) throw new InvalidOperationException(L10n.Text("編集できないPrefabまたはコンポーネントが含まれています。"));
                using (var serialized = new SerializedObject(source))
                {
                    var root = serialized.FindProperty("rootTransform");
                    if (root == null || root.propertyType != SerializedPropertyType.ObjectReference)
                        throw new InvalidOperationException(L10n.Text("rootTransformを読み取れないPhysBoneがあります。SDKの互換性を確認してください。"));
                }
                plan.Snapshots[source] = EditorJsonUtility.ToJson(source);
            }

            var sourceSet = new HashSet<Component>(plan.Sources);
            // 他の開いたシーンからの参照も、元のツールと同じ範囲で引き継ぐ。
            // 編集できない参照を先に検出し、部分的な移動を始めない。
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (!component) continue;
                    using (var serialized = new SerializedObject(component))
                    {
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                                !(property.objectReferenceValue is Component reference) || !sourceSet.Contains(reference)) continue;
                            if (!property.editable || !IsEditable(component) || !CanEditScene(scene))
                                throw new InvalidOperationException(L10n.Text("更新できないPhysBone参照があります。参照元の編集権限を確認してください: ") + component.name);
                            plan.References.Add(new ReferenceUpdate { Owner = component, PropertyPath = property.propertyPath, Source = reference });
                            if (!plan.Snapshots.ContainsKey(component)) plan.Snapshots[component] = EditorJsonUtility.ToJson(component);
                        }
                    }
                }
            }
            return plan;
        }

        internal static GameObject Execute(Plan plan)
        {
            string issue = Validate(plan.Avatar, plan.Armature, plan.ComponentType);
            if (issue != null) throw new InvalidOperationException(issue);
            if (!Collect(plan.Armature, plan.ComponentType).SequenceEqual(plan.Sources) ||
                plan.Snapshots.Any(pair => !pair.Key || EditorJsonUtility.ToJson(pair.Key) != pair.Value))
                throw new InvalidOperationException(L10n.Text("確認後に対象または参照が変わりました。対象を再確認してから実行してください。"));
            // 確認画面を閉じた時点でも参照を再走査する。追加された参照や権限変更も見逃さない。
            var refreshed = BuildPlan(plan.Avatar, plan.Armature, plan.ComponentType);
            var reviewedReferences = new HashSet<(int Owner, string Path, int Source)>(plan.References
                .Select(update => (update.Owner.GetInstanceID(), update.PropertyPath, update.Source.GetInstanceID())));
            if (!reviewedReferences.SetEquals(refreshed.References
                .Select(update => (update.Owner.GetInstanceID(), update.PropertyPath, update.Source.GetInstanceID()))))
                throw new InvalidOperationException(L10n.Text("確認後に対象または参照が変わりました。対象を再確認してから実行してください。"));
            plan = refreshed;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize PhysBones");
            try
            {
                var container = CreateObject(UniqueName(plan.Avatar, "PB"), plan.Avatar);
                var replacements = new Dictionary<Component, Component>();
                foreach (var source in plan.Sources)
                {
                    var holder = CreateObject(UniqueName(container.transform, source.gameObject.name), container.transform);
                    Undo.RecordObject(holder, "Preserve PhysBone activation");
                    holder.SetActive(source.gameObject.activeInHierarchy);
                    var copy = Undo.AddComponent(holder, source.GetType());
                    if (!copy) throw new InvalidOperationException(L10n.Text("PhysBoneを追加できませんでした。SDKの互換性とConsoleを確認してください。"));
                    Undo.RecordObject(copy, "Copy PhysBone settings");
                    EditorUtility.CopySerialized(source, copy);
                    using (var serialized = new SerializedObject(copy))
                    {
                        var root = serialized.FindProperty("rootTransform");
                        if (root == null || root.propertyType != SerializedPropertyType.ObjectReference)
                            throw new InvalidOperationException(L10n.Text("rootTransformを読み取れないPhysBoneがあります。SDKの互換性を確認してください。"));
                        if (!root.objectReferenceValue) root.objectReferenceValue = source.transform;
                        serialized.ApplyModifiedProperties();
                    }
                    replacements.Add(source, copy);
                }

                // Source同士の参照は複製側で更新し、それ以外は既存の参照元を更新する。
                foreach (var update in plan.References)
                {
                    var owner = replacements.TryGetValue(update.Owner, out var copy) ? copy : update.Owner;
                    using (var serialized = new SerializedObject(owner))
                    {
                        var property = serialized.FindProperty(update.PropertyPath);
                        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference ||
                            property.objectReferenceValue != update.Source)
                            throw new InvalidOperationException(L10n.Text("確認後に対象または参照が変わりました。対象を再確認してから実行してください。"));
                        Undo.RecordObject(owner, "Remap PhysBone reference");
                        property.objectReferenceValue = replacements[update.Source];
                        serialized.ApplyModifiedProperties();
                        if (PrefabUtility.IsPartOfPrefabInstance(owner)) PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
                    }
                }
                foreach (var source in plan.Sources) Undo.DestroyObjectImmediate(source);
                Undo.RecordObject(container, "Activate PhysBone container");
                container.SetActive(true);
                // RecordObjectの差分を確定してからまとめ、失敗時のUndoでも同じ単位で戻せるようにする。
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                return container;
            }
            catch (Exception operation)
            {
                try
                {
                    Undo.FlushUndoRecordObjects();
                    Undo.RevertAllDownToGroup(group);
                }
                catch (Exception rollback)
                {
                    throw new InvalidOperationException(L10n.Text("Undoによる復元にも失敗しました。シーンを保存せず、Consoleとバックアップを確認してください。") + "\n" + operation.Message + "\n" + rollback.Message,
                        new AggregateException(operation, rollback));
                }
                throw new InvalidOperationException(L10n.Text("変更をUndoで取り消しました。原因を確認して再実行してください。") + "\n" + operation.Message, operation);
            }
        }

        private static bool IsEditable(Object target)
        {
            if (!target || (target.hideFlags & HideFlags.NotEditable) != 0 || PrefabUtility.IsPartOfImmutablePrefab(target)) return false;
            return !(target is Component component) || (component.gameObject.hideFlags & HideFlags.NotEditable) == 0;
        }

        private static bool CanEditScene(Scene scene)
        {
            return string.IsNullOrEmpty(scene.path) || (AssetDatabase.IsOpenForEdit(scene.path) &&
                File.Exists(scene.path) && (File.GetAttributes(scene.path) & FileAttributes.ReadOnly) == 0);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, parent.gameObject.scene);
            // 複製中は非アクティブに保ち、設定と参照が揃う前のSDKコールバックを抑える。
            created.SetActive(false);
            Undo.RegisterCreatedObjectUndo(created, "Create PhysBone container");
            Undo.SetTransformParent(created.transform, parent, "Parent PhysBone container");
            Undo.RecordObject(created.transform, "Reset PhysBone container transform");
            created.transform.localPosition = Vector3.zero;
            created.transform.localRotation = Quaternion.identity;
            created.transform.localScale = Vector3.one;
            return created;
        }

        internal static string UniqueName(Transform parent, string original)
        {
            var names = new HashSet<string>(parent.Cast<Transform>().Select(child => child.name), StringComparer.Ordinal);
            string candidate = original;
            for (int i = 1; names.Contains(candidate); i++) candidate = original + " (" + i + ")";
            return candidate;
        }
    }
}
