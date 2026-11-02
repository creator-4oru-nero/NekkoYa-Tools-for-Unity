# NekkoYa Tools for Unity

Unity 2022.3.22f1向けの7つのEditorツールです。1.0.0に向けた改修版は **Unreleased / 1.0.0-pre.1** です。Unity実機でのコンパイル・動作確認と、正式公開の承認はまだ行っていません。

## ツールと使い方

Unity最上部の `NekkoYa` メニューから開きます。ツール名・ウィンドウタイトルは英語固定です。

| ツール | 主な操作 |
| --- | --- |
| BOOTH Description Studio | 商品、対応アバター、URL、内容物、更新履歴を入力し、プレビューを確認して全文コピー。商品・プリセットを登録し、JSONでバックアップ／読み込みできます。 |
| Asset Organizer | RenameタブでProjectアセットを追加して並べ替え、`{00}`などの連番と`{Name}`を使ってプレビュー後にリネーム。直前の変更を専用ボタンで戻せます。Folder Generatorタブではフォルダーツリーを編集し、Assets内に不足フォルダーだけを作成します。 |
| Blend Shape Name Presets | シーンまたはPrefab Modeのルートを選び、FBX更新前に値をJSON保存。更新後に同じルートでJSONを読み込み、追加・削除・変更のプレビューを確認して名前で復元します。 |
| Shape Key List | Prefabまたはシーンオブジェクトを指定し、非アクティブの子を含むメッシュ・シェイプキー一覧を表示してコピーします。アセットを編集しません。 |
| VRC Data Checker | Assets内の配布フォルダー、用途、PC／Mobileを選んで検査。種類・重要度・検索で絞り込み、原因と推奨対応を確認。結果をコピー／Markdown出力できます。 |
| Prefab Full Sync | Hierarchyの反映元とAssets内の反映先Prefabを指定。差分を確認し、バックアップしてから全差分を反映します。 |
| PhysBone Organizer | Hierarchyのアバターを指定してアーマチュアを自動検出。候補が複数ある場合は選択し、必要なら手動指定します。対象とシーン内参照数を確認し、確認画面からPhysBoneをアバター直下のPBへまとめます。 |

## 導入と移行

1. Unity 2022.3.22f1の検証用プロジェクトをバックアップします。
2. 旧版を導入済みの場合は、BOOTHのJSON、シェイプキープリセット、フォルダープリセット等のユーザーデータを先に退避します。旧ツールのスクリプトを重複して導入しないでください。
3. Package Managerの `+` → `Add package from git URL...` に次を入力します。現時点では未公開の作業ブランチです。

```text
https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity.git?path=/Packages/com.nekkoya.tools#refactor/physbone-organizer-v1.0.0
```

非公開リポジトリのため、GitHubへのアクセス権とローカルGitの認証が必要です。正式承認後は確認済みコミットまたは正式タグに固定してください。Unity標準APIを優先し、新しい外部ライブラリは追加していません。前の6ツールではVRChat SDK、Modular Avatar、lilToonは必須依存ではありません。既存の任意連携に対応し、特定バージョンとの実機互換性は未検証です。

PhysBone Organizerの実行には **VRChat SDK AvatarsのVRCPhysBone** が必要です。前の6ツールには必須ではありません。SDKなしでもウィンドウは開けますが、整理操作は無効になります。SDKの具体的な対応バージョンは実機未検証です。`rootTransform` を読み取れないSDKでは、データを変更する前に停止します。

`.cs.meta`、プリセットのGUID、BOOTHの保存場所とJSON形式、シェイプキーJSONのformat/versionは維持しています。フォルダープリセットには旧Editorアセンブリからの移行情報を追加しました。Unityによる実際の型移行は実機確認が必要です。旧版のBOOTHデータはOSのLocalApplicationData配下 `BoothDescriptionStudio/library.json` を引き続き使います。

## 設定

- `NekkoYa > Language > JP / EN / KR`：全ツール共通、初期値JP。EditorPrefsに保持し再起動後も維持します。選択中の言語だけにチェックが付きます。
- 言語変更で開いているツールを再描画します。既存の検査レポート、復元プレビュー、通知など生成済みの文字列は生成時の言語を保持する場合があるため、再検査／プレビュー更新／再読み込みしてください。
- BOOTHの商品本文、規約、商品名、ユーザーが入力した文章、正規表現の解析仕様はUI言語変更で書き換えません。元から日本語の商品テンプレートは日本語のままです。
- Asset Organizer：連番の開始番号・名前テンプレート、フォルダーツリー・保存先・プリセットを設定します。パッケージ付属／読み取り専用のプリセットは `Save As` でAssets内へコピーしてから編集します。
- Blend Shape Name Presets：新しく追加されたキーを0にするか選択できます。保存済みRendererだけに適用します。
- PhysBone Organizer：アバター本体と対象アーマチュアを指定します。`Armature`／`アーマチュア`と連番付きの名前を自動検出します。PBが既存なら `PB (1)` 等を作り、同名ボーンの格納先も重複を避けます。Root Transformが空なら元のボーンを指定し、既存指定・設定値・現在の有効／無効状態を維持します。
- VRC Data Checker：除外・保持パターン、推奨閾値、Android推奨形式、外部依存分類、除外Propertyを設定します。設定は従来の `ProjectSettings/NekkoyaVRCDataChecker.asset` を利用します。

## データ保護と注意事項

- シェイプキー復元とフォルダープリセットの編集はUndoに対応します。アセットファイルのリネームは専用の直前リネーム復元を使用します。フォルダー生成は通常のUndoには対応しません。失敗時は今回作成した空フォルダーだけを戻し、既存フォルダーや追加された内容は削除しません。
- Prefab Full Syncは反映先だけにある要素も削除し、Nested PrefabとVariantの継承を展開します。通常のUndoでは戻せません。確認画面を読み、バックアップから復旧してください。
- Prefab Full SyncのバックアップはUnityプロジェクト直下の `PrefabFullSyncBackups` に作成します。復旧時はPrefab Modeを閉じ、バックアップの `.prefab` と `.prefab.meta` を元パスへ一緒に戻してUnityをRefreshします。各バックアップの `Restore.txt` も参照してください。
- VRC Data CheckerはAssetsを書き換えません。未使用候補は削除許可ではなく、動的読み込みを判定できません。MA詳細、Animation／Animator、VRC Parameters／Performance Rankは未実装です。推奨値はVRChatの絶対制限ではありません。
- 正常な操作で大量のログを出さず、エラーログには `[NekkoYa][ToolName]` を付けます。OS／Unity由来の例外文は環境の言語で表示される場合があります。

## 検証

PhysBone Organizerは元ボーンからコンポーネントだけを移し、ボーン階層を維持します。開いている他の通常シーン内のコンポーネント参照も更新するため、関係するシーンをバックアップしてください。アセット、閉じたシーン、AnimationClipのパス、ボーンの将来のON/OFF制御は更新しません。結果を確認後にシーンを手動保存してください。Play Mode・Prefab Mode・別シーン間の対象指定には対応しません。途中失敗時はUndoで戻し、復元にも失敗した場合は成功と表示せず通知します。

元のPhysBone Organizerは `Original/PhysBoneOrganizer/v0.1.0/NekkoYaPhysBoneOrganizer.cs` に無改変で保存し、ツール単位の `physbone-organizer-v0.1.0` タグで区別しています。

リポジトリの `scripts/verify_repository.py` で元版の一致、GUID、アセンブリ参照、翻訳キーと書式引数を検証できます。C#構文解析も実施済みですが、Unityでのコンパイルや動作確認とは別です。

Unity Test RunnerのEditModeで既存VRC検査テストと `ToolRegressionTests` を実行してください。Packageのテストを列挙するにはプロジェクトの `Packages/manifest.json` の `testables` に `com.nekkoya.tools` を追加します。テストは検証用プロジェクトで実行してください。詳細はリポジトリの `VALIDATION.md` を参照してください。
