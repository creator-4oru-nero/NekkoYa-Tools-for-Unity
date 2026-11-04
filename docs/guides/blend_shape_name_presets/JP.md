# Blend Shape Name Presets

[ガイド一覧](../README_JP.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

開発ターゲット: **Unity 2022.3.22f1**

開発版ガイドです。Unity・VRChat SDKでの実機確認は未実施です。

## できること

FBX更新前のシェイプキー値を名前付きJSONへ保存し、更新後に名前で対応させて復元します。

## 準備

シーンまたはPrefab Modeで対象ルートを用意します。FBXを更新する前に保存してください。Project内のPrefabアセットをそのまま指定する操作とは異なります。

## 操作手順

1. `NekkoYa → Blend Shape Name Presets` を開き、対象ルート（アバター）を指定します。
2. 「現在の値をJSONに保存」で保存先を選びます。非アクティブの子と0の値も保存します。
3. FBXを更新した後、同じルートを指定して「保存したJSONを開く」を押します。
4. 一致・追加・削除・値変更のプレビューを確認します。階層・同名兄弟などのエラーを解消します。
5. 「新しく追加されたキーを0にする」を選びます。保存済みRenderer内の新規キーだけが対象で、OFFなら現在値を保ちます。
6. 「名前で復元（Undo対応）」を実行し、見た目を確認してシーン／Prefabを保存します。

## 設定

名前・Renderer階層で照合します。削除済みキーはスキップし、保存されていないRendererは変更しません。

## 結果と戻し方

対応したRendererのシェイプキー値を復元します。必要ならUndoで戻せます。JSON自体をFBXへ書き込む機能ではありません。

## 困ったとき

同名の兄弟や重複キー名、Renderer不一致で対応が曖昧な場合は復元前に停止します。階層とJSONの対象を確認し、エラーがあるままの強制復元は行わないでください。

[詳しい仕様・導入説明](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_JP.md) · [ガイド一覧](../README_JP.md)
