# SEO本番設定チェックリスト

更新日: 2026-08-15

この文書は、コードのデプロイ後に本番環境の所有者が行う作業をまとめたものです。確認結果はAB Projectsの親タスク `t-1fa72209` と該当サブタスクへ記録します。

## Google Search Console

対象タスク: `t-1fa72216`

1. Search Consoleで`wakaroute.com`のドメインプロパティを追加する。
2. Search Consoleが表示する所有権確認用TXTレコードを、`wakaroute.com`のDNSへ追加する。
3. 所有権確認が完了したら、サイトマップ画面から`https://wakaroute.com/sitemap.xml`を送信する。
4. サイトマップの取得成功、検出URL数、エラー件数をAB Taskへ記録する。
5. URL検査で次の3種類を確認する。
   - `https://wakaroute.com/`
   - 任意の学校詳細1ページ
   - 任意の理解マップ1ページ
6. 公開URLテストで、HTML、CSS、JavaScriptがGooglebotから取得できることを確認する。
7. 1週間後と4週間後に、ページのインデックス登録、検索クエリ、表示回数、CTR、Core Web Vitalsを記録する。

所有権確認用TXT値は秘密情報ではないが、環境ごとにSearch Consoleが発行した正確な値を使う。コードやドキュメントへ仮の値を記載しない。
