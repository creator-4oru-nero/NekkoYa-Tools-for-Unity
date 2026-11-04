#if UNITY_EDITOR
using L10n = NekkoYa.Editor.NekkoYaLocalization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.BoothDescriptionStudio
{
    [Serializable] public class LinkEntry { public string id = Guid.NewGuid().ToString("N"), name = "", url = "", note = ""; public bool enabled; }
    [Serializable] public class QuantityChild { public string name = ""; public int count = 1; public bool enabled = true; }
    [Serializable] public class QuantityEntry { public string name = ""; public int count = 1; public bool enabled = true; public List<QuantityChild> children = new List<QuantityChild>(); }
    [Serializable] public class HistoryEntry { public string date = "", text = ""; }
    [Serializable] public class PrefabEntry { public string name = "", source = ""; public bool enabled = true; }
    [Serializable] public class Section { public string kind = "text", title = "", text = ""; public bool enabled = true; }
    [Serializable] public class Document
    {
        public string name = "新しい商品", category = "", number = "", divider = "────────── ✦ ──────────";
        public List<LinkEntry> avatars = new List<LinkEntry>(), downloads = new List<LinkEntry>();
        public List<QuantityEntry> quantities = new List<QuantityEntry>();
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();
        public List<HistoryEntry> history = new List<HistoryEntry>();
        public List<Section> sections = new List<Section>();
        public string ProductId { get { return string.IsNullOrEmpty(category) || string.IsNullOrEmpty(number) ? "" : category + "-" + number.PadLeft(3, '0'); } }
    }
    [Serializable] public class SavedDocument { public string id = Guid.NewGuid().ToString("N"), name = ""; public Document document; }
    [Serializable] public class Library
    {
        public int version = 1;
        public string activeProduct = "";
        public Document draft;
        public List<LinkEntry> avatars = new List<LinkEntry>(), downloads = new List<LinkEntry>();
        public List<SavedDocument> products = new List<SavedDocument>(), presets = new List<SavedDocument>();
    }
    public static class DescriptionFormat
    {
        public static T Clone<T>(T value) { return JsonUtility.FromJson<T>(JsonUtility.ToJson(value)); }
        public static string PrefabText(PrefabEntry entry, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(entry.source)) return "";
            string text = Regex.Replace(entry.source, @"&#x20;|&#32;|&nbsp;", " ", RegexOptions.IgnoreCase).Replace("\r", "");
            text = Regex.Replace(text, @"([■✦├└])", "\n$1");
            var poly = Regex.Match(text, @"総ポリゴン数\s*[：:]\s*([\d,]+)");
            var materials = Regex.Match(text, @"(?<!総)マテリアル数\s*[：:]\s*([\d,]+)");
            var texture = Regex.Match(text, @"テクスチャ数\s*[：:]\s*([\d,]+)");
            var meshes = Regex.Matches(text, @"[├└]\s*([^\r\n]+?)\s*[：:]\s*[△Δ]\s*([\d,]+)\s*/\s*[〇○]\s*([\d,]+)");
            var sizes = Regex.Matches(text, @"[├└]\s*([\d,]+)\s*[×xX]\s*([\d,]+)\s*[：:]\s*([\d,]+)\s*枚");
            if (!poly.Success || !materials.Success || !texture.Success) { error = L10n.Text("総ポリゴン数・マテリアル数・テクスチャ数を含む情報を貼り付けてください。"); return ""; }
            try
            {
                if ((long.Parse(poly.Groups[1].Value.Replace(",", "")) != 0 && meshes.Count == 0) || (long.Parse(texture.Groups[1].Value.Replace(",", "")) != 0 && sizes.Count == 0)) { error = L10n.Text("メッシュ別情報またはテクスチャの内訳を読み取れません。"); return ""; }
                var output = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(entry.name)) output.AppendLine("【" + entry.name.Trim() + "】");
                output.AppendLine("　✦総ポリゴン数：" + Number(poly.Groups[1].Value));
                output.AppendLine("　✦マテリアル数：" + Number(materials.Groups[1].Value));
                output.AppendLine(); output.AppendLine("　✦メッシュ別情報（△→三角ポリゴン数 / 〇→マテリアルスロット）");
                for (int i = 0; i < meshes.Count; i++) { var m = meshes[i]; output.AppendLine("　　" + (i == meshes.Count - 1 ? "└ " : "├ ") + m.Groups[1].Value.Trim() + "：△ " + Number(m.Groups[2].Value) + " / 〇 " + Number(m.Groups[3].Value)); }
                output.AppendLine(); output.AppendLine("　✦テクスチャ数：" + Number(texture.Groups[1].Value) + "枚");
                for (int i = 0; i < sizes.Count; i++) { var m = sizes[i]; output.AppendLine("　　" + (i == sizes.Count - 1 ? "└ " : "├ ") + Number(m.Groups[1].Value) + " × " + Number(m.Groups[2].Value) + "：" + Number(m.Groups[3].Value) + "枚"); }
                return output.ToString().TrimEnd();
            }
            catch (Exception e) when (e is FormatException || e is OverflowException) { error = L10n.Text("数値を読み取れません。入力を確認してください。"); return ""; }
        }
        static string Number(string s) { return long.Parse(s.Replace(",", ""), CultureInfo.InvariantCulture).ToString("N0", CultureInfo.InvariantCulture); }
        public static string Build(Document d, out List<string> errors)
        {
            errors = new List<string>(); var blocks = new List<string>();
            foreach (var section in d.sections.Where(s => s.enabled))
            {
                string body = "";
                if (section.kind == "avatars" || section.kind == "downloads")
                    body = string.Join("\n\n", (section.kind == "avatars" ? d.avatars : d.downloads).Where(a => a.enabled).Select(a => "　・" + a.name + (string.IsNullOrWhiteSpace(a.url) ? "" : "\n　　" + a.url) + (string.IsNullOrWhiteSpace(a.note) ? "" : "\n　　（" + a.note + "）")));
                else if (section.kind == "contents")
                {
                    var items = new List<string>();
                    foreach (var q in d.quantities.Where(q => q.enabled))
                    {
                        string row = "　✦" + q.name + "×" + q.count;
                        var children = q.children.Where(c => c.enabled).ToList();
                        for (int i = 0; i < children.Count; i++) row += "\n　　" + (i == children.Count - 1 ? "└ " : "├ ") + children[i].name + "×" + children[i].count;
                        items.Add(row);
                    }
                    body = string.Join("\n", items);
                    foreach (var prefab in d.prefabs.Where(p => p.enabled))
                    {
                        string error; string formatted = PrefabText(prefab, out error);
                        if (error.Length > 0) errors.Add((prefab.name.Length > 0 ? prefab.name : L10n.Text("Prefab情報")) + "：" + error);
                        if (formatted.Length > 0) body += (body.Length > 0 ? "\n\n" : "") + formatted;
                    }
                }
                else if (section.kind == "history")
                {
                    var rows = new List<string>();
                    foreach (var row in d.history.Where(h => h.date.Length > 0 || h.text.Length > 0))
                    {
                        DateTime date;
                        if (!DateTime.TryParseExact(row.date, new[] { "yyyy/MM/dd", "yyyy/M/d", "yyyy-MM-dd", "yyyy-M-d" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) errors.Add(L10n.Text("更新履歴の日付を確認してください：") + row.date);
                        rows.Add((date == default(DateTime) ? row.date : date.ToString("yyyy/MM/dd")) + "　" + row.text);
                    }
                    body = string.Join("\n", rows);
                }
                if (!string.IsNullOrWhiteSpace(section.text)) body += (body.Length > 0 ? "\n\n" : "") + section.text.Trim();
                if (body.Length > 0) blocks.Add((section.kind == "intro" ? "" : "◆" + section.title + "\n") + body);
            }
            return string.Join("\n\n" + (d.divider.Length > 0 ? d.divider + "\n\n" : ""), blocks).Replace("\r\n", "\n");
        }
    }

    public class BoothDescriptionWindow : EditorWindow
    {
        [SerializeField] Library library;
        [SerializeField] bool dirty, storageBlocked;
        bool showRegistry;
        double nextSave;
        Vector2 editScroll, previewScroll;
        string status = "", presetName = "";
        int productChoice, presetChoice;
        GUIStyle area, heading;
        public static string StoragePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BoothDescriptionStudio", "library.json"); } }
        Document Draft { get { return library.draft; } }

        [MenuItem("NekkoYa/BOOTH Description Studio")]
        public static void Open() { var window = GetWindow<BoothDescriptionWindow>("BOOTH Description Studio"); window.minSize = new Vector2(820, 560); window.Show(); }
        void OnEnable() { if (library == null || !dirty) Load(); hasUnsavedChanges = dirty; EditorApplication.update += Tick; }
        void OnDisable() { EditorApplication.update -= Tick; Save(); }
        void Tick() { if (dirty && EditorApplication.timeSinceStartup >= nextSave) { Save(); Repaint(); } }
        void Touch()
        {
            dirty = true;
            hasUnsavedChanges = true;
            saveChangesMessage = L10n.Text("未保存の商品データがあります。保存先を確認してから閉じてください。");
            nextSave = EditorApplication.timeSinceStartup + 0.7;
        }
        public override void SaveChanges()
        {
            if (Save()) base.SaveChanges();
        }
        public override void DiscardChanges()
        {
            dirty = false;
            base.DiscardChanges();
        }
        void Load()
        {
            try
            {
                library = File.Exists(StoragePath) ? JsonUtility.FromJson<Library>(File.ReadAllText(StoragePath)) : CreateLibrary();
                Validate(library); status = L10n.Text("PC内に自動保存");
            }
            catch (Exception e) { storageBlocked = true; library = CreateLibrary(); status = L10n.Text("保存データを開けません。元ファイルを保護して自動保存を停止しました：") + e.Message; }
        }
        public static void Validate(Library value)
        {
            if (value == null || value.version != 1 || value.draft == null || value.products == null || value.presets == null || value.avatars == null || value.downloads == null) throw new InvalidDataException(L10n.Text("対応する保存データではありません。"));
            foreach (var d in value.products.Concat(value.presets).Select(p => p.document).Concat(new[] { value.draft }))
                if (d == null || d.sections == null || d.avatars == null || d.downloads == null || d.quantities == null || d.prefabs == null || d.history == null) throw new InvalidDataException(L10n.Text("商品データが不完全です。"));
        }
        bool Save()
        {
            if (!dirty) return true;
            if (library == null || storageBlocked) return false;
            try
            {
                var active = library.products.FirstOrDefault(p => p.id == library.activeProduct);
                if (active != null && Draft.ProductId.Length > 0 && library.products.Any(p => p.id != active.id && p.document.ProductId == Draft.ProductId)) { status = L10n.Text("商品IDが重複しています。別の番号に変更してください。"); nextSave = EditorApplication.timeSinceStartup + 10; return false; }
                if (active != null) { active.document = DescriptionFormat.Clone(Draft); active.name = ProductLabel(Draft); }
                Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                string temp = StoragePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(library, true), new UTF8Encoding(false));
                if (File.Exists(StoragePath)) File.Replace(temp, StoragePath, StoragePath + ".bak"); else File.Move(temp, StoragePath);
                dirty = false; hasUnsavedChanges = false; status = L10n.Text("保存済み ") + DateTime.Now.ToString("HH:mm:ss");
                return true;
            }
            catch (Exception e) { nextSave = EditorApplication.timeSinceStartup + 10; status = L10n.Text("保存できません：") + e.Message; return false; }
        }
        static string ProductLabel(Document d) { return (d.ProductId.Length > 0 ? d.ProductId + "　" : "") + d.name; }
        void OnGUI()
        {
            if (library == null) return;
            if (area == null) { area = new GUIStyle(EditorStyles.textArea) { wordWrap = true }; heading = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 }; }
            EditorGUILayout.Space(8); EditorGUILayout.LabelField("BOOTH Description Studio", heading); EditorGUILayout.Space(5);
            if (storageBlocked) { EditorGUILayout.HelpBox(status, MessageType.Error); if (GUILayout.Button(L10n.Text("保存フォルダーを開く"))) EditorUtility.RevealInFinder(StoragePath); return; }
            string before = Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint ? JsonUtility.ToJson(library) : null;
            EditorGUI.BeginChangeCheck();
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.53f)))
                {
                    editScroll = EditorGUILayout.BeginScrollView(editScroll);
                    if (showRegistry) DrawRegistry(); else DrawEditor();
                    EditorGUILayout.EndScrollView();
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    List<string> errors; string output = DescriptionFormat.Build(Draft, out errors);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(L10n.Text("完成プレビュー　") + output.Length.ToString("N0") + L10n.Text("文字"), EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(errors.Count > 0)) if (GUILayout.Button(L10n.Text("全文コピー"), GUILayout.Width(100))) { EditorGUIUtility.systemCopyBuffer = output; status = L10n.Text("全文をコピーしました"); }
                    }
                    foreach (var error in errors) EditorGUILayout.HelpBox(error, MessageType.Warning);
                    previewScroll = EditorGUILayout.BeginScrollView(previewScroll, EditorStyles.helpBox);
                    float height = Mathf.Max(300, area.CalcHeight(new GUIContent(output), Mathf.Max(200, position.width * 0.43f)));
                    EditorGUILayout.SelectableLabel(output, area, GUILayout.MinHeight(height), GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                }
            }
            if (EditorGUI.EndChangeCheck() || (before != null && before != JsonUtility.ToJson(library))) Touch();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(status, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(L10n.Text("今すぐ保存"), EditorStyles.toolbarButton)) { Touch(); Save(); }
                if (GUILayout.Button(L10n.Text("バックアップ出力"), EditorStyles.toolbarButton)) Export();
                if (GUILayout.Button(L10n.Text("読み込み"), EditorStyles.toolbarButton)) Import();
            }
        }
        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(L10n.Text("商品"), GUILayout.Width(30));
                productChoice = EditorGUILayout.Popup(Mathf.Clamp(productChoice, 0, library.products.Count), new[] { L10n.Text("保存済みの商品を選択") }.Concat(library.products.Select(p => p.name)).ToArray(), GUILayout.MinWidth(160));
                using (new EditorGUI.DisabledScope(productChoice == 0)) if (GUILayout.Button(L10n.Text("開く"), GUILayout.Width(45))) { if (!Save()) return; library.activeProduct = library.products[productChoice - 1].id; library.draft = DescriptionFormat.Clone(library.products[productChoice - 1].document); Sync(); Touch(); GUI.FocusControl(null); }
                if (GUILayout.Button(L10n.Text("商品として登録"), GUILayout.Width(115))) RegisterProduct();
                if (GUILayout.Button(L10n.Text("複製"), GUILayout.Width(45))) { if (!Save()) return; library.draft = DescriptionFormat.Clone(Draft); library.draft.name += L10n.Text(" コピー"); library.draft.number = ""; library.activeProduct = ""; Touch(); }
                if (GUILayout.Button(showRegistry ? L10n.Text("編集に戻る") : L10n.Text("登録一覧"), GUILayout.Width(85))) showRegistry = !showRegistry;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.Text("プリセット"), GUILayout.Width(70));
                presetChoice = EditorGUILayout.Popup(Mathf.Clamp(presetChoice, 0, library.presets.Count), new[] { L10n.Text("プリセットを選択") }.Concat(library.presets.Select(p => p.name)).ToArray());
                using (new EditorGUI.DisabledScope(presetChoice == 0)) if (GUILayout.Button(L10n.Text("新規作成"), GUILayout.Width(75))) { if (!Save()) return; library.draft = DescriptionFormat.Clone(library.presets[presetChoice - 1].document); library.draft.name = L10n.Text("新しい商品"); library.activeProduct = ""; Sync(); Touch(); GUI.FocusControl(null); }
                presetName = EditorGUILayout.TextField(presetName, GUILayout.Width(140));
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(presetName))) if (GUILayout.Button(L10n.Text("プリセット保存"), GUILayout.Width(110))) SavePreset();
            }
            EditorGUILayout.Space(6);
        }
        void RegisterProduct()
        {
            if (string.IsNullOrWhiteSpace(Draft.name) || Draft.ProductId.Length == 0) { EditorUtility.DisplayDialog(L10n.Text("商品登録"), L10n.Text("商品名と、カテゴリー・番号を入力してください。"), "OK"); return; }
            var duplicate = library.products.FirstOrDefault(p => p.document.ProductId == Draft.ProductId && p.id != library.activeProduct);
            if (duplicate != null) { EditorUtility.DisplayDialog(L10n.Text("商品IDの重複"), L10n.Text("この商品IDは登録済みです。別の番号を入力してください。"), "OK"); return; }
            var active = library.products.FirstOrDefault(p => p.id == library.activeProduct);
            if (active == null) { active = new SavedDocument(); library.products.Add(active); library.activeProduct = active.id; }
            active.name = ProductLabel(Draft); active.document = DescriptionFormat.Clone(Draft); productChoice = library.products.IndexOf(active) + 1; Touch(); Save();
        }
        void SavePreset()
        {
            string name = presetName.Trim(); var existing = library.presets.FirstOrDefault(p => p.name == name);
            if (existing != null && !EditorUtility.DisplayDialog(L10n.Text("プリセット"), "「" + name + L10n.Text("」を現在の内容で更新しますか？"), L10n.Text("更新"), L10n.Text("キャンセル"))) return;
            var copy = DescriptionFormat.Clone(Draft); copy.name = L10n.Text("新しい商品"); copy.category = ""; copy.number = ""; copy.history.Clear();
            if (existing == null) { existing = new SavedDocument(); library.presets.Add(existing); }
            existing.name = name; existing.document = copy; Touch(); if (!Save()) return; status = L10n.Text("プリセット「") + name + L10n.Text("」を保存しました");
        }
        void DrawEditor()
        {
            EditorGUIUtility.labelWidth = 95;
            Draft.name = EditorGUILayout.TextField(L10n.Text("商品名（管理用）"), Draft.name);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(L10n.Text("商品ID"), GUILayout.Width(90));
                Draft.category = Regex.Replace(EditorGUILayout.TextField(Draft.category, GUILayout.Width(65)).ToUpperInvariant(), "[^A-Z]", "");
                GUILayout.Label("−", GUILayout.Width(12));
                Draft.number = Regex.Replace(EditorGUILayout.TextField(Draft.number, GUILayout.Width(70)), "[^0-9]", "");
                GUILayout.Label(Draft.ProductId, EditorStyles.miniLabel);
            }
            Draft.divider = EditorGUILayout.TextField(L10n.Text("区切り線"), Draft.divider);
            EditorGUILayout.Space(8);
            Section move = null; int moveTo = 0; Section delete = null;
            for (int i = 0; i < Draft.sections.Count; i++)
            {
                var s = Draft.sections[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        s.enabled = EditorGUILayout.ToggleLeft(s.title, s.enabled, EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("↑", GUILayout.Width(28))) { move = s; moveTo = i - 1; }
                        using (new EditorGUI.DisabledScope(i == Draft.sections.Count - 1)) if (GUILayout.Button("↓", GUILayout.Width(28))) { move = s; moveTo = i + 1; }
                        if (s.kind == "custom" && GUILayout.Button("×", GUILayout.Width(26))) delete = s;
                    }
                    if (!s.enabled) continue;
                    if (s.kind == "avatars") DrawLinks(Draft.avatars);
                    else if (s.kind == "downloads") DrawLinks(Draft.downloads);
                    else if (s.kind == "contents") DrawContents();
                    else if (s.kind == "history") DrawHistory();
                    if (s.kind == "custom") s.title = EditorGUILayout.TextField(L10n.Text("見出し"), s.title);
                    if (s.kind != "avatars" && s.kind != "downloads" && s.kind != "history")
                    {
                        if (s.kind == "contents") EditorGUILayout.LabelField(L10n.Text("内容物の補足"), EditorStyles.miniLabel);
                        s.text = EditorGUILayout.TextArea(s.text, area, GUILayout.MinHeight(s.kind == "contents" ? 45 : 85));
                    }
                }
            }
            if (move != null) { Draft.sections.Remove(move); Draft.sections.Insert(moveTo, move); Touch(); }
            if (delete != null) { Draft.sections.Remove(delete); Touch(); }
            if (GUILayout.Button(L10n.Text("＋ 自由項目を追加"))) { Draft.sections.Add(new Section { kind = "custom", title = L10n.Text("自由項目") }); Touch(); }
        }
        void DrawLinks(List<LinkEntry> links)
        {
            foreach (var link in links)
            {
                link.enabled = EditorGUILayout.ToggleLeft(link.name, link.enabled);
                if (link.enabled) { EditorGUILayout.LabelField(link.url.Length > 0 ? link.url : L10n.Text("URLなし"), EditorStyles.wordWrappedMiniLabel); link.note = EditorGUILayout.TextField(L10n.Text("商品ごとの補足"), link.note); }
            }
            if (GUILayout.Button(L10n.Text("登録一覧を編集"))) showRegistry = true;
        }
        void DrawContents()
        {
            QuantityEntry remove = null;
            foreach (var row in Draft.quantities)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    row.enabled = EditorGUILayout.Toggle(row.enabled, GUILayout.Width(18));
                    row.name = EditorGUILayout.TextField(row.name);
                    GUILayout.Label("×", GUILayout.Width(15)); row.count = Mathf.Max(1, EditorGUILayout.IntField(row.count, GUILayout.Width(55)));
                    if (GUILayout.Button("×", GUILayout.Width(24))) remove = row;
                }
                if (row.enabled)
                {
                    QuantityChild childRemove = null;
                    foreach (var child in row.children)
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(24); child.enabled = EditorGUILayout.Toggle(child.enabled, GUILayout.Width(18)); GUILayout.Label("└", GUILayout.Width(15)); child.name = EditorGUILayout.TextField(child.name); GUILayout.Label("×", GUILayout.Width(15)); child.count = Mathf.Max(1, EditorGUILayout.IntField(child.count, GUILayout.Width(55))); if (GUILayout.Button("×", GUILayout.Width(24))) childRemove = child;
                        }
                    if (childRemove != null) row.children.Remove(childRemove);
                    if (GUILayout.Button(L10n.Text("＋ 内訳"), EditorStyles.miniButton)) row.children.Add(new QuantityChild { name = "png" });
                }
                EditorGUILayout.Space(4);
            }
            if (remove != null) Draft.quantities.Remove(remove);
            if (GUILayout.Button(L10n.Text("＋ 内容物を追加"))) Draft.quantities.Add(new QuantityEntry { name = L10n.Text("追加の内容物") });
            PrefabEntry delete = null;
            foreach (var prefab in Draft.prefabs)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope()) { prefab.enabled = EditorGUILayout.ToggleLeft(L10n.Text("ポリゴン・テクスチャ情報"), prefab.enabled); if (GUILayout.Button(L10n.Text("削除"), GUILayout.Width(45))) delete = prefab; }
                    if (!prefab.enabled) continue;
                    prefab.name = EditorGUILayout.TextField(L10n.Text("表示名"), prefab.name);
                    EditorGUILayout.LabelField(L10n.Text("Prefab情報を貼り付け"), EditorStyles.miniLabel);
                    prefab.source = EditorGUILayout.TextArea(prefab.source, area, GUILayout.MinHeight(100), GUILayout.MaxHeight(160));
                    string error; DescriptionFormat.PrefabText(prefab, out error); if (error.Length > 0) EditorGUILayout.HelpBox(error, MessageType.Warning);
                }
            if (delete != null) Draft.prefabs.Remove(delete);
            if (GUILayout.Button(L10n.Text("＋ ポリゴン・テクスチャ情報を追加"))) Draft.prefabs.Add(new PrefabEntry());
        }
        void DrawHistory()
        {
            HistoryEntry remove = null;
            foreach (var row in Draft.history)
                using (new EditorGUILayout.HorizontalScope())
                {
                    row.date = EditorGUILayout.TextField(row.date, GUILayout.Width(100)); row.text = EditorGUILayout.TextField(row.text);
                    if (GUILayout.Button("×", GUILayout.Width(25))) remove = row;
                }
            if (remove != null) Draft.history.Remove(remove);
            EditorGUILayout.LabelField(L10n.Text("日付：yyyy/MM/dd　／　右欄：更新内容"), EditorStyles.miniLabel);
            if (GUILayout.Button(L10n.Text("＋ 更新履歴"))) Draft.history.Add(new HistoryEntry { date = DateTime.Today.ToString("yyyy/MM/dd") });
        }
        void DrawRegistry()
        {
            EditorGUILayout.HelpBox(L10n.Text("名前・URLは登録一覧で管理します。保存済みの商品本文は自動で書き換えません。"), MessageType.Info);
            DrawCatalog(L10n.Text("対応アバター"), library.avatars); DrawCatalog(L10n.Text("事前ダウンロード"), library.downloads);
            if (GUILayout.Button(L10n.Text("登録一覧を編集中の商品に反映"))) { Sync(true); Touch(); }
        }
        void DrawCatalog(string title, List<LinkEntry> entries)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            LinkEntry remove = null;
            foreach (var entry in entries)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope()) { entry.name = EditorGUILayout.TextField(entry.name); if (GUILayout.Button(L10n.Text("削除"), GUILayout.Width(45))) remove = entry; }
                    entry.url = EditorGUILayout.TextField(entry.url);
                }
            if (remove != null && EditorUtility.DisplayDialog(L10n.Text("登録の削除"), L10n.Text("登録一覧から削除します。保存済みの商品内の記載は残ります。"), L10n.Text("削除"), L10n.Text("キャンセル"))) entries.Remove(remove);
            if (GUILayout.Button("＋ " + title + L10n.Text("を登録"))) { entries.Add(new LinkEntry { name = L10n.Text("新しい項目") }); Touch(); }
            EditorGUILayout.Space(10);
        }
        void Sync(bool update = false)
        {
            SyncList(library.avatars, Draft.avatars, update); SyncList(library.downloads, Draft.downloads, update);
        }
        static void SyncList(List<LinkEntry> catalog, List<LinkEntry> target, bool update)
        {
            foreach (var row in catalog) { var match = target.FirstOrDefault(l => l.id == row.id); if (match == null) { match = DescriptionFormat.Clone(row); match.enabled = false; target.Add(match); } else if (update) { match.name = row.name; match.url = row.url; } }
        }
        void Export()
        {
            Touch(); Save(); string path = EditorUtility.SaveFilePanel(L10n.Text("登録情報と全商品をバックアップ"), "", "booth-description-backup", "json");
            if (path.Length == 0) return;
            try { File.WriteAllText(path, JsonUtility.ToJson(library, true), new UTF8Encoding(false)); status = L10n.Text("バックアップを保存しました"); } catch (Exception e) { EditorUtility.DisplayDialog(L10n.Text("保存エラー"), e.Message, "OK"); }
        }
        void Import()
        {
            string path = EditorUtility.OpenFilePanel(L10n.Text("バックアップを読み込み"), "", "json"); if (path.Length == 0) return;
            try
            {
                var imported = JsonUtility.FromJson<Library>(File.ReadAllText(path)); Validate(imported);
                if (!EditorUtility.DisplayDialog(L10n.Text("バックアップの読み込み"), L10n.Text("現在の登録一覧・プリセット・商品を置き換えます。続けますか？"), L10n.Text("読み込み"), L10n.Text("キャンセル"))) return;
                Touch(); if (!Save()) return; if (File.Exists(StoragePath)) File.Copy(StoragePath, StoragePath + ".before-import", true);
                library = imported; productChoice = presetChoice = 0; Touch(); Save();
            }
            catch (Exception e) { EditorUtility.DisplayDialog(L10n.Text("読み込みエラー"), e.Message, "OK"); }
        }
        public static Library CreateLibrary()
        {
            var l = new Library();
            string[] names = { "オリジナル3Dモデル「マヌカ」", "オリジナル3Dモデル「しなの」", "【オリジナル３Dモデル】ヘルベチカ", "オリオン -Orion-【オリジナル3Dモデル】", "【オリジナル3Dモデル】墨惺", "#Menno3D", "オリジナル3Dアバター：Alué（アルエ）", "3Dアバター『カルネ / KALNE』", "オリジナル3Dモデル『ミルティナ』", "プラム -Plum- / オリジナル3Dモデル", "サメっ子オリジナル3Dモデル「rurune」-ルルネ-", "森羅 Shinra", "水瀬 Minase", "オリジナル3Dモデル「輝」Original 3D Model「Tell」", "ダミアン -Damien-【オリジナル3Dモデル】", "【くうた-Kuuta-】オリジナル3Dモデル #Kuuta3D", "【オリジナル3Dモデル】狛乃-Komano-", "『Nyuma』ニューマ/オリジナル3Dモデル", "【無料／CC0】3D共通素体「SiroinoSotai」", "+Head アバター制作者向けVRChat用素体" };
            string[] ids = { "5058077", "6106863", "5405062", "6093194", "5727810", "6504220", "6957623", "5142722", "6538026", "7770415", "5957830", "", "", "6344701", "6689147", "4897493", "5260363", "6337376", "8268676", "5153266" };
            for (int i = 0; i < names.Length; i++) l.avatars.Add(new LinkEntry { name = names[i], url = ids[i].Length > 0 ? "https://booth.pm/ja/items/" + ids[i] : "" });
            l.downloads.Add(new LinkEntry { name = "Modular Avatar", url = "https://modular-avatar.nadena.dev/ja" });
            l.downloads.Add(new LinkEntry { name = "lilToon", url = "https://booth.pm/ja/items/3087170" });
            l.downloads.Add(new LinkEntry { name = "BlendShare - Triturbo", url = "https://triturbo.booth.pm/items/5901276" });
            l.downloads.Add(new LinkEntry { name = "Lume Optic 目シェーダーシステム", url = "https://booth.pm/ja/items/7416098" });
            foreach (var kind in new[] { "衣装", "アイテクスチャ", "シェイプキー" }) l.presets.Add(new SavedDocument { name = kind, document = CreateDocument(l, kind) });
            l.draft = DescriptionFormat.Clone(l.presets[1].document); return l;
        }
        static Document CreateDocument(Library l, string kind)
        {
            var d = new Document(); d.avatars = DescriptionFormat.Clone(l).avatars; d.downloads = DescriptionFormat.Clone(l).downloads;
            d.sections.Add(new Section { kind = "intro", title = "商品紹介" });
            d.sections.Add(new Section { kind = "avatars", title = "対応アバター" });
            d.sections.Add(new Section { kind = "downloads", title = "事前ダウンロード", enabled = kind != "アイテクスチャ" });
            d.sections.Add(new Section { kind = "contents", title = "内容物" });
            d.quantities.Add(new QuantityEntry { name = "UnityPackage" }); d.quantities.Add(new QuantityEntry { name = "Clipデータ", enabled = kind == "アイテクスチャ" }); d.quantities.Add(new QuantityEntry { name = "PSDデータ", enabled = false });
            if (kind == "アイテクスチャ") d.quantities[0].children.Add(new QuantityChild { name = "png" });
            d.sections.Add(new Section { kind = "text", title = "導入方法", enabled = kind != "アイテクスチャ", text = kind == "衣装" ? "Modular Avatarの設定済みのため、アバター直下にUnityPrefabを入れてください。" : "導入用ドキュメントをご確認ください。" });
            d.sections.Add(new Section { kind = "text", title = "概要", text = "VRCでの利用を想定しています。\n\nアバター本体のデータ及び、商品紹介の写真に利用した該当以外の3Dモデルは付属していません。\nまた、写真に利用している自作のアニメーション等も付属していません。" });
            d.sections.Add(new Section { kind = "text", title = "許可・禁止事項", text = "利用規約を必ずご確認ください。\n基本的には、対応アバターの規約に準じます。\n\n本モデルデータはVN3ライセンス（ https://www.vn3.org/ ）のテンプレートを使用しています。\n日本語版と多言語版に相違が生じた場合は、日本語の利用規約が優先されるものとします。\n本利用規約は、将来変更される可能性があります。\n\n利用規約（日本語）\nhttps://drive.google.com/file/d/16ZDwjXDC2lNFX9fJKqk82NpLqeRh_Yjv/view?usp=drive_link\n\nTerms of use（EN）\nhttps://drive.google.com/file/d/1I-LqezK9FaQctVCe5Q-p8ja1MLMiEQ2i/view?usp=drive_link" });
            d.sections.Add(new Section { kind = "history", title = "更新履歴" });
            d.sections.Add(new Section { kind = "text", title = "Special Thanks", enabled = false, text = "テスターおよびサムネの撮影にご協力いただいた皆様" });
            return d;
        }
    }
}
#endif
