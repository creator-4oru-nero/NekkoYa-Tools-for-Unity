# 1.0.0候補の検証

## この環境で実施する検証

`python scripts/verify_repository.py`

- 元ZIP内の99ファイルと `Original/v0.1.0` のファイル集合・SHA-256一致。
- UPM manifest、Editor専用アセンブリ、参照解決と循環参照。
- Unityのmeta/GUID重複、既存スクリプトmetaの維持、付属プリセットのスクリプト参照。
- 翻訳キー、英語・韓国語訳、書式引数、メニュー名とログ規則。
- tree-sitterとtree-sitter-c-sharpが利用可能な場合は全C#ファイルの構文解析。これはUnityのAPI解決・コンパイル・実行検証ではない。

## Unity 2022.3.22f1で必要な確認（未実施）

1. バックアップ済みの検証用プロジェクトにGit URLでインストールし、コンパイルエラーがないこと。SDKなしでも6ツールが開くこと。
2. 最上部メニューがNekkoYaであり、6つのツール名・ウィンドウタイトルが英語、Languageが最下部であること。
3. 初回JP、JP/EN/KR切り替え、選択中だけのチェック、Unity再起動後の保持、開いているウィンドウの再描画。生成済みレポート／プレビューは再生成して翻訳を確認する。
4. BOOTH：旧library.jsonの読み込み、商品登録、複製、プリセット、リンク、本文、プレビュー、JSONバックアップ／読み込み。読み取り専用の保存先や重複商品IDで保存できない場合に、商品切り替えやインポートを止めて下書きを保持すること。閉じる際の保存／破棄確認。商品本文・規約を勝手に翻訳しないこと。
5. Asset Organizer：連番・Name・並べ替え・拡張子・既存名衝突・リネーム循環・専用復元・保存先権限。パッケージ内／サブアセットはリネームしないこと。フォルダーツリー・重複名・既存フォルダーの再利用・失敗時の空フォルダー巻き戻し。
6. 旧001.assetプリセットの型移行と読み込み、Assets内へのSave As、変更保存のUndo。パッケージ付属プリセットの保存／削除／リネームを拒否し、ユーザープリセットの削除はゴミ箱に移動すること。
7. Blend Shape Name Presets：旧JSON、0値・非アクティブRenderer、名前変更、キー増減、同名兄弟、無効値、Prefab Mode、Undo、保存と再読み込み。プレビュー後の変更を再確認すること。
8. Shape Key List：非アクティブ、複数メッシュ、空メッシュ、コピー、アセット不変更、言語変更後の再読み込み。
9. VRC Data Checker：既存EditModeテスト、除外／保持／設定保存／キャンセル／Markdown出力。正常・不正Prefabと参照切れ。SDKなし、必要なSDKあり、lilToonあり・なしで任意連携を確認。対象Assetsの不変更をハッシュで確認。
10. Prefab Full Sync：通常Prefab・Nested・Variant、元オブジェクト不変更、差分プレビュー更新、Missing Script／外部シーン参照の拒否、バックアップのGUID、復元方法、保存失敗時の自動復元。破壊的な処理なので複製プロジェクトで検証すること。
11. Unity Test Runner EditModeで `ToolRegressionTests` と既存 `CheckerTests` を実行する。`testables` に `com.nekkoya.tools` を追加し、Unity Test Frameworkが利用可能なテスト用プロジェクトを使う。

以上は実機未検証です。正式1.0.0のタグ・GitHub Releaseは検証結果とユーザー承認後に作成します。
