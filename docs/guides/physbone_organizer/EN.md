# PhysBone Organizer

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

Organize bone-attached PhysBones into a PB container directly under the avatar.

## Preparation

Requires VRCPhysBone from VRChat SDK Avatars. Use a normal scene and back up other open scenes containing references.

## Steps

1. Open `NekkoYa → PhysBone Organizer`.
2. Assign Avatar Root. Choose an automatically detected Armature/アーマチュア candidate, or set it manually.
3. Review Targets and the count of scene references to update.
4. Note that bone activation controls and AnimationClip paths are not migrated.
5. Run Organize PhysBones into PB and confirm count/scope.
6. Review settings/references/appearance under PB. Undo if needed, then manually save affected scenes.

## Settings

Creates PB (1) rather than reusing an existing PB. Empty Root Transform uses the original bone; assigned roots and current activation states remain.

## Result and recovery

Moves only PhysBone components, preserving the bone hierarchy. Also updates component references in other open normal scenes.

## Troubleshooting

SDK absence, Prefab/Play Mode and locked references prevent execution. Assets, closed scenes, animation paths and future activation controls need manual review. Re-review if targets change.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
