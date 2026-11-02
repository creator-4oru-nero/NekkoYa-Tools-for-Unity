using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    [Serializable]
    internal sealed class RenameTab
    {
        [SerializeField] private List<RenameItem> items = new List<RenameItem>();
        [SerializeField] private string template = "01_{00}_Asset_{Name}";
        [SerializeField] private int startNumber;
        [SerializeField] private Vector2 scroll;
        [SerializeField] private Vector2 previewScroll;

        [NonSerialized] private ReorderableList reorderableList;
        [NonSerialized] private EditorWindow owner;
        [NonSerialized] private List<RenamePlan> lastRename = new List<RenamePlan>();
        [NonSerialized] private string resultMessage;
        [NonSerialized] private MessageType resultType = MessageType.Info;

        public void Initialize(EditorWindow window)
        {
            owner = window;
            EnsureList();
        }

        public void OnGUI()
        {
            EnsureList();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawAssetList();
            EditorGUILayout.Space(8f);
            DrawTemplate();
            EditorGUILayout.Space(8f);
            DrawPreviewAndActions();
            EditorGUILayout.EndScrollView();
        }

        private void EnsureList()
        {
            if (items == null)
                items = new List<RenameItem>();
            if (reorderableList != null)
                return;

            reorderableList = new ReorderableList(items, typeof(RenameItem), true, true, false, true);
            reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect,
                L("Asset List (drag rows to reorder)", L10n.Text("アセット一覧（行をドラッグして並び替え）")));
            reorderableList.elementHeightCallback = _ => RenameUtility.UsesNameToken(template) ? 42f : 24f;
            reorderableList.drawElementCallback = DrawAssetRow;
            reorderableList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < items.Count)
                    items.RemoveAt(list.index);
                owner?.Repaint();
            };
            reorderableList.onReorderCallback = _ => owner?.Repaint();
        }

        private void DrawAssetRow(Rect rect, int index, bool active, bool focused)
        {
            if (index < 0 || index >= items.Count)
                return;
            RenameItem item = items[index];
            rect.y += 2f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect numberRect = new Rect(rect.x, rect.y, 28f, lineHeight);
            Rect objectRect = new Rect(rect.x + 30f, rect.y, rect.width * 0.62f, lineHeight);
            Rect typeRect = new Rect(objectRect.xMax + 4f, rect.y, rect.xMax - objectRect.xMax - 4f, lineHeight);
            EditorGUI.LabelField(numberRect, (index + 1).ToString("00"));
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.ObjectField(objectRect, item.Asset, typeof(UnityEngine.Object), false);
            EditorGUI.LabelField(typeRect, item.Asset == null ? L("Missing", L10n.Text("未設定")) : item.Asset.GetType().Name,
                EditorStyles.miniLabel);

            if (RenameUtility.UsesNameToken(template))
            {
                Rect nameRect = new Rect(rect.x + 30f, rect.y + lineHeight + 2f, rect.width - 30f, lineHeight);
                item.CustomName = EditorGUI.TextField(nameRect, "{Name}", item.CustomName);
            }
        }

        private void DrawAssetList()
        {
            reorderableList.DoLayoutList();
            Rect dropRect = GUILayoutUtility.GetRect(0f, 52f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, L("Drag & Drop Project Assets Here", L10n.Text("ここにProjectアセットをドラッグ＆ドロップ")), EditorStyles.helpBox);
            HandleAssetDrop(dropRect);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Add Selected Assets", L10n.Text("選択中のアセットを追加"))))
                AddAssets(Selection.objects);
            using (new EditorGUI.DisabledScope(items.Count == 0))
            {
                if (GUILayout.Button(L("Clear All", L10n.Text("すべてクリア"))))
                {
                    items.Clear();
                    resultMessage = null;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void HandleAssetDrop(Rect rect)
        {
            Event current = Event.current;
            if (!rect.Contains(current.mousePosition) ||
                (current.type != EventType.DragUpdated && current.type != EventType.DragPerform))
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddAssets(DragAndDrop.objectReferences);
            }
            current.Use();
        }

        private void AddAssets(IEnumerable<UnityEngine.Object> candidates)
        {
            var existing = new HashSet<UnityEngine.Object>(items.Where(item => item.Asset != null).Select(item => item.Asset));
            int skippedFolders = 0;
            foreach (UnityEngine.Object candidate in candidates)
            {
                if (candidate == null || existing.Contains(candidate))
                    continue;
                string path = AssetDatabase.GetAssetPath(candidate);
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                {
                    skippedFolders++;
                    continue;
                }
                items.Add(new RenameItem { Asset = candidate });
                existing.Add(candidate);
            }

            if (skippedFolders > 0)
            {
                resultMessage = L(
                    skippedFolders + " folder(s) were skipped. Folder rename is not supported.",
                    skippedFolders + L10n.Text("個のフォルダーを除外しました。フォルダー名の変更には対応していません。"));
                resultType = MessageType.Warning;
            }
            owner?.Repaint();
        }

        private void DrawTemplate()
        {
            EditorGUILayout.LabelField(L("Rename Template", L10n.Text("リネームテンプレート")), EditorStyles.boldLabel);
            template = EditorGUILayout.TextField(L("Template", L10n.Text("テンプレート")), template);
            startNumber = EditorGUILayout.IntField(L("Start Number", L10n.Text("開始番号")), startNumber);
            EditorGUILayout.HelpBox(L(
                    "Use {0}, {00}, {000}... for numbering and optional {Name} for each row. Extensions are preserved.",
                    L10n.Text("連番には {0}、{00}、{000}...、各行の個別名には {Name} を使用します。拡張子は維持されます。")),
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(items.Count == 0))
            {
                if (GUILayout.Button(L("Use Current Names", L10n.Text("現在の名前を使用"))))
                {
                    foreach (RenameItem item in items)
                    {
                        string path = item.Asset == null ? string.Empty : AssetDatabase.GetAssetPath(item.Asset);
                        item.CustomName = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileNameWithoutExtension(path);
                    }
                }
                if (GUILayout.Button(L("Edit Name List", L10n.Text("Nameリストを一括編集"))))
                    NameListEditorWindow.Open(items, () => owner?.Repaint());
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewAndActions()
        {
            var errors = new List<string>();
            if (items.Count == 0)
                errors.Add(L("Add at least one asset.", L10n.Text("アセットを1つ以上追加してください。")));
            List<RenamePlan> plans = RenameUtility.BuildPlans(items, template, startNumber, errors);

            EditorGUILayout.LabelField(L("Preview", L10n.Text("プレビュー")), EditorStyles.boldLabel);
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, EditorStyles.helpBox,
                GUILayout.MinHeight(90f), GUILayout.MaxHeight(230f));
            foreach (RenamePlan plan in plans)
            {
                EditorGUILayout.LabelField(Path.GetFileName(plan.OldPath), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  → " + plan.NewName + plan.Extension);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(L(plans.Count + " Assets", plans.Count + L10n.Text("個のアセット")));

            foreach (string error in errors.Distinct())
                EditorGUILayout.HelpBox(error, MessageType.Error);
            if (!string.IsNullOrEmpty(resultMessage))
                EditorGUILayout.HelpBox(resultMessage, resultType);

            using (new EditorGUI.DisabledScope(errors.Count > 0 || plans.Count == 0))
            {
                if (GUILayout.Button(L("Rename Assets", L10n.Text("アセット名を変更")), GUILayout.Height(30f)) &&
                    EditorUtility.DisplayDialog(L("Rename Assets", L10n.Text("アセット名の変更")),
                        L("Rename " + plans.Count + " asset(s) exactly as shown in the preview?",
                            L10n.Text("プレビューどおりに") + plans.Count + L10n.Text("個のアセット名を変更しますか？")),
                        L("Rename", L10n.Text("変更")), L("Cancel", L10n.Text("キャンセル"))))
                {
                    RenameResult result = RenameUtility.Execute(plans);
                    if (result.Errors.Count == 0)
                    {
                        lastRename = plans;
                        resultMessage = L("Renamed " + result.SuccessCount + " Assets.",
                            result.SuccessCount + L10n.Text("個のアセット名を変更しました。"));
                        resultType = MessageType.Info;
                    }
                    else
                    {
                        resultMessage = L("Rename failed. ", L10n.Text("リネームに失敗しました。")) + string.Join("\n", result.Errors);
                        resultType = MessageType.Error;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(lastRename == null || lastRename.Count == 0))
            {
                if (GUILayout.Button(L("Revert Last Rename", L10n.Text("直前のリネームを戻す"))) &&
                    EditorUtility.DisplayDialog(L("Revert Last Rename", L10n.Text("直前のリネームを戻す")),
                        L("Restore the names from the last successful rename?", L10n.Text("直前に変更した名前を元に戻しますか？")),
                        L("Revert", L10n.Text("元に戻す")), L("Cancel", L10n.Text("キャンセル"))))
                {
                    var revertErrors = new List<string>();
                    List<RenamePlan> revertPlans = RenameUtility.BuildRevertPlans(lastRename, revertErrors);
                    if (revertErrors.Count == 0)
                    {
                        RenameResult result = RenameUtility.Execute(revertPlans);
                        revertErrors.AddRange(result.Errors);
                    }
                    if (revertErrors.Count == 0)
                    {
                        resultMessage = L("Reverted " + revertPlans.Count + " Assets.",
                            revertPlans.Count + L10n.Text("個のアセット名を元に戻しました。"));
                        resultType = MessageType.Info;
                        lastRename.Clear();
                    }
                    else
                    {
                        resultMessage = L("Revert failed. ", L10n.Text("元に戻せませんでした。")) + string.Join("\n", revertErrors);
                        resultType = MessageType.Error;
                    }
                }
            }
        }

        private static string L(string english, string japanese)
        {
            return OrganizerLocalization.Text(english, japanese);
        }
    }
}
