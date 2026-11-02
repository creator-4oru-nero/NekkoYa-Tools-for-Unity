using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using L10n = NekkoYa.Editor.NekkoYaLocalization;

namespace NekkoYa.PhysBoneOrganizer.Editor
{
    public sealed class PhysBoneOrganizerWindow : EditorWindow
    {
        [SerializeField] private Transform armature;
        [SerializeField] private Transform avatar;
        private Transform[] candidates = Array.Empty<Transform>();
        private Component[] preview = Array.Empty<Component>();
        private Vector2 scroll;
        private string notice;
        private MessageType noticeType;

        [MenuItem("NekkoYa/PhysBone Organizer", false, 700)]
        private static void Open()
        {
            var window = GetWindow<PhysBoneOrganizerWindow>("PhysBone Organizer");
            window.minSize = new Vector2(560, 440);
        }

        private void OnEnable()
        {
            candidates = PhysBoneOrganizerOperations.DetectArmatures(avatar);
            Undo.undoRedoPerformed += Invalidate;
            EditorApplication.hierarchyChanged += Invalidate;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Invalidate;
            EditorApplication.hierarchyChanged -= Invalidate;
        }

        private void Invalidate()
        {
            preview = Array.Empty<Component>();
            candidates = PhysBoneOrganizerOperations.DetectArmatures(avatar);
            Repaint();
        }

        private void DetectArmatures()
        {
            candidates = PhysBoneOrganizerOperations.DetectArmatures(avatar);
            armature = candidates.Length == 1 ? candidates[0] : null;
            preview = Array.Empty<Component>();
            notice = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("PhysBone Organizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10n.Text("アバター本体を指定すると、中のアーマチュアを自動検出します。ボーンに付いているPhysBoneを、アバター直下のPBにまとめます。"), MessageType.Info);
            EditorGUI.BeginChangeCheck();
            avatar = (Transform)EditorGUILayout.ObjectField(L10n.Text("アバター本体"), avatar, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) DetectArmatures();
            using (new EditorGUI.DisabledScope(!avatar))
                if (GUILayout.Button(L10n.Text("アーマチュアを再検出"))) DetectArmatures();
            if (avatar && candidates.Length == 0)
                EditorGUILayout.HelpBox(L10n.Text("アーマチュアが見つかりません。下の欄に手動で指定してください。"), MessageType.Info);
            if (candidates.Length > 1)
            {
                EditorGUILayout.HelpBox(L10n.Text("候補が複数あります。今回まとめるアーマチュアを選んでください。"), MessageType.Info);
                int current = Array.IndexOf(candidates, armature);
                var labels = new[] { L10n.Text("候補を選択…") }.Concat(candidates.Select(t => t ? AnimationUtility.CalculateTransformPath(t, avatar) : L10n.Text("（削除済み）"))).ToArray();
                EditorGUI.BeginChangeCheck();
                int selected = EditorGUILayout.Popup(L10n.Text("検出した候補"), current + 1, labels);
                if (EditorGUI.EndChangeCheck()) { armature = selected > 0 ? candidates[selected - 1] : null; preview = Array.Empty<Component>(); notice = null; }
            }
            EditorGUI.BeginChangeCheck();
            armature = (Transform)EditorGUILayout.ObjectField(L10n.Text("対象アーマチュア"), armature, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) { preview = Array.Empty<Component>(); notice = null; }
            EditorGUILayout.HelpBox(L10n.Text("作成する構成: アバター本体 / PB / 元のオブジェクト名\n既存のPBがある場合はPB (1)などを新規作成します。Root Transformが空なら元のボーンを設定し、設定済みならそのまま維持します。"), MessageType.None);
            EditorGUILayout.HelpBox(L10n.Text("ボーンのON/OFFやAnimationClipによる制御は自動で引き継ぎません。これらを使用している場合は整理後に確認してください。"), MessageType.Warning);
            EditorGUILayout.HelpBox(L10n.Text("開いている他のシーン内のコンポーネント参照も更新します。アセット・閉じたシーン・アニメーションのパスは更新しません。"), MessageType.Warning);

            string issue;
            var componentType = PhysBoneOrganizerOperations.FindPhysBoneType();
            try { issue = PhysBoneOrganizerOperations.Validate(avatar, armature, componentType); }
            catch (Exception exception) { issue = exception.Message; }
            if (issue != null) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            using (new EditorGUI.DisabledScope(issue != null))
            {
                if (GUILayout.Button(L10n.Text("対象を確認")))
                {
                    try
                    {
                        var plan = PhysBoneOrganizerOperations.BuildPlan(avatar, armature, componentType);
                        preview = plan.Sources;
                        notice = string.Format(L10n.Text("更新するシーン内の参照: {0} 個"), plan.References.Count);
                        noticeType = MessageType.Info;
                    }
                    catch (Exception exception) { ShowError(exception); }
                }
                if (GUILayout.Button(L10n.Text("PhysBoneをPBにまとめる（Undo対応）"))) Organize(componentType);
            }
            if (!string.IsNullOrEmpty(notice)) EditorGUILayout.HelpBox(notice, noticeType);
            EditorGUILayout.LabelField(string.Format(L10n.Text("確認対象: {0} 個"), preview.Count(component => component)));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var component in preview)
                if (component) EditorGUILayout.ObjectField(component, typeof(Component), true);
            EditorGUILayout.EndScrollView();
        }

        private void Organize(Type componentType)
        {
            try
            {
                var plan = PhysBoneOrganizerOperations.BuildPlan(avatar, armature, componentType);
                preview = plan.Sources;
                string message = string.Format(L10n.Text("{0} 個のPhysBoneをPB配下の専用オブジェクトにまとめます。\n元のボーンからはPhysBoneコンポーネントだけを外します。\n開いたシーン内の参照 {1} 個を更新します。\nアニメーション制御は引き継ぎません。シーンは自動保存しません。"), plan.Sources.Length, plan.References.Count);
                if (!EditorUtility.DisplayDialog("PhysBone Organizer", message, L10n.Text("まとめる"), L10n.Text("キャンセル"))) return;
                int count = plan.Sources.Length;
                var folder = PhysBoneOrganizerOperations.Execute(plan);
                preview = Array.Empty<Component>();
                notice = string.Format(L10n.Text("{0} 個のPhysBoneを {1}/{2} にまとめました。\nCtrl+Zで戻せます。確認後にシーンを保存してください。"), count, avatar.name, folder.name);
                noticeType = MessageType.Info;
            }
            catch (Exception exception) { ShowError(exception); }
        }

        private void ShowError(Exception exception)
        {
            preview = Array.Empty<Component>();
            notice = L10n.Text("整理を実行できませんでした。原因を確認してください。") + "\n" + exception.Message;
            noticeType = MessageType.Error;
            Debug.LogError("[NekkoYa][PhysBoneOrganizer] " + exception.Message);
        }
    }
}
