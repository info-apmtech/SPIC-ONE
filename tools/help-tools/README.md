# help-tools

Developer tools for the Help Desk guides in `SPIC.Core/HelpContent/`. Run them from the repository root.

```
dotnet run --project tools/help-tools -- check
dotnet run --project tools/help-tools -- translate [--module m] [--lang ta|te|mr|hi]
dotnet run --project tools/help-tools -- apply-reviews <help-corrections-xx-yyyymmdd.json>
```

| Command | What it does |
|---|---|
| `check` | Lists every screen (`@page` route) with no guide section, stale or missing translations, `for:` tokens that are not a `PagePermission`, screenshots that don't exist, and guide files with format mistakes. Exits 1 when it finds anything. Run it by hand before merging a screen change. |
| `translate` | Translates new or changed English sections with Claude, then translates each result back to English and compares it; sections whose meaning drifted are marked `confidence: low` so reviewers check them first. Only changed sections are sent. Needs `ANTHROPIC_API_KEY` (never commit it); model from `HELP_TOOLS_MODEL`, default `claude-opus-5-5`. A refused or cut-off section stays English and is listed. |
| `apply-reviews` | Writes native-speaker corrections (the JSON from **Download corrections** on `/Help/Review`) into the translation files, newest correction per section, skipping any made against older English. Prints pronunciation reports as ready-to-paste `glossary.json` entries. |

Pages that never need a guide are listed in `coverage-ignore.txt`.

How to write and maintain guides: `docs/help-desk.md`.
