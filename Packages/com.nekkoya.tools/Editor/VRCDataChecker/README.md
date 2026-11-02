# VRC配布データ検査 — Phase 1

Unity 2022.3.22f1向けの配布前検査Editor拡張です。

## 使用方法

1. `Tools > NekkoYa > VRC配布データ検査` を開きます。
2. Projectの検査フォルダをドラッグするか「選択中フォルダを使用」を押します。
   フォルダ右クリックの `Nekkoya > VRC配布データをチェック` からも開けます。
3. 用途とPC／Mobileを指定して「検査開始」を押します。
4. 上部のエラー／警告／情報の件数ボタンと、左側の種類一覧で絞り込みます。初期表示は「要確認のみ」（エラー・警告）です。問題がなく情報のみの場合は「情報」を表示します。
5. 一覧の項目をクリックすると、Project側でも対象Assetが選択され、右側（狭いウィンドウでは下側）に検出内容・推奨対応・参照元が表示されます。
6. 除外や閾値の編集は右上の「設定」から開きます。
7. 下部の「コピー」「Markdown保存」は、表示フィルタに関係なく全検査結果を出力します。

検査開始ではAsset保存・Import・Refresh・自動修正を行いません。
Prefabは一時Sceneへ読み込み、必ず解放します。結果の「Prefabを開く」「選択」はユーザー操作時のみ実行します。
除外・閾値設定の編集時のみ `ProjectSettings/NekkoyaVRCDataChecker.asset` を保存します。

## 警告条件と未使用候補

- `Packages/`以下への依存は情報として記録します。パッケージ内のEditorフォルダへの依存も、配置だけでは警告・エラーにしません。実際のMissing参照は別途検出します。
- lilToonは機能スイッチを読み、OFFの機能や現在のShaderにない設定に残ったTextureのMissingを「情報」に分けます。使用中・判断できないTextureはエラーのままです。Shader自体のMissingは抑制しません。Animationで機能を有効化するケースは未解析のため、情報の推奨文に明示しています。
- lilToonの判定は導入済み2.3.4と[公式のMaterial処理](https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Editor/lilMaterialUtils.cs)を参照しています。チェッカーからlilToonの修正関数は呼びません。
- Normal Map推定は画像のファイル名に`normal`（大小文字不問）または`ノーマル`を含む場合のみです。フォルダ名や`Bump`プロパティだけでは警告しません。
- Max Sizeが元画像の長辺より大きい場合に警告します。元画像2048／Max Size1024はOK、元画像1024／Max Size2048は警告、同じサイズはOKです。Defaultおよび指定対象のPC／Android Overrideを確認します。用途別の大きさ判定はimport後の実サイズで行います。
- 添付画像に見える初期設定（Default、2D、sRGB、Alpha、Mipmap関連、Swizzle、Repeat、Bilinear、Aniso 1、Max Size2048、Mitchell、Automatic、Normal Quality、Crunch OFF）との一致を警告します。プラットフォームOverrideがある場合は対象外です。過去に編集されたかは分かりません。
- 「Linear Data」は「色ではなく光沢・金属感・マスクの強さを表す画像」と表示します。
- 未使用候補は専用タブに分離し、画像・Materialなどのプレビューを表示します。プレビュー非対応Assetはアイコン表示です。Markdownでも別セクションに出力します。
- TextureはAssets全体のMaterial（Model内のMaterialも含む）と検査対象の依存Materialを調べ、「どのMaterialにも未設定」「無効機能／旧Shader設定にだけ残存」を別の説明で候補に出します。Packages全体の独立したMaterial・動的生成Materialは網羅しません。UIやメニューアイコン、Animation、スクリプトで使う画像はMaterial未使用でも必要な場合があります。
- 画像の詳細上部にMaterial名・画像欄・有効／無効の判定・選択ボタンを表示します。無効な機能に残る画像もUnityの保存上の依存なので、フォルダ外依存の確認対象には残します。
- Read/Writeは「通常のMaterial表示ならOFF」「ピクセルをスクリプトで読み書きするならON」という具体例と、Inspectorで変更する場所を表示します。検査から自動変更は行いません。
- 新規設定ではPNG／JPGを一律に保持対象へ入れません。以前に保存した設定は維持するため、画像を候補へ出したい場合は「設定」の保持対象に`*.png`や`*.jpg`が残っていないか確認してください。

## 実装範囲

- 再帰Asset収集、Sub Asset対応、境界を考慮したルート判定。
- 直接依存グラフと推移的依存集合、外部依存の分類と直接参照元。
- 全SerializedPropertyのMissing参照、Missing Script、MaterialのMissing Shader／Texture。
- TextureのMipmap、Read/Write、サイズ、NPOT、sRGB、Normal、圧縮、Android設定、推定メモリ。
- Mesh集計、使用中Skinned／Particle MeshのRead/Write、Normal／Tangent／Weight／Bounds。
- 全Prefab・Variant・Nested Prefab・非アクティブObjectの走査、Material／Mesh空欄、Scale等。
- 未使用候補、除外一覧、キャンセル・Asset単位の例外継続、Markdown。
- SDK／MA／lilToonへのコンパイル時参照なし。

Scene Assetは依存関係のみ対象で、Scene内部のObjectは開いて検査しません。
FBX内Mesh／MaterialはSub Assetとして処理しますが、FBX内部のComponent参照検査は行いません。
Mipmapの3D用途は検査対象Prefab内のRendererから推定します。Scene専用Materialや動的に設定されるMaterialの3D用途は判定できません。
Missing GUID検出はUnityのSerializedObjectがMissingとして公開するものに限ります。
ShaderのエラーShader状態は検出しますが、Shaderコンパイルエラーの網羅的解析は行いません。
設定値は本ツールの推奨値です。最新VRChat SDKの検証を代替するものではありません。

## 次段階・未実装

設計書のPhase 2（MA必須参照、Animation／Animator、VRC Expression Parameters／Menu、PhysBone等の集計、性能ランク）とPhase 3（修正・UnityPackage書き出し）は未実装です。
Package衛生検査、Material／ファイル内容の重複候補、Audio／Particle全項目も未実装です。
CompleteAvatarは現段階ではAvatar向けの基本ルールを適用し、性能ランクを算出しません。
MobileはTexture／Instancing等の補助検査です。Mobile Shader可否や厳密なSDK制限はまだ判定しません。

## 検証

`Editor/Tests` にEditModeテストを用意しています。Test Runnerで `Nekkoya.VRCDataChecker.Tests` を実行してください。
テストは一意の `Assets/__VRCDataCheckerTests_*` に一時Assetを作成し、終了時に削除します。
通常の検査と異なり、テストは検証用Assetの作成・Import・削除を行います。

実装時に本体・テスト両方をUnity 2022.3.22f1のRoslynコンパイラと標準Unity参照DLLで静的コンパイルしています。本体の確認にはVRCSDK／MA／lilToon DLLを使用していません。
Unity Editor上のEditModeテスト実行・UI操作・描画・実Assetによる動作確認は未実施です。
追加変更のうち、Max Size比較・Normal名判定・Packageパス判定の11ケースは、Unity付属Monoで本体DLLの処理を実行して成功しています。Importer／Material／UIを使う追加EditModeテストはコンパイルのみ確認しました。
