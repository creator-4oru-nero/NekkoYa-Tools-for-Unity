# Shape Key List

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

List mesh and blend-shape names from a prefab or scene hierarchy and copy them.

## Preparation

Prepare a prefab or scene root. Inactive children are included.

## Steps

1. Open `NekkoYa → Shape Key List`.
2. Drag a Project or Hierarchy target into the prefab/object field.
3. Inspect mesh/shape counts and hierarchy paths.
4. Use Copy Name, Copy or Copy All for the required text.
5. Reload after changes to rebuild the list.

## Settings

Text can also be selected and copied directly. No target means no list.

## Result and recovery

Writes text to the clipboard only; assets, shape keys and weights are not edited.

## Troubleshooting

Check for MeshFilter/SkinnedMeshRenderer components with meshes. Meshes without shape keys are explicitly marked.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
