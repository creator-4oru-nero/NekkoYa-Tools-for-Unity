> 本ツールはAI（Codex）を活用して開発しています。ご利用は自己責任でお願いいたします。

> 現在の開発ターゲット: **Unity 2022.3.22f1（VRChat向け）**。Unity実機での動作確認は未実施です。

# NekkoYa-Tools for Unity

Unity向けツール・エディタ拡張のリポジトリです。7ツールの改修版ガイドを用意しています。

[使い方ガイド一覧・導入方法](docs/guides/README_JP.md) · [English](docs/guides/README_EN.md) · [한국어](docs/guides/README_KR.md)

| ツール | 使い方ガイド |
| --- | --- |
| BOOTH Description Studio | [JP](docs/guides/booth_description_studio/JP.md) / [EN](docs/guides/booth_description_studio/EN.md) / [KR](docs/guides/booth_description_studio/KR.md) |
| Asset Organizer | [JP](docs/guides/asset_organizer/JP.md) / [EN](docs/guides/asset_organizer/EN.md) / [KR](docs/guides/asset_organizer/KR.md) |
| Blend Shape Name Presets | [JP](docs/guides/blend_shape_name_presets/JP.md) / [EN](docs/guides/blend_shape_name_presets/EN.md) / [KR](docs/guides/blend_shape_name_presets/KR.md) |
| Shape Key List | [JP](docs/guides/shape_key_list/JP.md) / [EN](docs/guides/shape_key_list/EN.md) / [KR](docs/guides/shape_key_list/KR.md) |
| VRC Data Checker | [JP](docs/guides/vrc_data_checker/JP.md) / [EN](docs/guides/vrc_data_checker/EN.md) / [KR](docs/guides/vrc_data_checker/KR.md) |
| Prefab Full Sync | [JP](docs/guides/prefab_full_sync/JP.md) / [EN](docs/guides/prefab_full_sync/EN.md) / [KR](docs/guides/prefab_full_sync/KR.md) |
| PhysBone Organizer | [JP](docs/guides/physbone_organizer/JP.md) / [EN](docs/guides/physbone_organizer/EN.md) / [KR](docs/guides/physbone_organizer/KR.md) |

改修版の導入先は `refactor/unity-v1.0.0` ブランチの開発パッケージです。mainには元版の保存ファイルがあり、配置・メニューが異なります。導入URL、旧版との違い、必要なSDKはガイド一覧に記載しています。

PhysBone Organizerの実行にはVRChat SDK AvatarsのVRCPhysBoneが必要です。SDKの具体的な対応バージョンは未確定です。

元版6ツールは [Original/v0.1.0](Original/v0.1.0)、PhysBone Organizer原本は[専用Release](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/releases/tag/physbone-organizer-v0.1.0)で確認できます。

## 開発方針

- Unity専用のツールとして開発・管理します。
- AGENTS.mdに従い作業ブランチで開発してPRを作成します。mainへのマージと正式公開は承認後に行います。
