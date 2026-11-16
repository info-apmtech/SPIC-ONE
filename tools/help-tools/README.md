# help-tools

Developer tools for the Help Centre guides in `SPIC.Core/HelpContent/`. Run them from the repository root.

```
dotnet run --project tools/help-tools -- check
dotnet run --project tools/help-tools -- translate [--module m] [--lang ta|te|mr|hi]
dotnet run --project tools/help-tools -- import <translated.md> --module m --lang ta|te|mr|hi
dotnet run --project tools/help-tools -- shoot [--module m] [--section id] [--desktop-only|--phone-only]
dotnet run --project tools/help-tools -- apply-reviews <help-corrections-xx-yyyymmdd.json>
```

| Command | What it does |
|---|---|
| `check` | Lists every screen (`@page` route) with no guide section, stale or missing translations, `for:` tokens that are not a `PagePermission`, missing screenshots, block mistakes (a flow step pointing at a stage, lane or screen that does not exist), tour steps whose `[data-help=…]` target no Razor page has, and role paths (`paths.json`) pointing at nothing. A screen with no phone picture is a warning. Exits 1 only for non-warning issues. Run it by hand before merging a screen change. |
| `translate` | Translates new or changed English sections with Claude, then back-translates each result to check the meaning; drifted sections are marked `confidence: low` so reviewers check them first. Only changed sections are sent. Needs `ANTHROPIC_API_KEY` (never commit it); model from `HELP_TOOLS_MODEL`, default `claude-opus-5-5`. |
| `import` | Brings in a translation made outside `translate` (by hand, an agency or an assistant): a copy of `guide.en.md` with only the human text translated. Refuses the file if a section or block item is missing or extra, or if an on-screen name in **bold** was translated. Writes it with the English routes, layout lines and current source hashes, labelled `confidence: ok`, so it shows as machine translated until reviewed. |
| `shoot` | Signs in to the **local** web app (`spicone-web-local`, against the local API and `spicone_dev`) as the local test users in `tools/help-demo/shots.json`, photographs each listed screen at 1280 wide (PNG) and 390 wide (JPEG), saves them in `SPIC.MauiBlazorApp.Shared/wwwroot/help/<module>/images/`, and writes the `screenshot` / `hotspots` / `screenshot-phone` / `hotspots-phone` keys in `guide.en.md`. Hotspots are measured from the section's tour targets, so markers and tour always match. Uses the installed Microsoft Edge (no browser download). Refuses any base URL that is not localhost. |
| `apply-reviews` | Writes native-speaker corrections (the JSON from **Download corrections** on `/Help/Review`) into the translation files, newest correction per section, skipping any made against older English. Prints pronunciation reports as ready-to-paste `glossary.json` entries. |

Pages that never need a guide are listed in `coverage-ignore.txt`.

How to write and maintain guides: `docs/help-desk.md`. Which module each remaining screen belongs to: `docs/help-module-map.md`.
