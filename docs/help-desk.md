# Help Desk — how to add and maintain guides

The Help Desk (`/Help`, and the **?** button on every page) shows each signed-in user a plain-language
guide for the screens their role can use, in English, Tamil, Telugu, Marathi and Hindi, with a
**Listen** button. Design: `docs/superpowers/specs/2026-11-16-help-desk-design.md`.

## The rule

> When you add or change a screen, add or update its section in
> `SPIC.Core/HelpContent/<module>/guide.en.md` in the same change, run
> `help-tools translate`, and run `help-tools check` before merging.
> Screenshots use demo data only.

The check runs by hand only (there is no CI gate), so this rule is what keeps guides in step with the screens.

## Where things live

| What | Where |
|---|---|
| English guide (source of truth) | `SPIC.Core/HelpContent/<module>/guide.en.md` |
| Translations (generated, reviewed) | `SPIC.Core/HelpContent/<module>/guide.<ta\|te\|mr\|hi>.md` |
| Languages and voices | `SPIC.Core/HelpContent/help.config.json` |
| Glossary (terms kept, fixed spellings, how to say them) | `SPIC.Core/HelpContent/glossary.json` |
| Screenshots | `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/wwwroot/help/<module>/images/` |
| Tools | `tools/help-tools` (see its README) |

Guide files are embedded in `SPIC.Core`, so the web app, the offline MAUI app and the API (voice) all read the same text.

## Guide format

```markdown
---
module: publicity
title: Publicity Material Tracking
icon: bi-megaphone
order: 40
---

::: section raise-request
title: Raise a publicity request
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}
for: PublicityRequest.Entry
roles:
designations:

**What this screen is for** - ...

## Steps
1. Tap **New request**.
:::
```

- Header lines run until the first blank line. Lists are comma-separated.
- `routes:` are the screens the section explains. A user sees the section only if they can open at least one of
  them (the same rule the page guard uses). They also drive the **?** button. `{id}` matches one URL segment.
- `for:` (`Page.Action`), `roles:` (AppRole names) and `designations:` (designation names) only **narrow** who sees it.
- Body Markdown: `##`/`###` headings, paragraphs, `**bold**`, `1.` and `- ` lists, `> ` notes,
  `![alt](images/file.png)`. Everything else shows as plain text.
- Keep section ids stable: translations, review approvals and voice clips are keyed by them.
- Suggested section shape: *What this screen is for → Steps → Fields explained → Common problems*. Short steps,
  button names exactly as on screen.

## Commands

```
dotnet run --project tools/help-tools -- check
dotnet run --project tools/help-tools -- translate [--module m] [--lang ta|te|mr|hi]
dotnet run --project tools/help-tools -- apply-reviews <help-corrections-xx.json>
```

- `check` lists screens without a guide, stale or missing translations, unknown permissions and missing screenshots.
- `translate` re-translates only sections whose English changed (needs `ANTHROPIC_API_KEY`).
- `apply-reviews` writes native-speaker corrections downloaded from `/Help/Review` into the translation files.

## After deploying guide changes

An admin calls `POST /api/Help/audio/warm` once, so the voice clips for new or changed sections are generated before
anyone taps **Listen**. Clips are saved on the server and reused for every user; unchanged sections are never re-made.
