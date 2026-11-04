# Shape Key List

[ガイド一覧](../README_JP.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

開発ターゲット: **Unity 2022.3.22f1**

開発版ガイドです。Unity・VRChat SDKでの実機確認は未実施です。

## できること

Prefabやシーンオブジェクト内のメッシュ名・シェイプキー名を一覧表示してコピーします。

## 準備

確認したいPrefabまたはシーン上のルートを用意します。非アクティブの子も検索対象です。

## 操作手順

1. `NekkoYa → Shape Key List` を開きます。
2. Prefab／オブジェクト欄へProjectまたはHierarchyから対象をドラッグします。
3. メッシュ件数・シェイプキー合計数・各メッシュのパスを確認します。
4. 「名前をコピー」「コピー」で必要な項目を取得し、「すべてコピー」で全体を取得します。
5. 更新後は「再読み込み」で一覧を作り直します。

## 設定

一覧内の文字を選択してコピーすることもできます。選択欄が空なら一覧は表示しません。

## 結果と戻し方

クリップボードへテキストを書き出します。アセット・シェイプキー・ウェイトは編集しません。

## 困ったとき

一覧が空なら対象にMeshFilterまたはSkinnedMeshRendererとメッシュがあるか確認します。メッシュはあってもシェイプキーがない場合は、その旨を表示します。

[詳しい仕様・導入説明](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_JP.md) · [ガイド一覧](../README_JP.md)
