# Blend Shape Name Presets

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

Save blend-shape values before an FBX update, then restore by names afterward.

## Preparation

Use a scene root or an object opened in Prefab Mode. Save before updating the FBX; a Project prefab asset alone is not the editable target.

## Steps

1. Open `NekkoYa → Blend Shape Name Presets` and assign the root.
2. Save Current Values to JSON; inactive children and zero values are included.
3. After updating the FBX, assign the same root and Open Saved JSON.
4. Review matched/added/removed/changed values and resolve hierarchy errors.
5. Choose whether to zero newly added keys on saved renderers; OFF keeps current values.
6. Restore by Name, inspect the result and save the scene/prefab.

## Settings

Matches names and renderer hierarchy. Removed keys are skipped; unsaved renderers are left unchanged.

## Result and recovery

Restores corresponding blend-shape values with Undo. It does not write the JSON into the FBX.

## Troubleshooting

Duplicate siblings/key names or unmatched renderers stop ambiguous restoration. Check hierarchy and the selected JSON before proceeding.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
