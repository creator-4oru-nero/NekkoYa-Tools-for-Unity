> 本ツールはAI（Codex）を活用して開発しています。ご利用は自己責任でお願いいたします。

# Unity 使い方ガイド

[日本語](README_JP.md) / [English](README_EN.md) / [한국어](README_KR.md) · [Repository README](../../README.md)

開発ターゲット: **Unity 2022.3.22f1**

| ツール | できること |
| --- | --- |
| [BOOTH Description Studio](booth_description_studio/JP.md) | BOOTHの商品説明を組み立て、商品・プリセット・登録一覧を管理します。 |
| [Asset Organizer](asset_organizer/JP.md) | Project内のアセットを一括リネームし、フォルダーツリーをプリセットから作成します。 |
| [Blend Shape Name Presets](blend_shape_name_presets/JP.md) | FBX更新前のシェイプキー値を名前付きJSONへ保存し、更新後に名前で対応させて復元します。 |
| [Shape Key List](shape_key_list/JP.md) | Prefabやシーンオブジェクト内のメッシュ名・シェイプキー名を一覧表示してコピーします。 |
| [VRC Data Checker](vrc_data_checker/JP.md) | 配布フォルダーの参照切れ、テクスチャ・メッシュ・マテリアル・Prefabの問題を確認します。 |
| [Prefab Full Sync](prefab_full_sync/JP.md) | Hierarchy上のオブジェクトの全差分を、ProjectのPrefabへバックアップ付きで反映します。 |
| [PhysBone Organizer](physbone_organizer/JP.md) | ボーン上のPhysBoneをアバター直下のPBコンテナへ整理します。 |

## 導入

このガイドは英語名の最上部NekkoYaメニューを持つ改修版パッケージが対象です。現在の7ツール入り開発版は `refactor/unity-v1.0.0` ブランチにあり、mainの元版スクリプトとは配置が異なります。

1. Unity 2022.3.22f1のプロジェクトをバックアップします。旧スクリプトとの重複導入を避けてください。
2. Package Manager → `+` → `Add package from git URL...` に次を入力します。

```text
https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity.git?path=/Packages/com.nekkoya.tools#refactor/unity-v1.0.0
```

3. 非公開リポジトリにアクセスできるGit認証を用意し、コンパイル完了後に `NekkoYa` メニューを開きます。
4. `NekkoYa → Language → JP / EN / KR` で共通言語を選びます。

現在は1.0.0-pre.1の開発候補で、Unity実機での動作確認は未実施です。PhysBone Organizerの実行にだけVRChat SDK AvatarsのVRCPhysBoneが必要です。SDKの具体的な対応バージョンは未確定です。

## 元版を使っている場合

元版0.1.0は `Tools → NekkoYa` の日本語メニューです。画面の項目名や安全機能が改修版と異なるため、[元版の保存先](../../Original/v0.1.0)とその説明書も確認してください。旧版と改修版を同時に入れないでください。
