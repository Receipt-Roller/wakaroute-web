# 数学・理科・社会の学習カード

`study-cards.json` は、ワカルートのWeb・将来のiOS・Androidで共用する学習カード初版です。

- 数学32枚：数と式、図形、関数、データの活用
- 理科32枚：エネルギー、粒子、生命、地球
- 社会32枚：地理、歴史、公民、資料と考察
- 単純な用語暗記だけでなく、公式・実験・図、地図・時系列・因果・資料読解をカード種別として保持します。

学習範囲は文部科学省の学習指導要領解説を参照していますが、問い・答え・解説は教科書本文を転載せず、ワカルートが独自に作成しています。推奨学年は国や教科書の公式配当ではありません。

## 再生成

```powershell
dotnet run --project tools/StudyCardGenerator/StudyCardGenerator.csproj
```

## API

- `GET /api/v1/study-cards?subject=math`
- `GET /api/v1/study-cards?subject=science`
- `GET /api/v1/study-cards?subject=social-studies&domain=history&recommendedGrade=2`
- `GET /api/v1/study-cards/{id}`
- `GET /api/v1/study-cards/dataset`
