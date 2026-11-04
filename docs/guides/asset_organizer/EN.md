# Asset Organizer

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

Batch-rename Project assets and create folder trees from presets.

## Preparation

Back up the project. Rename only main assets under Assets; folders and package assets are excluded.

## Steps

1. Open `NekkoYa → Asset Organizer`, Rename tab.
2. Drag Project assets in or add selected assets; drag rows to reorder.
3. Set a template: `{00}` is numbering and `{Name}` is per-row text. `{00}_{Name}`, start 7, Body produces `07_Body`.
4. Fill/edit the Name list and review destination previews and conflicts.
5. Run Rename. Use Revert Last Rename for the last batch; extensions remain.
6. For folders, switch to Folder Generator and select a destination under Assets.
7. Create a New preset, or select one and Save As into Assets.
8. Add roots/children, edit hierarchy/names and Save the preset.
9. Review missing/existing folders and Create Folders; existing folders are reused.

## Settings

Configure numbering, template, Name list and folder presets. Package/read-only presets cannot be saved, renamed or deleted directly.

## Result and recovery

Updates Project asset paths/folders. Working tree edits are not stored in the preset until Save.

## Troubleshooting

Standard Undo does not restore file operations. Rename uses dedicated revert; failed folder creation removes only newly created empty folders. Revert may refuse assets renamed again; inspect paths and backups.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
