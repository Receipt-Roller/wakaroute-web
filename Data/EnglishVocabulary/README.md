# WakaRoute English vocabulary dataset

`english-words.json` is the shared vocabulary source for the WakaRoute website and future mobile clients.

- 650 high-frequency words are marked as `elementary-review`.
- The following 1,800 words are marked as `junior-high` and split into three editorial groups of 600.
- This is a WakaRoute learning sequence, not a national or textbook-specific grade assignment.
- The Ministry of Education specifies approximate vocabulary counts, but does not publish one nationwide word-by-word grade list.

The dataset combines NGSL 1.2 frequency order with Japanese definitions from EJDict-hand. See `NOTICE.md` for licensing.

## Regenerate

```powershell
dotnet run --project tools/EnglishVocabularyGenerator/EnglishVocabularyGenerator.csproj
```

The generator validates item counts, grade counts, unique IDs, and contiguous sequence numbers before writing the file.

## API

- `GET /api/v1/english-words`
- `GET /api/v1/english-words?stage=junior-high&recommendedGrade=1&q=help`
- `GET /api/v1/english-words/{id}`
- `GET /api/v1/english-words/dataset`
