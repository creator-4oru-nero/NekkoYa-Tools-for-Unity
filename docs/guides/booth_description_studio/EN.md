# BOOTH Description Studio

[All guides](../README_EN.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

Development target: **Unity 2022.3.22f1**

Development-build guide; runtime verification in Unity / VRChat SDK is pending.

## Purpose

Compose BOOTH descriptions and manage products, templates and registries.

## Preparation

Back up existing JSON. UI language does not automatically translate product text.

## Steps

1. Open `NekkoYa → BOOTH Description Studio`.
2. Choose a preset and Create New, or enter a product name, category and number.
3. Edit avatars, required downloads, contents, prefab summaries and history. Enable sections and reorder with arrows.
4. Review output and warnings. Resolve warnings, then Copy Full Text into BOOTH.
5. Register Product to save it; Open returns to an existing product.
6. Export Backup saves registry/products/presets as JSON. Import replaces current data after confirmation.

## Settings

Manage names/URLs in Registry; apply registry updates to the current product explicitly. Save named presets for reuse.

## Result and recovery

Auto-saves to `BoothDescriptionStudio/library.json` under OS LocalApplicationData. Save Now shows the current status.

## Troubleshooting

Check duplicate IDs, date formats and pasted numeric summaries. If saving fails, check folder permissions and export the draft. Unreadable stored data stops auto-save to protect the original.

[Detailed requirements and installation](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_EN.md) · [All guides](../README_EN.md)
