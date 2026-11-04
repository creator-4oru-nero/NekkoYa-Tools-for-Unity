# PhysBone Organizer

[ガイド一覧](../README_JP.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

開発ターゲット: **Unity 2022.3.22f1**

開発版ガイドです。Unity・VRChat SDKでの実機確認は未実施です。

## できること

ボーン上のPhysBoneをアバター直下のPBコンテナへ整理します。

## 準備

VRChat SDK AvatarsのVRCPhysBoneが必要です。通常シーンでアバターを用意し、参照を持つ他の開いたシーンもバックアップしてください。

## 操作手順

1. `NekkoYa → PhysBone Organizer` を開きます。
2. 「アバター本体」を指定します。Armature／アーマチュアを自動検出し、複数候補なら選び、見つからなければ手動指定します。
3. 「対象を確認」で対象コンポーネントと更新するシーン内参照数を確認します。
4. ボーンのON/OFF制御とAnimationClipのパスは自動移行しない点を確認します。
5. 「PhysBoneをPBにまとめる（Undo対応）」を押し、件数と影響範囲を読んで確認します。
6. PB配下の設定・参照・見た目を確認し、必要ならUndoで戻します。問題なければ関係するシーンを手動保存します。

## 設定

既存PBは再利用せずPB (1)等を新規作成します。Root Transformが空なら元のボーンを指定し、設定済みなら維持します。現在の有効／無効状態を引き継ぎます。

## 結果と戻し方

元ボーンからPhysBoneコンポーネントだけを移し、ボーン階層は維持します。他の開いた通常シーンのコンポーネント参照も更新します。

## 困ったとき

SDKなし・Prefab Mode・Play Mode・編集不可参照では実行できません。アセット・閉じたシーン内の参照、アニメーションや将来の有効状態制御は別途確認してください。確認後に対象が変わった場合は再確認します。

[詳しい仕様・導入説明](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_JP.md) · [ガイド一覧](../README_JP.md)
