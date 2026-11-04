> These tools are developed with AI (Codex). Use at your own risk.

# Unity User Guides

[日本語](README_JP.md) / [English](README_EN.md) / [한국어](README_KR.md) · [Repository README](../../README.md)

Development target: **Unity 2022.3.22f1**

| Tool | Purpose |
| --- | --- |
| [BOOTH Description Studio](booth_description_studio/EN.md) | Compose BOOTH descriptions and manage products, templates and registries. |
| [Asset Organizer](asset_organizer/EN.md) | Batch-rename Project assets and create folder trees from presets. |
| [Blend Shape Name Presets](blend_shape_name_presets/EN.md) | Save blend-shape values before an FBX update, then restore by names afterward. |
| [Shape Key List](shape_key_list/EN.md) | List mesh and blend-shape names from a prefab or scene hierarchy and copy them. |
| [VRC Data Checker](vrc_data_checker/EN.md) | Review distribution-folder references, textures, meshes, materials and prefabs. |
| [Prefab Full Sync](prefab_full_sync/EN.md) | Apply all differences from a Hierarchy source to a Project prefab with a backup. |
| [PhysBone Organizer](physbone_organizer/EN.md) | Organize bone-attached PhysBones into a PB container directly under the avatar. |

## Install

These guides describe the refactored package with a top-level NekkoYa menu. The current seven-tool development package is on `refactor/unity-v1.0.0`, not the original-script layout on main.

1. Back up a Unity 2022.3.22f1 project and avoid duplicate old scripts.
2. Use Package Manager → `+` → `Add package from git URL...`:

```text
https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity.git?path=/Packages/com.nekkoya.tools#refactor/unity-v1.0.0
```

3. Authenticate Git access to the private repository. Wait for compilation, then open NekkoYa.
4. Set the common language at `NekkoYa → Language → JP / EN / KR`.

The 1.0.0-pre.1 candidate has not been runtime-verified in Unity. Only PhysBone Organizer requires VRChat SDK Avatars / VRCPhysBone. A specific SDK compatibility version has not been established.

## Original version

Original 0.1.0 uses Japanese menu names under `Tools → NekkoYa`. Labels/safety behavior differ; refer to the [original sources](../../Original/v0.1.0) and their documentation. Do not install old and refactored scripts together.
