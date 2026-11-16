# help-tools

Developer tools for the Help Desk guides in `SPIC.Core/HelpContent/`. Run them from the repository root.

```
dotnet run --project tools/help-tools -- check
```

| Command | What it does |
|---|---|
| `check` | Lists every screen (`@page` route) with no guide section, stale or missing translations, `for:` tokens that are not a `PagePermission`, screenshots that don't exist, and guide files with format mistakes. Exits 1 when it finds anything. Run it by hand before merging a screen change. |

Pages that never need a guide are listed in `coverage-ignore.txt`.

How to write and maintain guides: `docs/help-desk.md`.
