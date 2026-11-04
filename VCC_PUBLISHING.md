# VCC配布

元リポジトリは非公開のまま維持します。公開配布先は https://github.com/creator-4oru-nero/NekkoYa-Tools-VPM です。配布ZIPにはUnityで必要なC#ソースが含まれますが、元リポジトリの履歴・Originalアーカイブは含めません。

## 更新手順

1. 承認済みのバージョンをpackage.jsonに設定し、Unityで検証します。
2. `python3 scripts/verify_repository.py` と `python3 -m unittest discover -s scripts -p test_vpm.py` を実行します。
3. `python3 scripts/build_vpm.py build --output /tmp/nekkoya-vpm --tag v1.0.0-pre.1` でZIPを作成します（タグは対象バージョンに置き換えてください）。
4. 配布先リポジトリで同じタグのReleaseにZIPを添付して公開します。候補版はPre-releaseにします。過去のZIPは上書きせず、新バージョンを使用します。
5. 配布先のPublish listingワークフローが公開済みRelease全件からSHA-256付きの一覧を生成してPagesへ反映します。失敗時は同ワークフローを手動実行できます。

VCC登録URL: https://creator-4oru-nero.github.io/NekkoYa-Tools-VPM/index.json

元リポジトリのCIは検証と確認用ZIP生成のみです。配布先の認証情報を元リポジトリに追加せず、Release公開は権限のある担当者が実行します。UnityおよびVCC実機での確認は未実施です。
