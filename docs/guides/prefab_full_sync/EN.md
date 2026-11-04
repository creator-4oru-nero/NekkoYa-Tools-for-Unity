# Prefab Full Sync

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

Apply all differences from a Hierarchy source to a Project prefab with a backup.

## Preparation

Try a cloned project first. Save the destination and close its Prefab Mode. Standard Undo cannot restore this operation.

## Steps

1. Open `NekkoYa → Prefab Full Sync`.
2. Assign a normal-scene source root and a destination prefab-asset root under Assets.
3. Preview Differences and review additions/removals/changes. Destination-only elements are deleted.
4. Confirm that nested/variant inheritance is unpacked and root name/local Transform are replaced.
5. Back Up and Apply All Differences, then read and approve the confirmation.
6. Inspect the result/backup. To restore, close Prefab Mode, copy both .prefab and .prefab.meta to original paths and Refresh Unity.

## Settings

Backups live in project-root `PrefabFullSyncBackups`. Open Latest Backup Folder and each Restore.txt explain recovery.

## Result and recovery

Overwrites the destination prefab. Materials/meshes themselves are not edited, but internal fileID changes may affect external references.

## Troubleshooting

Resolve read-only state, unsaved edits, missing scripts and unsaveable references first. Re-review refreshed previews after changes. If automatic restore fails, inspect the backup before saving.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
