# Gpro 展示用パスキー統合

main の既存 UI / StoryScene / PasskeyScene を維持し、feature/passkey で動作確認した認証機能を組み込む開発ブランチ。

## 展示の導線
1. exhibit-test/register.html でパスキー登録・ゲーム用ダミー情報の発行
2. タイトル → START → StoryScene → PasskeyScene
3. 扉1: 架空IDとパスワードによるフィッシング体験（入力内容は送信・保存しない）
4. 扉2: 実際のWebAuthnパスキー認証
5. Cloudflare Workerで検証成功後のみ Select Scene へ遷移 → ゲーム本編

偽の扉はゲーム用演出で、別ドメインのフィッシング耐性実証ではありません。
サーバー側セッション管理は未実装です。Unity側の遷移制御はデモ用です。

## ビルド手順
1. このブランチをUnity 6で開く。
2. File → Build Profiles → WebでDecompression Fallbackを有効にする。
3. Unityビルドをプロジェクト外の ../Gpro_exhibit_build へ出力する。
4. python3 scripts/prepare-exhibit-build.py ../Gpro_exhibit_build を実行する。
   既存のdocs/exhibit-testを更新する場合は、末尾に --replace を付ける。
5. docs/exhibit-test だけをコミットし、GitHub Pagesにテスト公開する。
6. PCのChromeとiPhoneのSafariで、登録からゲーム開始まで確認する。

main本番公開はまだ行わないこと。旧PasskeyGameFlow.csを取り込むと、認証成功でタイトルへ強制遷移してしまうので追加しないこと。
パスキーのWorkerは既存デプロイ https://passkey-api.gpro-security.workers.dev を利用する。