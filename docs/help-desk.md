# Help Desk — how to add and maintain guides

The Help Centre (`/Help`, and the **?** button on every page) shows each signed-in user a plain-language
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

## Guide format (v2)

A guide is one Markdown file per module and language. The English file is the source of truth; translations
copy its layout and change only the human text. See `SPIC.Core/HelpContent/publicity/guide.en.md` for a full example.

### Front matter

```markdown
---
module: publicity
title: Publicity Material Tracking
icon: bi-megaphone
order: 40
category: Marketing          # banner eyebrow, translated
color: #0b6b3a               # module colour on cards and banner
example: IND-1042 · Banner 6x3   # demo label on the banner's example journey (never real data)
updated: 2026-11-16          # shown on the banner and in What's new
note: What changed           # What's new line, translated
---
```

### Blocks (one of each per module, all optional)

Every block is a `::: section` with a `kind:` header. Its body is an optional lead paragraph, then items. An item's
first line is `- <id> | key: value | …` (layout, English only); its indented lines are `field: text` (translated)
and `- bullet` lines (translated).

| kind | Shows | Item layout keys | Item text fields |
|---|---|---|---|
| `overview` | Lead text (also the banner text) and 3 fact cards | `icon`, `color` | `title`, `text` |
| `flow` | Swimlane on wide screens, vertical timeline on phones, and the banner's example journey | lanes: `type: lane`, `color`, `roles`, `designations`; stages: `lane`, `col`, `icon`, `color`, `next` (comma list), `return`, `screen`, `kind: exception` + `from` / `to` | lanes: `title`, `sub`; stages: `title`, `status`, `text` |
| `statuses` | Every status as a coloured tag | `color` | `label`, `text` |
| `roles` | Who-does-what cards; the user's card is marked "You" | `color`, `roles`, `designations` | `title`, bullets = tasks |
| `faq` | Problems and answers; `popular: true` items also show on the home page | `popular` | `q`, `a` |

Blocks are shown to a user only when they can open at least one of the module's screens. Keep `status:` and
`label:` values exactly as the screen shows them (they stay English in translations).

### Screen sections

```markdown
::: section requester-list
title: See and manage my requests
routes: /PublicityRequest
for: PublicityRequest.View          # optional narrowing, as before
minutes: 2
tour: 1 > [data-help=pub-cards] | 2 > [data-help=pub-search] | …
screenshot: requester-list.png               # written by help-tools shoot
hotspots: 1 @ 2.2,12.2,95.5,33.6 | …         # written by help-tools shoot
screenshot-phone: requester-list.phone.jpg
hotspots-phone: 1 @ … | …

**What this screen is for**
Intro text, shown above the steps.

1. Step one. Step N goes with hotspot N and tour step N.
2. Step two.

> Tip: A tip box.
> Careful: A warning box.

| Field | What to enter |
|---|---|
| **Quantity** | Number of pieces. |

## Common problems

- **Problem.** Answer.
:::
```

- Header lines run until the first blank line. Lists are comma-separated.
- `routes:` decide who sees the section (the same rule the page guard uses) and drive the **?** button. `{id}`
  matches one URL segment. `for:`, `roles:` and `designations:` only narrow it.
- The **first numbered list is the step list**. Hotspots and tour steps must match its count (the parser and
  `check` report a mismatch). Keep other lists and tables after it.
- **Tours** are for each role's main tasks only (about 3–5 per module). Each tour step targets an element marked
  `data-help="<id>"` on the real page; add the attribute to the page in the same change. A screen whose every route
  needs a record id (`{id}`) offers its tour only from **?** while the user is on such a record.
- `Tip:` and `Careful:` are fixed English keywords, also in translations.
- Keep section and item ids stable: translations, review approvals and voice clips are keyed by them.

### Screenshots

Run `help-tools shoot` with the local API and `spicone-web-local` running. It signs in as the local test users in
`tools/help-demo/shots.json` and photographs each listed screen at desktop and phone size. Add a line to
`shots.json` for every new screen guide. Pictures come from local demo data only, never production.

### Role paths and support

- `SPIC.Core/HelpContent/paths.json`: the "Your path" steps on the home page, per role or designation.
- `help.config.json` → `support`: phone, WhatsApp and hours for the "Still stuck?" box. The box stays hidden while
  both numbers are empty.

## Commands

```
dotnet run --project tools/help-tools -- check
dotnet run --project tools/help-tools -- translate [--module m] [--lang ta|te|mr|hi]
dotnet run --project tools/help-tools -- import <translated.md> --module m --lang ta|te|mr|hi
dotnet run --project tools/help-tools -- shoot [--module m] [--section id]
dotnet run --project tools/help-tools -- apply-reviews <help-corrections-xx.json>
```

- `check` lists screens without a guide, stale or missing translations, unknown permissions and missing screenshots.
- `translate` re-translates only sections whose English changed (needs `ANTHROPIC_API_KEY`).
- `import` brings in a translation made by hand or by someone else, after checking every section is there and on-screen names stayed English.
- `shoot` takes the screenshots from the local app and writes the picture and hotspot keys.
- `apply-reviews` writes native-speaker corrections downloaded from `/Help/Review` into the translation files.

## Feedback

Every guide page ends with "Was this guide helpful?". Answers are stored per user, guide and day (V33,
`HelpFeedbacks`) and reviewers see the totals on the **Feedback** tab of `/Help/Review`, with the guides people found
least helpful first.

## After deploying guide changes

An admin calls `POST /api/Help/audio/warm` once, so the voice clips for new or changed sections are generated before
anyone taps **Listen**. Clips are saved on the server and reused for every user; unchanged sections are never re-made.
