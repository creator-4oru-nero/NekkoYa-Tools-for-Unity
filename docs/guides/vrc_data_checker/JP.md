# VRC Data Checker

[ガイド一覧](../README_JP.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

開発ターゲット: **Unity 2022.3.22f1**

開発版ガイドです。Unity・VRChat SDKでの実機確認は未実施です。

## できること

配布フォルダーの参照切れ、テクスチャ・メッシュ・マテリアル・Prefabの問題を確認します。

## 準備

Assets内の配布対象フォルダーを用意します。Packagesやプロジェクト外のフォルダーは指定できません。

## 操作手順

1. `NekkoYa → VRC Data Checker` を開きます。
2. 検査フォルダーを指定するか、Projectで選んで「選択中のフォルダ」を押します。
3. Avatarアイテム／Worldアイテムなどの用途と、PC／Mobileを選択します。
4. Import・コンパイルが終わってから「検査開始」を押します。キャンセルや例外があれば未完了表示に注意します。
5. 重要度・種類・検索で絞り込み、項目を選んで原因・推奨対応・参照元を読みます。
6. Projectでの表示やPrefabを開く操作で対象を確認し、全結果をコピーまたはMarkdown保存します。
7. 設定では除外・保持パターンと推奨閾値を調整できます。変更後は再検査します。

## 設定

通常検査の除外でも依存関係・参照切れ検査は対象です。「未使用候補にしない保持対象」で必要な素材を保護します。推奨値はVRChatの絶対制限ではありません。

## 結果と戻し方

検査対象のアセットを書き換えず、レポートを表示・出力します。

## 困ったとき

未使用候補は削除許可ではありません。動的読み込みを判定できず、MA詳細・Animation／Animator・VRC Parameters／Performance Rank検査は未実装です。未完了なら原因を解消して再検査してください。

[詳しい仕様・導入説明](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_JP.md) · [ガイド一覧](../README_JP.md)
