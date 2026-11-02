# NekkoYa Tools for Unity

Seven Editor tools targeting Unity 2022.3.22f1. The 1.0.0 candidate is **Unreleased / 1.0.0-pre.1**. Unity compilation, interactive verification and approval for a final release are pending.

## Tools

Open tools from the top-level `NekkoYa` menu. Tool names and window titles remain in English.

| Tool | Usage |
| --- | --- |
| BOOTH Description Studio | Enter product details, supported avatars, URLs, contents and update history. Review and copy the output; register products/presets and export/import JSON backups. |
| Asset Organizer | Add and reorder Project assets in Rename, preview `{00}` numbering and `{Name}` tokens, then rename. Use Revert Last Rename to undo the last batch. In Folder Generator, edit a tree and create only missing folders under Assets. |
| Blend Shape Name Presets | Save scene/Prefab Mode root values to JSON before updating an FBX. Load the JSON afterward, review added/removed/changed keys, then restore by name. |
| Shape Key List | Select a prefab or scene object to list and copy meshes and blend shapes, including inactive children. Does not edit assets. |
| VRC Data Checker | Select an Assets distribution folder, usage and PC/Mobile targets. Filter by category/severity/search, inspect recommendations, and copy/export Markdown reports. |
| Prefab Full Sync | Select a Hierarchy source and a destination prefab under Assets. Preview differences, confirm, then back up and apply. |
| PhysBone Organizer | Select a Hierarchy avatar, detect its armatures, choose a candidate or assign one manually. Review targets and scene-reference counts, then confirm moving PhysBones into PB under the avatar. |

## Install and migrate

1. Back up a test project using Unity 2022.3.22f1.
2. Back up old BOOTH JSON, blend shape presets, folder presets and other user data. Remove the old tool scripts before installing; do not import both versions together.
3. Use Package Manager → `+` → `Add package from git URL...`:

```text
https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity.git?path=/Packages/com.nekkoya.tools#refactor/physbone-organizer-v1.0.0
```

This is the unreleased development branch. The repository is private; GitHub permission and local Git authentication are required. Pin an approved commit/tag after final approval. Prioritizes Unity standard APIs without new third-party libraries. VRChat SDK, Modular Avatar and lilToon are optional for the first six tools; compatibility with specific versions has not been verified in Unity.

**PhysBone Organizer requires VRChat SDK Avatars / VRCPhysBone to run.** The first six tools do not require it. Without the SDK, the window opens but its operation is disabled. Specific SDK versions have not been verified in Unity. If `rootTransform` cannot be read, the operation stops before changing data.

Existing script `.meta` files, preset GUIDs, BOOTH storage/JSON format and blend shape JSON format/version are preserved. Folder presets include migration metadata for their former Editor assembly; verify actual Unity type migration. BOOTH continues using `BoothDescriptionStudio/library.json` under the OS LocalApplicationData folder.

## Settings

- `NekkoYa > Language > JP / EN / KR` is shared by all tools. Default: JP. Selection is persisted in EditorPrefs; only the current language is checked.
- Open windows repaint on language changes. Previously generated reports, restore previews and notices may retain their original language; rerun the check, refresh the preview or reload the list.
- Language changes do not rewrite product descriptions, terms, names, authored text or parser expressions. Original Japanese product templates remain Japanese.
- Asset Organizer: configure numbering, names, destination and folder presets. Copy package/read-only presets into Assets with `Save As` before editing.
- Blend Shape Name Presets: optionally zero newly added keys, only on renderers present in the saved preset.
- PhysBone Organizer: select the avatar and armature. Detects `Armature` / `アーマチュア` with optional numeric suffixes. Creates PB (1) if PB already exists and gives duplicate bone names unique holders. Empty Root Transform fields use the original bone; assigned roots, settings and current activation states are preserved.
- VRC Data Checker: configure exclusions, retained patterns, thresholds, recommended Android formats, dependency patterns and excluded properties. Existing settings remain at `ProjectSettings/NekkoyaVRCDataChecker.asset`.

## Protection and limitations

- Blend shape restoration and folder preset edits support Undo. Asset renames use the dedicated last-batch revert. Folder creation has no standard Undo: on failure only empty folders created by that operation are rolled back; existing folders and newly added contents are retained.
- Prefab Full Sync removes destination-only elements and unpacks nested/variant inheritance. Standard Undo cannot restore it. Read the confirmation and use the backup when needed.
- Prefab backups are stored under the project root in `PrefabFullSyncBackups`. Close Prefab Mode, copy both backed-up `.prefab` and `.prefab.meta` to the original paths, then Refresh Unity. Follow the included `Restore.txt`.
- VRC Data Checker does not modify checked assets. Unused candidates are not safe-to-delete guarantees and dynamic loading cannot be inferred. Detailed MA, Animation/Animator and VRC Parameters/Performance Rank checks remain unimplemented. Recommendations are not absolute VRChat limits.
- Routine operations avoid noisy logging. Error logs use `[NekkoYa][ToolName]`. OS/Unity exception messages may remain in their native language.

## Validation

PhysBone Organizer removes only the original components and keeps bone hierarchies. References from components in other open normal scenes are also updated; back up affected scenes. Assets, closed scenes, AnimationClip paths and future bone activation controls are not updated. Review and manually save scenes afterward. Play Mode, Prefab Mode and source selections across different scenes are unsupported. Failures revert with Undo; a failed rollback is reported rather than presented as success.

The unchanged PhysBone Organizer is stored at `Original/PhysBoneOrganizer/v0.1.0/NekkoYaPhysBoneOrganizer.cs` with its own `physbone-organizer-v0.1.0` tag.

Run repository script `scripts/verify_repository.py` for byte-for-byte original preservation, GUIDs, assembly references, translations and format placeholders. C# syntax parsing is separate from Unity compilation.

Run the retained VRC checker tests and `ToolRegressionTests` in Unity Test Runner, EditMode, in a test project. Add `com.nekkoya.tools` to the project's `Packages/manifest.json` `testables` list to discover package tests. See repository `VALIDATION.md` for the pending interactive checks.
