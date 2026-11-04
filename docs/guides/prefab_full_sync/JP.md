# Prefab Full Sync

[ガイド一覧](../README_JP.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

開発ターゲット: **Unity 2022.3.22f1**

開発版ガイドです。Unity・VRChat SDKでの実機確認は未実施です。

## できること

Hierarchy上のオブジェクトの全差分を、ProjectのPrefabへバックアップ付きで反映します。

## 準備

複製した検証プロジェクトで先に試してください。対象Prefabを保存し、反映先のPrefab Modeを閉じます。通常のUndoでは戻せません。

## 操作手順

1. `NekkoYa → Prefab Full Sync` を開きます。
2. 反映元（Hierarchy）へ通常シーンのルート、反映先（Project）へAssets内のPrefabアセットのルートを指定します。
3. 「差分をプレビュー」を押し、追加・削除・変更を読みます。反映先だけにある要素は削除されます。
4. Nested PrefabとVariantの継承が展開され、ルート名とローカルTransformも置き換わることを確認します。
5. 「バックアップを作成して全差分を反映」を押し、確認画面を読んで実行します。
6. 結果とバックアップを確認します。戻す場合はPrefab Modeを閉じ、バックアップの.prefabと.prefab.metaを元パスへ一緒に戻してUnityをRefreshします。

## 設定

プロジェクト直下の `PrefabFullSyncBackups` に保存します。「直近のバックアップフォルダを開く」と各フォルダーのRestore.txtを参照できます。

## 結果と戻し方

反映先Prefabを上書きします。参照先のMaterial・Mesh自体は編集しませんが、同名オブジェクトなどでは内部fileID変更が外部参照に影響する場合があります。

## 困ったとき

読み取り専用・未保存変更・Missing Script・保存不能な参照は先に解消します。プレビュー後の変更検出時は新しい差分を再確認してください。自動復元も失敗した場合は保存せずバックアップを確認します。

[詳しい仕様・導入説明](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_JP.md) · [ガイド一覧](../README_JP.md)
