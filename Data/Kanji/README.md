# 中学校漢字データ

`kanji.json` は、ワカルートのWeb・iOS・Androidで共用する中学校段階の漢字データです。

## 収録範囲

- 中学校学習指導要領が「中学校修了までに大体を読む」とする、小学校配当外の常用漢字1,110字
- 中1推奨350字、中2推奨400字、中3推奨360字
- 音読み、訓読み、画数、KANJIDIC2に収録された使用頻度順位

中1・中2・中3への割り振りは国の公式配当ではありません。KANJIDIC2の使用頻度順位を優先し、同順位または順位がない字は画数と文字コード順で並べた、ワカルート独自の初版学習順です。

## 更新方法

```powershell
dotnet run --project tools/KanjiCatalogGenerator/KanjiCatalogGenerator.csproj
```

生成時に中学校段階が1,110字であることと、学年別件数が350・400・360字であることを検証します。

## 出典とライセンス

- 学習目標: 文部科学省「中学校学習指導要領（平成29年告示）解説 国語編」
- 漢字・画数・読み・頻度: KANJIDIC2

KANJIDIC2はElectronic Dictionary Research and Development Groupの著作物で、CC BY-SA 4.0に基づいて利用しています。`kanji.json`のKANJIDIC2由来部分もCC BY-SA 4.0で提供します。詳細はJSON内の`license`と`NOTICE.md`を参照してください。
