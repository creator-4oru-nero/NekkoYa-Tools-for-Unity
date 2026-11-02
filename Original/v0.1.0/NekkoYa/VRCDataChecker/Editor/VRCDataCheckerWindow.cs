using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nekkoya.VRCDataChecker
{
    public sealed class VRCDataCheckerWindow : EditorWindow
    {
        [SerializeField] private DefaultAsset folder;
        [SerializeField] private CheckTargetMode mode = CheckTargetMode.AvatarItem;
        [SerializeField] private TargetPlatformMode platforms = TargetPlatformMode.PC;
        private CheckContext report;
        private CheckResult selected;
        private Vector2 listScroll, detailScroll, categoryScroll, settingsScroll;
        private int severityFilter, category = -1, page;
        private bool showSettings, showTarget = true;
        private bool unusedView, previewPending;
        private readonly Dictionary<string, UnityEngine.Object> previewAssets = new Dictionary<string, UnityEngine.Object>();
        private string search = "", notice;
        private const int PageSize = 100;
        private static readonly string[] Modes = { "Avatarアイテム", "Worldアイテム", "完成Avatar", "汎用" };
        private static readonly string[] SeverityNames = { "エラー", "警告", "情報" };
        private static readonly string[] CategoryNames = { "全般", "参照・外部依存", "テクスチャ", "モデル", "メッシュ", "マテリアル", "シェーダー", "Prefab", "アニメーション", "Animator", "Modular Avatar", "VRChat", "音声", "パーティクル", "配布内容", "未使用候補" };
        private GUIStyle heading, muted, wrapped, rowTitle;

        public static void Open(DefaultAsset target = null)
        {
            var window = GetWindow<VRCDataCheckerWindow>("VRC配布データ検査");
            window.minSize = new Vector2(720, 560);
            if (target != null) { window.folder = target; window.showTarget = true; }
            window.Show();
        }
        private void OnEnable() { minSize = new Vector2(720, 560); }
        private void OnInspectorUpdate() { if (previewPending) Repaint(); }
        private void Styles()
        {
            heading = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            muted = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
            wrapped = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12, padding = new RectOffset(0, 0, 4, 8) };
            rowTitle = new GUIStyle(EditorStyles.boldLabel) { clipping = TextClipping.Clip };
        }
        private static Color SeverityColor(CheckSeverity severity)
        {
            if (severity == CheckSeverity.Error) return new Color(0.91f, 0.30f, 0.31f);
            if (severity == CheckSeverity.Warning) return new Color(0.91f, 0.65f, 0.22f);
            return new Color(0.32f, 0.64f, 0.87f);
        }
        private static string CategoryName(CheckCategory value) => CategoryNames[(int)value];
        private void ResetList() { page = 0; listScroll = Vector2.zero; selected = null; detailScroll = Vector2.zero; }
        private void OnGUI()
        {
            Styles();
            previewPending = false;
            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(12, 12, 10, 8) }))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("VRC配布データ検査", heading);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(showSettings ? "結果に戻る" : "設定", GUILayout.Width(96), GUILayout.Height(26))) showSettings = !showSettings;
                }
                GUILayout.Space(8);
                if (showSettings) { DrawSettings(); return; }
                DrawTarget();
                if (!string.IsNullOrEmpty(notice)) EditorGUILayout.HelpBox(notice, MessageType.Info);
                if (report == null)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("配布するフォルダを選んで検査", heading);
                    GUILayout.Label("参照切れ、Texture・Mesh設定、Prefabの問題を種類別に確認できます。", wrapped);
                    GUILayout.Label("検査ではAssetを変更しません。", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    return;
                }
                if (report.cancelled || report.results.Any(r => r.ruleId == "CHECK_INTERNAL_001"))
                    EditorGUILayout.HelpBox("一部の検査が未完了です。キャンセル／検査例外のある部分は結果に含まれないことがあります。", MessageType.Warning);
                int unusedCount = report.results.Count(r => r.category == CheckCategory.UnusedAsset);
                int tab = GUILayout.Toolbar(unusedView ? 1 : 0, new[] { "検査結果", "未使用候補  " + unusedCount + "件" }, GUILayout.Height(28));
                if ((tab == 1) != unusedView) { unusedView = tab == 1; category = -1; search = ""; ResetList(); }
                if (unusedView)
                    EditorGUILayout.HelpBox("Prefabなどから参照を辿れなかった候補です。動的な読み込みや素材配布に使う場合があります。クリックするとProject側でも選択します。", MessageType.Info);
                else DrawSummary();
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("検索", GUILayout.Width(32));
                    EditorGUI.BeginChangeCheck();
                    search = GUILayout.TextField(search, EditorStyles.toolbarSearchField);
                    if (EditorGUI.EndChangeCheck()) ResetList();
                    if (GUILayout.Button("クリア", EditorStyles.toolbarButton, GUILayout.Width(54))) { search = ""; ResetList(); }
                }
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
                {
                    if (!unusedView) DrawCategories();
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        // Narrow windows retain a usable list, with details below it.
                        if (position.width >= 1060)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Max(320, (position.width - 200) * 0.53f)))) DrawList();
                                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true))) DrawDetails();
                            }
                        }
                        else
                        {
                            DrawList();
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(Mathf.Clamp(position.height * 0.3f, 150, 260)))) DrawDetails();
                        }
                    }
                }
                DrawExport();
            }
        }
        private void DrawTarget()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    showTarget = EditorGUILayout.Foldout(showTarget, report == null ? "検査対象" : "検査対象  /  " + report.rootFolder, true);
                    if (report != null) GUILayout.Label(report.checkedAt.ToString("HH:mm") + " の結果", muted, GUILayout.Width(88));
                }
                if (!showTarget) return;
                using (new EditorGUILayout.HorizontalScope())
                {
                    folder = (DefaultAsset)EditorGUILayout.ObjectField(folder, typeof(DefaultAsset), false);
                    if (GUILayout.Button("選択中のフォルダ", GUILayout.Width(128)))
                    {
                        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                        if (AssetCollector.Validate(path) == null) { folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path); notice = null; }
                        else notice = "ProjectでAssets以下のフォルダを選択してください。";
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    mode = (CheckTargetMode)EditorGUILayout.Popup((int)mode, Modes, GUILayout.Width(150));
                    bool pc = GUILayout.Toggle((platforms & TargetPlatformMode.PC) != 0, "PC", GUILayout.Width(48));
                    bool mobile = GUILayout.Toggle((platforms & TargetPlatformMode.Mobile) != 0, "Mobile", GUILayout.Width(76));
                    platforms = (pc ? TargetPlatformMode.PC : 0) | (mobile ? TargetPlatformMode.Mobile : 0);
                    GUILayout.FlexibleSpace();
                    var path = AssetDatabase.GetAssetPath(folder);
                    var invalid = AssetCollector.Validate(path);
                    bool busy = EditorApplication.isCompiling || EditorApplication.isUpdating;
                    using (new EditorGUI.DisabledScope(invalid != null || busy || platforms == 0))
                        if (GUILayout.Button(busy ? "Import / Compile中" : "検査開始", GUILayout.Width(138), GUILayout.Height(28)))
                        {
                            try
                            {
                                report = CheckRunner.Run(path, mode, platforms, CheckerSettings.instance);
                                previewAssets.Clear(); unusedView = false;
                                severityFilter = report.results.Any(r => r.severity != CheckSeverity.Info) ? 0 : 3;
                                category = -1; search = ""; ResetList(); showTarget = false; notice = null;
                            }
                            catch (Exception e) { notice = e.Message; }
                        }
                }
                var reason = AssetCollector.Validate(AssetDatabase.GetAssetPath(folder));
                if (reason != null) GUILayout.Label(reason, muted);
                else if (platforms == 0) GUILayout.Label("対象プラットフォームを選択してください。", muted);
            }
        }
        private void DrawSummary()
        {
            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < 3; i++)
                {
                    var severity = (CheckSeverity)i;
                    int count = report.results.Count(r => r.severity == severity && r.category != CheckCategory.UnusedAsset);
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = SeverityColor(severity);
                    bool clicked = GUILayout.Toggle(severityFilter == i + 1, SeverityNames[i] + "   " + count + "件", "Button", GUILayout.Height(36));
                    GUI.backgroundColor = old;
                    if (clicked && severityFilter != i + 1) { severityFilter = i + 1; ResetList(); }
                }
            }
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(severityFilter == 0, "要確認のみ", EditorStyles.miniButtonLeft, GUILayout.Width(94)) && severityFilter != 0) { severityFilter = 0; ResetList(); }
                if (GUILayout.Toggle(severityFilter == 4, "すべて", EditorStyles.miniButtonRight, GUILayout.Width(72)) && severityFilter != 4) { severityFilter = 4; ResetList(); }
                GUILayout.FlexibleSpace();
                GUILayout.Label(severityFilter == 0 ? "エラー・警告を表示中。集計や外部パッケージ情報は「情報」へ。" : "「要確認のみ」でエラー・警告をまとめて確認", muted);
            }
            GUILayout.Space(8);
        }
        private bool Matches(CheckResult r) => (unusedView ? r.category == CheckCategory.UnusedAsset :
            r.category != CheckCategory.UnusedAsset && (severityFilter == 4 || (severityFilter == 0 ? r.severity != CheckSeverity.Info : (int)r.severity == severityFilter - 1))) &&
            (string.IsNullOrWhiteSpace(search) || (r.assetPath + " " + r.objectPath + " " + r.ruleId + " " + r.message).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        private void DrawCategories()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(162)))
            {
                GUILayout.Label("種類", EditorStyles.boldLabel);
                using (var view = new EditorGUILayout.ScrollViewScope(categoryScroll))
                {
                    categoryScroll = view.scrollPosition;
                    CategoryButton(-1, "すべての種類", report.results.Count(Matches));
                    foreach (CheckCategory value in Enum.GetValues(typeof(CheckCategory)))
                    {
                        int count = report.results.Count(r => r.category == value && Matches(r));
                        if (count > 0 || category == (int)value) CategoryButton((int)value, CategoryName(value), count);
                    }
                }
            }
        }
        private void CategoryButton(int value, string label, int count)
        {
            var style = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 6, 4, 4) };
            if (GUILayout.Toggle(category == value, label + "  " + count, style, GUILayout.Height(30)) && category != value) { category = value; ResetList(); }
        }
        private void DrawList()
        {
            var filtered = report.results.Where(r => Matches(r) && (category < 0 || (int)r.category == category))
                .OrderBy(r => r.severity).ThenBy(r => r.category).ThenBy(r => r.ruleId).ThenBy(r => r.assetPath).ToList();
            int pages = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
            page = Mathf.Clamp(page, 0, pages - 1);
            GUILayout.Label((unusedView ? "未使用候補" : category < 0 ? "検査結果" : CategoryName((CheckCategory)category)) + "  /  " + filtered.Count + "件", EditorStyles.boldLabel);
            using (var view = new EditorGUILayout.ScrollViewScope(listScroll, GUILayout.ExpandHeight(true)))
            {
                listScroll = view.scrollPosition;
                if (filtered.Count == 0) EditorGUILayout.HelpBox("この条件に該当する結果はありません。", MessageType.Info);
                string previous = null;
                foreach (var result in filtered.Skip(page * PageSize).Take(PageSize))
                {
                    string group = SeverityNames[(int)result.severity] + "  /  " + CategoryName(result.category);
                    if (!unusedView && previous != group) { GUILayout.Space(8); GUILayout.Label(group, EditorStyles.boldLabel); previous = group; }
                    DrawRow(result);
                }
            }
            if (pages > 1)
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    using (new EditorGUI.DisabledScope(page == 0)) if (GUILayout.Button("前へ", EditorStyles.toolbarButton)) { page--; listScroll = Vector2.zero; }
                    GUILayout.Label((page + 1) + " / " + pages, EditorStyles.centeredGreyMiniLabel);
                    using (new EditorGUI.DisabledScope(page >= pages - 1)) if (GUILayout.Button("次へ", EditorStyles.toolbarButton)) { page++; listScroll = Vector2.zero; }
                }
        }
        private void DrawRow(CheckResult result)
        {
            var rect = GUILayoutUtility.GetRect(0, 68, GUILayout.ExpandWidth(true));
            var background = selected == result ? new Color(0.20f, 0.43f, 0.65f, 0.32f) : EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.035f) : new Color(0, 0, 0, 0.035f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height - 3), background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height - 3), SeverityColor(result.severity));
            float inset = unusedView ? 76 : 12;
            if (unusedView) DrawPreview(new Rect(rect.x + 10, rect.y + 5, 56, 56), result.assetPath);
            GUI.Label(new Rect(rect.x + inset, rect.y + 5, rect.width - inset - 8, 18), result.title, rowTitle);
            string firstLine = (result.message ?? "").Split('\n')[0];
            GUI.Label(new Rect(rect.x + inset, rect.y + 25, rect.width - inset - 8, 18), new GUIContent(firstLine, firstLine), EditorStyles.label);
            GUI.Label(new Rect(rect.x + inset, rect.y + 45, rect.width - inset - 8, 16), new GUIContent(result.objectPath ?? result.assetPath, result.assetPath), muted);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                selected = result; detailScroll = Vector2.zero;
                var asset = PreviewAsset(result.assetPath);
                if (asset != null) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
            }
        }
        private UnityEngine.Object PreviewAsset(string path)
        {
            if (!previewAssets.TryGetValue(path, out var asset) || asset == null)
                previewAssets[path] = asset = AssetDatabase.LoadMainAssetAtPath(path);
            return asset;
        }
        private void DrawPreview(Rect rect, string path)
        {
            var asset = PreviewAsset(path);
            if (asset == null) { GUI.Label(rect, "プレビューなし", EditorStyles.centeredGreyMiniLabel); return; }
            Texture preview = asset as Texture2D;
            if (preview == null) preview = AssetPreview.GetAssetPreview(asset);
            if (preview == null)
            {
                previewPending |= AssetPreview.IsLoadingAssetPreview(asset.GetInstanceID());
                preview = AssetPreview.GetMiniThumbnail(asset);
            }
            if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            else GUI.Label(rect, "プレビューなし", EditorStyles.centeredGreyMiniLabel);
        }
        private void DrawDetails()
        {
            GUILayout.Label("詳細", EditorStyles.boldLabel);
            using (var view = new EditorGUILayout.ScrollViewScope(detailScroll))
            {
                detailScroll = view.scrollPosition;
                if (selected == null) { GUILayout.Space(12); GUILayout.Label("一覧の項目を選択すると、\n原因・推奨対応・参照元を表示します。", wrapped); return; }
                var badge = new GUIStyle(EditorStyles.boldLabel);
                badge.normal.textColor = SeverityColor(selected.severity);
                GUILayout.Label(SeverityNames[(int)selected.severity] + "  /  " + CategoryName(selected.category), badge);
                GUILayout.Label(selected.title, new GUIStyle(heading) { wordWrap = true });
                GUILayout.Label(selected.ruleId, muted);
                if (unusedView) DrawPreview(GUILayoutUtility.GetRect(100, 160, GUILayout.ExpandWidth(true)), selected.assetPath);
                GUILayout.Space(12);
                GUILayout.Label("検出内容", EditorStyles.boldLabel);
                GUILayout.Label(selected.message, wrapped);
                DrawTextureReferences(selected);
                if (selected.category == CheckCategory.Dependency && selected.textureReferences.Count == 0)
                    DrawDirectReferences(selected);
                GUILayout.Space(4);
                GUILayout.Label("推奨する対応", EditorStyles.boldLabel);
                GUILayout.Label(selected.recommendation, wrapped);
                GUILayout.Space(8);
                GUILayout.Label("対象", EditorStyles.boldLabel);
                SelectablePath(selected.assetPath);
                if (!string.IsNullOrEmpty(selected.objectPath)) { GUILayout.Label("Prefab内の場所", muted); SelectablePath(selected.objectPath); }
                if (!string.IsNullOrEmpty(selected.propertyPath)) { GUILayout.Label("プロパティ", muted); SelectablePath(selected.propertyPath); }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Projectで表示")) EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(selected.assetPath));
                    if (GUILayout.Button("選択")) Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(selected.assetPath);
                }
                if (selected.assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && GUILayout.Button("Prefabを開く")) AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(selected.assetPath));
                GUILayout.Space(16);
                if (selected.category != CheckCategory.Dependency || selected.textureReferences.Count > 0) DrawDirectReferences(selected);
            }
        }
        private void DrawTextureReferences(CheckResult result)
        {
            if (result.textureReferences.Count == 0) return;
            GUILayout.Space(8);
            GUILayout.Label("どのMaterialの、どの欄？", EditorStyles.boldLabel);
            foreach (var use in result.textureReferences.OrderBy(u => u.inactiveReason != null).ThenBy(u => u.materialPath))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(use.materialName, EditorStyles.boldLabel);
                    GUILayout.Label(use.label + (use.label == use.property ? "" : "  (" + use.property + ")"), wrapped);
                    GUILayout.Label(use.inactiveReason == null ? "画像が設定されています（有効、または機能のON/OFFを判定できない欄）" : "表示には不使用・参照だけ残存: " + use.inactiveReason, wrapped);
                    SelectablePath(use.materialPath);
                    if (GUILayout.Button("このMaterialを選択"))
                    {
                        var material = use.material != null ? use.material : AssetDatabase.LoadAssetAtPath<Material>(use.materialPath);
                        if (material != null) { Selection.activeObject = material; EditorGUIUtility.PingObject(material); }
                    }
                }
            }
        }
        private void DrawDirectReferences(CheckResult result)
        {
            GUILayout.Label("保存上の参照元  /  " + result.referencedBy.Count + "件", EditorStyles.boldLabel);
            foreach (string source in result.referencedBy)
                if (GUILayout.Button(new GUIContent(source, source), new GUIStyle(EditorStyles.linkLabel) { wordWrap = true })) Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(source);
            if (result.referencedBy.Count == 0) GUILayout.Label("直接参照しているAssetはありません。", muted);
        }
        private void SelectablePath(string path)
        {
            var style = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            float width = position.width >= 1060 ? Mathf.Max(200, (position.width - 200) * 0.47f - 50) : position.width - 220;
            EditorGUILayout.SelectableLabel(path, style, GUILayout.Height(Mathf.Max(20, style.CalcHeight(new GUIContent(path), width))));
        }
        private void DrawExport()
        {
            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("全結果を出力", muted, GUILayout.Width(88));
                if (GUILayout.Button("コピー", EditorStyles.toolbarButton, GUILayout.Width(60))) { EditorGUIUtility.systemCopyBuffer = ReportExporter.Markdown(report); notice = "全検査結果をコピーしました。"; }
                if (GUILayout.Button("Markdown保存", EditorStyles.toolbarButton, GUILayout.Width(112)))
                {
                    string destination = EditorUtility.SaveFilePanel("検査レポートを保存", "", "VRCDataCheckerReport.md", "md");
                    if (!string.IsNullOrEmpty(destination))
                        try { File.WriteAllText(destination, ReportExporter.Markdown(report), new UTF8Encoding(false)); notice = "保存しました: " + destination; }
                        catch (Exception e) { notice = "保存に失敗しました: " + e.Message; }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("結果をクリア", EditorStyles.toolbarButton, GUILayout.Width(100))) { report = null; ResetList(); showTarget = true; notice = null; }
            }
        }
        private void DrawSettings()
        {
            using (var view = new EditorGUILayout.ScrollViewScope(settingsScroll))
            {
                settingsScroll = view.scrollPosition;
                var settings = CheckerSettings.instance;
                GUILayout.Label("除外・保持対象", heading);
                GUILayout.Label("1行1パターン。* と ? が使用可能。参照切れ検査は除外しません。", wrapped);
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("通常の検査から除外", EditorStyles.boldLabel);
                settings.exclusions = EditorGUILayout.TextArea(settings.exclusions, GUILayout.Height(130));
                GUILayout.Space(12);
                GUILayout.Label("未使用候補にしない保持対象", EditorStyles.boldLabel);
                settings.keepPatterns = EditorGUILayout.TextArea(settings.keepPatterns, GUILayout.Height(80));
                settings.checkUnused = EditorGUILayout.Toggle("未使用候補を検査", settings.checkUnused);
                if (EditorGUI.EndChangeCheck()) settings.SaveSettings();
                GUILayout.Space(18);
                GUILayout.Label("判定の設定", heading);
                GUILayout.Label("本ツールの推奨値です。VRChatの絶対制限ではありません。", wrapped);
                using (var serialized = new SerializedObject(settings))
                {
                    serialized.Update();
                    EditorGUILayout.PropertyField(serialized.FindProperty("thresholds"), true);
                    EditorGUILayout.PropertyField(serialized.FindProperty("recommendedAndroidFormats"), new GUIContent("Android推奨形式"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("dependencyPatterns"), new GUIContent("外部依存分類"), true);
                    EditorGUILayout.PropertyField(serialized.FindProperty("ignoredReferenceProperties"), new GUIContent("参照検査除外Property"));
                    if (serialized.ApplyModifiedPropertiesWithoutUndo()) settings.SaveSettings();
                }
                GUILayout.Space(18);
                EditorGUILayout.HelpBox("対応範囲：依存関係・参照切れ・Texture・Mesh・Material・Prefab。MA詳細、Animation／Animator、VRC性能ランクは未実装です。", MessageType.Info);
            }
        }
    }
}
