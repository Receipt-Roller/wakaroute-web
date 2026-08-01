# WakaRoute School Catalog

高校検索で使用する学校マスタです。文部科学省が公開する「令和7年5月1日時点（確定版）」の学校コードCSVを出典とします。

- `school-id.json`: 現行・廃止を含む高校コードのアイデンティティ台帳。WakaRoute ID、文科省学校コード、旧学校調査番号、別名、統合先を保持します。
- `schools.json`: 現在検索対象となる高校コードの表示情報。廃止年月日のない学校・分校を含みます。
- `school-id.schema.json` / `schools.schema.json`: 各JSONの基本構造を示すJSON Schemaです。

## 再生成

リポジトリのルートで次を実行します。

```powershell
dotnet run --project tools/SchoolCatalogGenerator/SchoolCatalogGenerator.csproj -- --output Data/Schools
```

生成ツールは既存ファイルを読み込み、次の手動補完を保持します。

- `school-id.json`: `id`, `aliases`, `replacedById`
- `schools.json`: `nameKana`, `latitude`, `longitude`, `officialUrl`, `tags`

学校名、住所、郵便番号、設置区分など公式CSV由来の項目は、再生成時に最新の公式値で更新されます。出典CSVを変更する場合は `--east-url`、`--west-url`、`--as-of` を指定してください。

## 件数について

令和7年度学校基本統計の学校数は4,761校ですが、学校コード一覧は統計表とは集計目的が異なります。本カタログは検索漏れを防ぐため、学校コード一覧で廃止年月日が空の高校コードを掲載し、分校も独立した検索結果として扱います。

## 手動編集のルール

- `id` は公開後に変更しないでください。
- 学校名をURL用IDとして使用しないでください。
- 公式情報を確認した日を `lastVerifiedAt` に記録してください。
- 学校紹介文、画像、偏差値など、権利や出典を確認できない情報は追加しないでください。
