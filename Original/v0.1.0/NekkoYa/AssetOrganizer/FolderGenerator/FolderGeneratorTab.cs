using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NekkoYa.AssetOrganizer.Editor
{
    [Serializable]
    internal sealed class FolderGeneratorTab
    {
        [SerializeField] private DefaultAsset targetFolder;
        [SerializeField] private FolderPreset selectedPreset;
        [SerializeField] private List<FolderNode> workingRoots = new List<FolderNode>();
        [SerializeField] private Vector2 scroll;
        [SerializeField] private Vector2 previewScroll;

        [NonSerialized] private List<FolderPreset> presets = new List<FolderPreset>();
        [NonSerialized] private FolderNode selectedNode;
        [NonSerialized] private EditorWindow owner;
        [NonSerialized] private string resultMessage;
        [NonSerialized] private MessageType resultType = MessageType.Info;

        public void Initialize(EditorWindow window)
        {
            owner = window;
            if (workingRoots == null)
                workingRoots = new List<FolderNode>();
            RefreshPresets(false);
        }

        public void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawTargetFolder();
            EditorGUILayout.Space(8f);
            DrawPresetControls();
            EditorGUILayout.Space(8f);
            DrawTreeEditor();
            EditorGUILayout.Space(8f);
            DrawPreviewAndCreate();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetFolder()
        {
            EditorGUILayout.LabelField(L("Target Folder", "作成先フォルダー"), EditorStyles.boldLabel);
            DefaultAsset newTarget = (DefaultAsset)EditorGUILayout.ObjectField(targetFolder, typeof(DefaultAsset), false);
            if (newTarget != targetFolder)
                SetTargetIfFolder(newTarget);

            Rect dropRect = GUILayoutUtility.GetRect(0f, 42f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, L("Drag & Drop a Project Folder Here", "ここにProjectフォルダーをドラッグ＆ドロップ"), EditorStyles.helpBox);
            HandleFolderDrop(dropRect);
            if (GUILayout.Button(L("Use Selected Folder", "選択中のフォルダーを使用")))
                SetTargetIfFolder(Selection.activeObject as DefaultAsset);

            string targetPath = GetTargetPath();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(targetPath)
                    ? L("No valid target selected", "有効な作成先が選択されていません")
                    : targetPath,
                EditorStyles.miniLabel);
        }

        private void HandleFolderDrop(Rect rect)
        {
            Event current = Event.current;
            if (!rect.Contains(current.mousePosition) ||
                (current.type != EventType.DragUpdated && current.type != EventType.DragPerform))
                return;
            DefaultAsset folder = DragAndDrop.objectReferences.OfType<DefaultAsset>()
                .FirstOrDefault(candidate => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(candidate)));
            DragAndDrop.visualMode = folder == null ? DragAndDropVisualMode.Rejected : DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform && folder != null)
            {
                DragAndDrop.AcceptDrag();
                SetTargetIfFolder(folder);
            }
            current.Use();
        }

        private void SetTargetIfFolder(DefaultAsset candidate)
        {
            string path = candidate == null ? string.Empty : AssetDatabase.GetAssetPath(candidate);
            if (candidate != null && AssetDatabase.IsValidFolder(path) && path.StartsWith("Assets", StringComparison.Ordinal))
            {
                targetFolder = candidate;
                resultMessage = null;
            }
            else if (candidate != null)
            {
                resultMessage = L("Only folders inside Assets can be used as the target.",
                    "作成先にはAssets内のフォルダーだけを指定できます。");
                resultType = MessageType.Warning;
            }
        }

        private void DrawPresetControls()
        {
            EditorGUILayout.LabelField(L("Folder Preset", "フォルダープリセット"), EditorStyles.boldLabel);
            if (presets == null)
                RefreshPresets(false);

            string[] names = presets.Count == 0 ? new[] { L("(No presets)", "（プリセットなし）") } : presets.Select(preset => preset.name).ToArray();
            int currentIndex = selectedPreset == null ? -1 : presets.IndexOf(selectedPreset);
            int shownIndex = Mathf.Max(0, currentIndex);
            using (new EditorGUI.DisabledScope(presets.Count == 0))
            {
                int newIndex = EditorGUILayout.Popup(L("Preset", "プリセット"), shownIndex, names);
                if (presets.Count > 0 && (currentIndex < 0 || newIndex != currentIndex))
                    LoadPreset(presets[newIndex]);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("New", "新規")))
                CreateNewPreset();
            using (new EditorGUI.DisabledScope(selectedPreset == null))
            {
                if (GUILayout.Button(L("Save", "保存")))
                    SavePreset();
                if (GUILayout.Button(L("Save As", "名前を付けて保存")))
                    SaveAsPreset(selectedPreset == null
                        ? L("Folder Preset", "フォルダープリセット")
                        : selectedPreset.name + L(" Copy", " のコピー"));
                if (GUILayout.Button(L("Duplicate", "複製")))
                    SaveAsPreset(selectedPreset.name + L(" Copy", " のコピー"));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(selectedPreset == null))
            {
                if (GUILayout.Button(L("Rename Preset", "プリセット名を変更")))
                    RenamePreset();
                if (GUILayout.Button(L("Delete Preset", "プリセットを削除")))
                    DeletePreset();
                if (GUILayout.Button(L("Reset Changes", "変更をリセット")))
                    LoadPreset(selectedPreset);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(L(
                    "Tree edits affect only this working copy until Save is pressed. Presets are portable .asset files.",
                    "ツリーの編集内容は「保存」を押すまで保存済みプリセットへ反映されません。プリセットは持ち運び可能な.assetファイルです。"),
                MessageType.Info);
        }

        private void DrawTreeEditor()
        {
            EditorGUILayout.LabelField(L("Folder Structure", "フォルダー構成"), EditorStyles.boldLabel);
            if (workingRoots.Count == 0)
                EditorGUILayout.HelpBox(L("The tree is empty. Add a root folder.",
                    "ツリーが空です。ルートフォルダーを追加してください。"), MessageType.Info);
            for (int i = 0; i < workingRoots.Count; i++)
                DrawNode(workingRoots[i], 0);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("+ Root Folder", "+ ルート")))
            {
                var node = new FolderNode { Name = L("New Folder", "新しいフォルダー") };
                workingRoots.Add(node);
                selectedNode = node;
            }
            using (new EditorGUI.DisabledScope(selectedNode == null))
            {
                if (GUILayout.Button(L("+ Child", "+ 子フォルダー")))
                {
                    var child = new FolderNode { Name = L("New Folder", "新しいフォルダー") };
                    selectedNode.Children.Add(child);
                    selectedNode.Expanded = true;
                    selectedNode = child;
                }
                if (GUILayout.Button(L("Delete", "削除")))
                    DeleteSelectedNode();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(selectedNode == null))
            {
                if (GUILayout.Button("↑", GUILayout.Width(42f))) MoveSelected(-1);
                if (GUILayout.Button("↓", GUILayout.Width(42f))) MoveSelected(1);
                if (GUILayout.Button(L("→ Indent", "→ 子階層へ"))) IndentSelected();
                if (GUILayout.Button(L("← Outdent", "← 親階層へ"))) OutdentSelected();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNode(FolderNode node, int depth)
        {
            Rect row = EditorGUILayout.GetControlRect();
            row.xMin += depth * 18f;
            bool hasChildren = node.Children.Count > 0;
            Rect foldoutRect = new Rect(row.x, row.y, 18f, row.height);
            if (hasChildren)
                node.Expanded = EditorGUI.Foldout(foldoutRect, node.Expanded, GUIContent.none);
            Rect selectRect = new Rect(row.x + 18f, row.y, 24f, row.height);
            if (GUI.Toggle(selectRect, selectedNode == node, "●", EditorStyles.miniButton) && selectedNode != node)
                selectedNode = node;
            Rect nameRect = new Rect(row.x + 46f, row.y, row.width - 46f, row.height);
            node.Name = EditorGUI.TextField(nameRect, node.Name);
            if (node.Expanded)
            {
                foreach (FolderNode child in node.Children)
                    DrawNode(child, depth + 1);
            }
        }

        private void DrawPreviewAndCreate()
        {
            string targetPath = GetTargetPath();
            List<string> errors = FolderUtility.Validate(targetPath, workingRoots);
            List<FolderPreviewEntry> preview = string.IsNullOrEmpty(targetPath) || !AssetDatabase.IsValidFolder(targetPath)
                ? new List<FolderPreviewEntry>()
                : FolderUtility.BuildPreview(targetPath, workingRoots);

            EditorGUILayout.LabelField(L("Preview", "プレビュー"), EditorStyles.boldLabel);
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, EditorStyles.helpBox,
                GUILayout.MinHeight(100f), GUILayout.MaxHeight(260f));
            foreach (FolderPreviewEntry entry in preview)
            {
                string marker = entry.Exists ? "✓ " : "+ ";
                string state = entry.Exists ? L("  Existing", "  既存") : L("  Create", "  作成");
                EditorGUILayout.LabelField(new string(' ', entry.Depth * 3) + marker + entry.RelativePath + state);
            }
            EditorGUILayout.EndScrollView();

            foreach (string error in errors.Distinct())
                EditorGUILayout.HelpBox(error, MessageType.Error);
            if (!string.IsNullOrEmpty(resultMessage))
                EditorGUILayout.HelpBox(resultMessage, resultType);

            using (new EditorGUI.DisabledScope(errors.Count > 0 || preview.Count == 0))
            {
                if (GUILayout.Button(L("Create Folders", "フォルダーを作成"), GUILayout.Height(30f)) &&
                    EditorUtility.DisplayDialog(L("Create Folders", "フォルダー作成"),
                        L("Create only the missing folders shown in the preview? Existing folders will be kept.",
                            "プレビューに表示された不足フォルダーだけを作成しますか？既存フォルダーは維持されます。"),
                        L("Create", "作成"), L("Cancel", "キャンセル")))
                {
                    FolderCreateResult result = FolderUtility.CreateFolders(targetPath, workingRoots);
                    if (result.Errors.Count == 0)
                    {
                        resultMessage = L(
                            "Created " + result.CreatedCount + " folder(s); reused " + result.ExistingCount + " existing folder(s).",
                            result.CreatedCount + "個のフォルダーを作成し、既存の" + result.ExistingCount + "個を再利用しました。");
                        resultType = MessageType.Info;
                    }
                    else
                    {
                        resultMessage = L("Folder creation stopped: ", "フォルダー作成を中止しました: ") +
                                        string.Join("\n", result.Errors);
                        resultType = MessageType.Error;
                    }
                }
            }
        }

        private void CreateNewPreset()
        {
            var defaults = new List<FolderNode>
            {
                new FolderNode { Name = "Material" },
                new FolderNode { Name = "Prefab" },
                new FolderNode { Name = "Texture" }
            };
            string path = FolderPresetRepository.AskForPresetPath(L("New Folder Preset", "新規フォルダープリセット"));
            FolderPreset created = FolderPresetRepository.Create(path, defaults);
            if (created != null)
            {
                RefreshPresets(false);
                LoadPreset(created);
            }
        }

        private void SavePreset()
        {
            selectedPreset.Roots = FolderUtility.CloneTree(workingRoots);
            EditorUtility.SetDirty(selectedPreset);
            AssetDatabase.SaveAssets();
            resultMessage = L("Saved preset '" + selectedPreset.name + "'.",
                "プリセット「" + selectedPreset.name + "」を保存しました。");
            resultType = MessageType.Info;
        }

        private void SaveAsPreset(string defaultName)
        {
            string path = FolderPresetRepository.AskForPresetPath(defaultName);
            FolderPreset created = FolderPresetRepository.Create(path, workingRoots);
            if (created != null)
            {
                RefreshPresets(false);
                LoadPreset(created);
                resultMessage = L("Saved preset as '" + created.name + "'.",
                    "プリセットを「" + created.name + "」として保存しました。");
                resultType = MessageType.Info;
            }
        }

        private void RenamePreset()
        {
            FolderPreset preset = selectedPreset;
            TextPromptWindow.Open(L("Rename Preset", "プリセット名を変更"),
                L("Preset Name", "プリセット名"), preset.name, newName =>
            {
                string error = AssetNameValidator.Validate(newName, L("Preset name", "プリセット名"));
                if (error != null)
                {
                    resultMessage = error;
                    resultType = MessageType.Error;
                    owner?.Repaint();
                    return;
                }
                string renameError = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(preset), newName);
                resultMessage = string.IsNullOrEmpty(renameError)
                    ? L("Renamed preset to '" + newName + "'.", "プリセット名を「" + newName + "」へ変更しました。")
                    : renameError;
                resultType = string.IsNullOrEmpty(renameError) ? MessageType.Info : MessageType.Error;
                AssetDatabase.SaveAssets();
                RefreshPresets(false);
                owner?.Repaint();
            });
        }

        private void DeletePreset()
        {
            string path = AssetDatabase.GetAssetPath(selectedPreset);
            if (!EditorUtility.DisplayDialog(L("Delete Preset", "プリセットを削除"),
                    L("Delete preset asset '" + selectedPreset.name + "'?",
                        "プリセット「" + selectedPreset.name + "」を削除しますか？"),
                    L("Delete", "削除"), L("Cancel", "キャンセル")))
                return;
            if (!AssetDatabase.DeleteAsset(path))
            {
                resultMessage = L("Unity could not delete preset '" + path + "'.",
                    "Unityでプリセット「" + path + "」を削除できませんでした。");
                resultType = MessageType.Error;
                return;
            }
            selectedPreset = null;
            workingRoots.Clear();
            selectedNode = null;
            RefreshPresets(true);
        }

        private void RefreshPresets(bool selectFirst)
        {
            presets = FolderPresetRepository.LoadAll();
            if (selectedPreset != null && !presets.Contains(selectedPreset))
                selectedPreset = null;
            if (selectFirst && presets.Count > 0)
                LoadPreset(presets[0]);
        }

        private void LoadPreset(FolderPreset preset)
        {
            selectedPreset = preset;
            workingRoots = preset == null ? new List<FolderNode>() : FolderUtility.CloneTree(preset.Roots);
            selectedNode = null;
            resultMessage = null;
        }

        private void DeleteSelectedNode()
        {
            if (TryFindNode(selectedNode, out List<FolderNode> siblings, out int index, out _))
            {
                siblings.RemoveAt(index);
                selectedNode = null;
            }
        }

        private void MoveSelected(int direction)
        {
            if (!TryFindNode(selectedNode, out List<FolderNode> siblings, out int index, out _))
                return;
            int destination = index + direction;
            if (destination < 0 || destination >= siblings.Count)
                return;
            FolderNode node = siblings[index];
            siblings.RemoveAt(index);
            siblings.Insert(destination, node);
        }

        private void IndentSelected()
        {
            if (!TryFindNode(selectedNode, out List<FolderNode> siblings, out int index, out _) || index <= 0)
                return;
            FolderNode newParent = siblings[index - 1];
            siblings.RemoveAt(index);
            newParent.Children.Add(selectedNode);
            newParent.Expanded = true;
        }

        private void OutdentSelected()
        {
            if (!TryFindNode(selectedNode, out List<FolderNode> siblings, out int index, out FolderNode parent) || parent == null)
                return;
            if (!TryFindNode(parent, out List<FolderNode> parentSiblings, out int parentIndex, out _))
                return;
            siblings.RemoveAt(index);
            parentSiblings.Insert(parentIndex + 1, selectedNode);
        }

        private bool TryFindNode(FolderNode target, out List<FolderNode> siblings, out int index, out FolderNode parent)
        {
            return TryFindNodeRecursive(workingRoots, target, null, out siblings, out index, out parent);
        }

        private static bool TryFindNodeRecursive(List<FolderNode> nodes, FolderNode target, FolderNode currentParent,
            out List<FolderNode> siblings, out int index, out FolderNode parent)
        {
            index = nodes.IndexOf(target);
            if (index >= 0)
            {
                siblings = nodes;
                parent = currentParent;
                return true;
            }
            foreach (FolderNode node in nodes)
            {
                if (TryFindNodeRecursive(node.Children, target, node, out siblings, out index, out parent))
                    return true;
            }
            siblings = null;
            index = -1;
            parent = null;
            return false;
        }

        private string GetTargetPath()
        {
            return targetFolder == null ? string.Empty : AssetDatabase.GetAssetPath(targetFolder);
        }

        private static string L(string english, string japanese)
        {
            return OrganizerLocalization.Text(english, japanese);
        }
    }
}
