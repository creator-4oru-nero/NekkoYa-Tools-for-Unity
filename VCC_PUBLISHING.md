# VCC repository publication

## 公開前の準備

- 本変更は `refactor/unity-v1.0.0` の7ツール入りパッケージを対象にしています。PRをそのブランチへ取り込み、公開対象のソースとワークフローを既定ブランチにも反映してください。
- VCCから認証なしで一覧JSONとReleaseのZIPを取得できる公開リポジトリが必要です。リポジトリの公開状態はこの環境から確認できていません。非公開リポジトリを自動で公開に変更しないでください。
- GitHubの **Settings → Pages → Build and deployment → Source** を **GitHub Actions** に設定します。`github-pages` Environmentのデプロイ対象にリリースタグが許可されていることを確認してください。
- Unity 2022.3.22f1でパッケージのコンパイルと既存EditModeテストを実行してください。この変更の検証はZIP・一覧生成・既存の静的検証までです。Unity/VCC実機検証は未実施です。

## 承認後の公開

1. `Packages/com.nekkoya.tools/package.json` のバージョンを確認します。現在は開発候補 `1.0.0-pre.1` のままです。
2. 検証済みコミットに、そのバージョンに `v` を付けたタグ（候補例 `v1.0.0-pre.1`）でGitHub Releaseを作成し、承認後に公開します。開発候補はPre-releaseにしてください。既存のタグやReleaseを上書きしないでください。
3. **Publish VCC repository** がパッケージ直下をZIP化し、公開したReleaseに添付します。続いて、過去の公開済みパッケージReleaseを含む一覧JSONを生成し、Pagesへデプロイします。ZIPのルートに `package.json` を配置し、`.meta` を保持します。各バージョンに配布URLとZIPのSHA-256を含めます。
4. 次のURLをVCCに登録し、検証用プロジェクトでパッケージ追加・Unityコンパイル・更新を確認します。

   `https://creator-4oru-nero.github.io/NekkoYa-Tools-for-Unity/index.json`

   配布ページの **Add repository to VCC** でも登録できます。候補版はVCC側のプレリリース表示設定が必要な場合があります。

この作業ではRelease公開・Pages設定変更・マージを実行していません。GitHub Actionsからの公開処理もまだ未実行です。既存のUPM Git URL導入は引き続き利用できます。SDKを必須依存に追加していないため、PhysBone Organizerを使う場合は別途VRChat SDK Avatarsが必要です。

## ローカル確認

```sh
python3 scripts/verify_repository.py
python3 -m unittest discover -s scripts -p 'test_vpm.py'
python3 scripts/build_vpm.py build --output /tmp/nekkoya-vpm
```

PRの **Check VCC package** は同じ検証を実行し、確認用ZIPをArtifactに保存します。これでは公開されません。

## 一覧だけを再生成

公開済みReleaseの削除・非公開化後やPages側だけの失敗時は、既定ブランチの **Publish VCC repository → Run workflow** を実行します。新しいReleaseやZIPは作成せず、公開済みの一致するZIPから一覧を再構築します。公開パッケージがゼロの場合は空の一覧で上書きせず失敗するため、配布終了時はPagesを明示的に停止してください。

ZIP添付後に一覧生成が失敗した場合も、同じ手動実行で復旧します。既存ZIPの上書きは禁止し、パッケージ修正時は新バージョンを発行してください。元版の個別ツールReleaseは一覧に含めません。
