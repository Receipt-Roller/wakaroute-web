# WakaRoute Web

[ワカルート（WakaRoute）](https://wakaroute.com)のWebアプリケーションです。高校受験に必要な情報と学習内容を細かく整理し、生徒と保護者が「今の位置」と「次に進む道」を理解できるサービスを目指しています。

ワカルートは株式会社レシートローラーが開発する、無料のオープンソースプロジェクトです。特定の学校、教育委員会、文部科学省が運営する公式サービスではありません。

## 現在利用できる機能

- 高校受験の全体像を理解するためのガイド
- 保護者向けガイド
- 国語・数学・英語・理科・社会の理解マップ
- 文部科学省の学校コードを基にした全国高校検索
- モバイルアプリからも利用できる学校検索JSON API

## 技術構成

- .NET 10
- ASP.NET Core MVC / Razor Views
- Vanilla CSS / JavaScript
- JSONベースの学校カタログ

## ローカル開発

.NET 10 SDKをインストールし、リポジトリのルートで実行します。

```powershell
dotnet restore
dotnet run
```

起動後、コンソールに表示されるローカルURLをブラウザで開いてください。学校検索APIは `/api/schools` で確認できます。

Releaseビルド：

```powershell
dotnet build --configuration Release
```

## Azureへのデプロイ

`.github/workflows/deploy-azure.yml`は、`main`へのpushまたはGitHub Actionsからの手動実行で、Release publishを`wpp-wakaroute` Azure Web Appへデプロイします。

GitHubリポジトリに、Azure Portalから取得した発行プロファイルのXML全体を次のActions Secretとして登録してください。

```text
AZURE_WEBAPP_PUBLISH_PROFILE
```

発行プロファイルはアプリへデプロイできる認証情報です。ファイルや内容をリポジトリへコミットせず、漏えいした場合はAzure Portalで直ちに再発行してください。

## 学校カタログ

学校カタログは文部科学省の学校コードCSVから再生成できます。

```powershell
dotnet run --project tools/SchoolCatalogGenerator/SchoolCatalogGenerator.csproj -- --output Data/Schools
```

データ構造、件数、手動補完のルールは[Data/Schools/README.md](Data/Schools/README.md)を参照してください。実際の出願や進路判断では、必ず学校・都道府県教育委員会などの最新の公式情報を確認してください。

### 学校データの公開と再利用

- `school-id.json`と`schools.json`の学校コード、学校名、住所等は、文部科学省の「令和7年5月1日時点（確定版）」学校コードCSVを高校に絞り、正規化・加工して作成しています。出典と加工内容は[NOTICE](NOTICE)に記載しています。
- WakaRouteが作成した安定ID、データ構造、学校選びの観点、独自の要約・注釈は、個別に別の条件を示していない限り、本リポジトリのApache License 2.0で利用できます。
- 各レコードの`sources`は確認に使用した公式ページへの参照です。参照先ページの文章、写真、ロゴ等の権利が本リポジトリのライセンスへ移るものではありません。本リポジトリはそれらを転載せず、事実を整理した独自要約とリンクを収録します。
- 再配布・加工時は、文部科学省等の出典を保持し、加工した場合はその旨を明記してください。学校情報・入試情報は変更されるため、利用者自身でも公式情報を確認してください。

GitHub上のJSONを取得して別サービスや研究で利用することも想定していますが、正確性・完全性・最新性を保証するものではありません。特に出願、受検、進路判断に利用する場合は、学校・教育委員会の最新発表を優先してください。

## MANABU2との関係

[MANABU2.COM](https://manabu2.com/ja-JP)は、株式会社レシートローラーが開発する学習基盤です。今後ワカルートでは、教材、問題、テスト、学習履歴などをMANABU2 APIから利用する計画です。

このリポジトリには、APIクライアント、認証設定例、ローカル開発用モックなど、MANABU2 APIの参考実装を置く方針です。APIキー、ユーザーデータ、教材原本、運用環境の秘密情報は含めません。

## リポジトリ構成方針

プラットフォームごとにリリース工程、依存関係、署名情報が異なるため、コードは別リポジトリで管理します。

- `wakaroute-web`：ASP.NET Core Webアプリ（このリポジトリ）
- `wakaroute-ios`：iOSアプリ（予定）
- `wakaroute-android`：Androidアプリ（予定）

学校IDやMANABU2 APIの契約は全クライアントで共通化し、OpenAPIやJSON Schemaなどの機械可読な仕様を共有します。

## コントリビューション

バグ報告、改善案、ドキュメント修正、実装への協力を歓迎します。作業を始める前に[CONTRIBUTING.md](CONTRIBUTING.md)をご確認ください。脆弱性は公開Issueではなく、[SECURITY.md](SECURITY.md)の手順で報告してください。

## ライセンスと出典

ソースコードは[Apache License 2.0](LICENSE)で公開します。WakaRouteの名称やロゴの扱いは[TRADEMARKS.md](TRADEMARKS.md)、学校データの出典と利用条件は[NOTICE](NOTICE)を確認してください。
