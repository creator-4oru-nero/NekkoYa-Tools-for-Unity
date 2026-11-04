# VRC Data Checker

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

Review distribution-folder references, textures, meshes, materials and prefabs.

## Preparation

Use a folder under Assets. Packages and external folders are unsupported.

## Steps

1. Open `NekkoYa → VRC Data Checker`.
2. Assign the folder or use Selected Folder from Project.
3. Choose usage such as Avatar/World Item and PC/Mobile platforms.
4. Wait for import/compilation, then Start Check. Note incomplete/cancelled status.
5. Filter severity/category/search and read causes, recommendations and references.
6. Show assets in Project/open prefabs, then copy or save Markdown results.
7. Adjust exclusions, retained patterns and thresholds in Settings, then rerun.

## Settings

Exclusion from ordinary checks does not skip dependency/missing-reference checks. Retain required materials explicitly. Recommendations are not absolute VRChat limits.

## Result and recovery

Displays/exports a report without changing checked assets.

## Troubleshooting

Unused candidates are not deletion permission; dynamic loads cannot be determined. Detailed MA, Animation/Animator and VRC Parameters/Performance Rank checks are unimplemented. Resolve incomplete-check causes and rerun.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
