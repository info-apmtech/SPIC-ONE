# Help Centre Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Help Desk into the help centre in the approved mockups:
- an animated home page;
- module pages with a role swimlane flow, status tags, screenshots with numbered markers, FAQ and
  feedback;
- guided tours on the real screens.

The three existing guides move to the new format, in five languages.

**Architecture:**
- Every new block (overview, flow, statuses, roles, faq) is still a `::: section` with a `kind:`
  header. Translation, import, review, the voice cache and the coverage check therefore keep
  working per section.
- Layout keys sit on item header lines and are always taken from English. Text lines are
  translated.
- New Razor components in the shared library render the blocks. One JS file draws flow
  connectors, tracks scrolling and runs the tour.

**Tech Stack:**
- .NET 10.
- Blazor shared Razor library for the web and MAUI hosts.
- ASP.NET Core API with EF Core and PostgreSQL.
- SPIC.RegressionHarness (`Check.That` / `Check.Equal`).
- `tools/help-tools`, with Microsoft.Playwright for screenshots, driving the installed Edge.

**Spec:** `docs/superpowers/specs/2026-11-16-help-centre-redesign-design.md`. The mockups are in
`docs/superpowers/specs/help-centre-mockups/`: `index.html`, `module.html` and `tour.html`.
Open them; they are the visual reference for every UI task.

## Global Constraints

- Work on branch `Satham`. Stages 1–3 (this plan) ship together in one PR from `Satham` into
  `azure-deploy`.
- Local data only. Use the API against `Host=localhost;Port=5432;Database=spicone_dev`, and the
  web app through the `spicone-web-local` launch entry.
  - Never use the appsettings connection string. Never use the `spicone-web` launch entry, which
    points at the live API.
  - The local PostgreSQL service may be stopped. Start it with `pg_ctl` as the current user.
- The harness build rebuilds SpicAPI and kills a running local API. Restart the API after each
  harness run.
- Migrations are dry-run on the local database before anyone touches prod. The user runs prod
  deploys.
- No API keys or real passwords are committed. Demo users exist only in the local database.
- Layout data (lanes, colours, icons, columns, hotspots, tour targets, screenshots) is always
  copied from English. A translation changes text only.
- Button and field names stay in English inside translations, as the v1 import rule already
  enforces.
- All motion stops under `prefers-reduced-motion`.
- Breakpoints:
  - 1180px: the right column is hidden;
  - 900px: one column, a chip menu, and a vertical flow;
  - 520px: the compact banner, with the floating cards hidden.
  - Nothing may scroll sideways at 375px.
- Brand: `--primary` #0b6b3a, `--accent` #2bb673, font Poppins, Bootstrap Icons.
- Harness baseline: 9 failures existed before this work. A task is green when the count is still
  9 and every new check passes.
- Harness command:

  ```
  dotnet run --project SPIC.RegressionHarness > "$env:TEMP\harness.txt" 2>&1
  Select-String -Path "$env:TEMP\harness.txt" -Pattern "passed|FAIL" | Select-Object -Last 15
  ```

  It takes about 6 minutes, so run it in the background.
- Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Plan rulings (where this plan refines the spec)

- **Banner text.** The spec's front-matter `summary:` is the overview block's lead paragraph
  instead. It is then translated, hashed and reviewed like every other section, and there is no
  untracked translated front matter.
- **Category and note.** These are the only translated front-matter values. They carry a
  `meta-source-hash`, so a stale translation falls back to English.
- **Layout changes mark translations stale.** Hashes still cover a whole section body,
  including item header lines. Changing a colour in English therefore re-flags that block's
  translations. A re-import with the same text clears it. This is a safe default, and a cheap
  re-stamp.
- **No assistant and no microphone** (spec §3). The mockups' chat button and microphone are not
  built.

## Review Focus

1. **A translated block whose layout lines differ from English** (a translator changed a colour,
   lane or column). The page shows the English layout with the translated text. Pinned in
   Task 2 (`MergeLayoutKeepsEnglishLayout`).
2. **A user who can open none of a module's screens.** The module, its flow, roles, statuses and
   FAQ are all hidden, including from home search and popular questions. Pinned in Task 8
   (`BlocksHiddenWhenNoScreenVisible`).
3. **A tour step whose target is not on the page** (hidden by role, not loaded yet, or a record
   screen). The step is skipped and the card says so. Nothing is highlighted at an empty spot.
   Pinned in Task 12 (manual check with a role that lacks the target).
4. **A section with no phone picture, viewed on a phone.** The desktop picture is shown and the
   markers stay on the right spots, because positions are in %. Pinned in Task 10.
5. **An old v1-format translation file** (no `kind:`, no screen keys) next to a v2 English
   guide. It loads without errors and its sections show as stale English until re-imported.
   Pinned in Task 3 (`V1TranslationAgainstV2EnglishFallsBack`).

---

## Part 1 — Guide format (SPIC.Core)

### Task 1: Guide model and parser v2

**Files:**
- Modify: `SPIC.Core/Help/HelpModels.cs`, `SPIC.Core/Help/HelpGuideParser.cs`
- Create: `SPIC.Core/Help/HelpSteps.cs`
- Test: `SPIC.RegressionHarness/HelpParserV2Tests.cs`; register `HelpParserV2Tests.Run()` in
  `SPIC.RegressionHarness/Program.cs` after `HelpImportTests.Run();`

**Interfaces:**
- Produces:
  - On `HelpGuide`, init-only properties, so existing constructors stay valid:
    - `string Category = ""`, `string Color = "#0b6b3a"`, `string Example = ""`, `string Note = ""`;
    - `DateOnly? Updated`;
    - `string? MetaSourceHash`, read from `meta-source-hash:` and used by translations.
  - On `HelpSection`: `string Kind { get; init; } = "screen"` and `HelpScreenMeta? Screen { get; init; }`.
  - Records:
    - `HelpHotspot(int N, double X, double Y, double W, double H)`;
    - `HelpTourStep(int N, string Target)`;
    - `HelpScreenMeta(string? Image, IReadOnlyList<HelpHotspot> Hotspots, string? PhoneImage, IReadOnlyList<HelpHotspot> PhoneHotspots, IReadOnlyList<HelpTourStep> Tour, int Minutes)`.
  - `public static class HelpSteps { public static IReadOnlyList<string> Of(string body); }`
    returns the items of the first numbered list (`1. …`) in the body, with no number prefix.
  - `public static readonly string[] HelpKinds = { "screen", "overview", "flow", "statuses", "roles", "faq" };`
    on `HelpGuideParser`.

- [ ] **Step 1: Write the failing tests.**

  `ParsesFrontMatterV2`:
  - front matter `category: Marketing`, `color: #0d9488`, `example: IND-1042 · Banner 6x3`,
    `updated: 2026-11-14`, `note: Transfer added`;
  - check each property, with `Updated == new DateOnly(2026,11,14)`.

  `ParsesScreenMeta`. A section with these headers:
  ```
  screenshot: list.png
  hotspots: 1 @ 2,4,96,18 | 2 @ 2,26,96,9
  screenshot-phone: list.phone.webp
  hotspots-phone: 1 @ 4,10,92,20 | 2 @ 4,34,92,8
  tour: 1 > [data-help=pub-status-cards] | 2 > [data-help=pub-search]
  minutes: 2
  ```
  and a body with the steps `1. A` and `2. B`. Check:
  - `Screen.Hotspots[1] == new HelpHotspot(2,2,26,96,9)`;
  - `Screen.Tour[0].Target == "[data-help=pub-status-cards]"`;
  - `Screen.Minutes == 2`;
  - `HelpSteps.Of(body) == ["A","B"]`.

  `KindDefaultsToScreen`: a v1 section has `Kind == "screen"`, `Screen` not null, and no hotspots.

  `ReportsMetaProblems`. Each case produces exactly one problem that names the section id:
  - 3 hotspots and 2 steps → contains `"hotspot"`;
  - 1 tour step and 2 steps → contains `"tour"`;
  - `hotspots: 1 @ x` → contains `"hotspot"`;
  - `hotspots: 1 @ 2,4,120,18` → contains `"0–100"`;
  - `kind: chart` → contains `"kind"`, and the section is kept as `screen`.

- [ ] **Step 2: Run the harness.**
  Expected: the build fails on the missing members. That is the red state.

- [ ] **Step 3: Implement.**
  - The parser reads the new front-matter and section keys.
  - It validates hotspot and tour counts against `HelpSteps.Of(body)` when either list is
    present.
  - Hotspot numbers must be 1..N in order.

- [ ] **Step 4: Run the harness.**
  Expected: the new checks pass; failures are still 9.

- [ ] **Step 5: Commit**
  `feat(help): guide format v2 front matter, section kinds and screen meta`

### Task 2: Block items, typed views, layout merge and speech text

**Files:**
- Create: `SPIC.Core/Help/HelpBlocks.cs`
- Test: `SPIC.RegressionHarness/HelpBlocksTests.cs` (registered after `HelpParserV2Tests.Run()`)

**Item grammar.** A block body is optional lead paragraphs, then items:
```
- <id> | key: value | key: value        ← header line: id + layout keys (English only)
  title: text                           ← text fields (translated)
  text: text
  - bullet text                         ← bullets (translated; roles' tasks)
```

**Interfaces:**
- Consumes: `HelpSection.Kind` from Task 1.
- Produces:
  - `HelpBlockItem(string Id, IReadOnlyDictionary<string,string> Layout, IReadOnlyDictionary<string,string> Text, IReadOnlyList<string> Bullets)`.
  - `HelpBlocks.Lead(string body) -> string`, the paragraphs before the first item.
  - `HelpBlocks.Items(string body, List<string>? problems = null) -> IReadOnlyList<HelpBlockItem>`.
  - `HelpBlocks.MergeLayout(string englishBody, string translatedBody) -> string`. The result
    keeps the English item order and header lines, takes text fields, bullets and lead from the
    translation per item id, and falls back to English text per field.
  - `HelpBlocks.SpeechText(HelpSection s) -> string`:
    - a screen uses `HelpMarkdown.ToSpeech(title, body)`;
    - a block uses the title, the lead, then each item's `title`, `status`, `text`, `q`, `a` and
      bullets, as sentences in item order.
  - `HelpBlocks.TextLines(HelpSection s) -> IReadOnlyList<string>`: every translatable text value
    (lead, fields, bullets). It is used by the import's bold check and by review.
  - Typed views. Each takes a `HelpSection` and a `List<string> problems`:
    - `Overview → IReadOnlyList<HelpOverviewCard(Id, Icon, Color, Title, Text)>`;
    - `Flow → HelpFlowView(IReadOnlyList<HelpLane> Lanes, IReadOnlyList<HelpStage> Stages)`, where:
      - lanes are items with `type: lane`:
        `HelpLane(Id, Color, IReadOnlyList<string> Roles, IReadOnlyList<string> Designations, Title, Sub)`;
      - stages are the other items:
        `HelpStage(Id, Lane, Col, Icon, Color, Screen?, IReadOnlyList<string> Next, Return?, Exception, From?, To?, Title, Text, Status)`;
    - `Statuses → HelpStatusTag(Id, Color, Label, Text)`;
    - `Roles → HelpRoleCard(Id, Color, Roles, Designations, Title, IReadOnlyList<string> Tasks)`;
    - `Faq → HelpFaqItem(Id, bool Popular, Question, Answer)`.
  - `HelpBlocks.Validate(HelpGuide english) -> IReadOnlyList<string>`. It reports:
    - duplicate item ids;
    - a stage lane that is not a lane;
    - `next`, `return`, `from` or `to` that is not a stage;
    - `screen` that is not a screen section id;
    - `col` that is not a positive integer;
    - a flow with no lanes;
    - more than one block of the same kind.

- [ ] **Step 1: Write the failing tests.**
  - `ParsesItems`: a roles body with 2 items and bullets. Check ids, Layout `color`, Text
    `title`, and that Bullets has 3 entries.
  - `MergeLayoutKeepsEnglishLayout`: the English stage header is
    `- raise | lane: rmdo | col: 1 | color: #64748b`. The translation has
    `- raise | lane: vendor | col: 9 | color: red`, with `title: உருவாக்கு`. Merging gives the
    English header line and the Tamil title.
  - `MergeLayoutFallsBackPerField`: the translation lacks `text:` for one item, so the merged
    item has the English `text`. An extra translated item `bogus` is dropped.
  - `FlowViewAndValidation`: lanes `rmdo` and `approver`; stages `raise` (next `approve`) and
    `approve` (`return: raise`, `screen: requests-list`). Validating against a guide with screen
    `requests-list` gives no problems. Changing to `next: nowhere` gives one problem containing
    `nowhere`.
  - `SpeechTextOfFlow`: contains `"Raise and submit."` before `"Check and approve."`, and
    contains no `|` or `color`.
  - `TextLinesOfFaq`: contains the question and the answer, and nothing from header lines.

- [ ] **Step 2: Run the harness.** Expected: FAIL to build.

- [ ] **Step 3: Implement `HelpBlocks`.**
  - Header lines are lines starting with `- ` at column 0. Fields are lines with two-space indent
    and `name:`. Bullets are lines with two-space indent and `- `.
  - Parse problems: an item with no id, or an indented line before any item.

- [ ] **Step 4: Run the harness.** Expected: new checks pass; still 9 failures.

- [ ] **Step 5: Commit**
  `feat(help): block items, flow/roles/statuses/faq views, layout merge`

### Task 3: Library, writer and import understand v2

**Files:**
- Modify: `SPIC.Core/Help/HelpLibrary.cs`, `SPIC.Core/Help/HelpTranslationFiles.cs`,
  `SPIC.Core/Help/HelpTranslationImport.cs`
- Test: add to `SPIC.RegressionHarness/HelpLibraryTests.cs` and `HelpImportTests.cs`

**Interfaces:**
- Consumes: `HelpBlocks.MergeLayout` and `TextLines` (Task 2), `HelpSection.Kind` and `Screen`
  (Task 1).
- Produces:
  - `HelpLibrary.Localized`:
    - block sections get `Body = HelpBlocks.MergeLayout(source.Body, t.Body)`;
    - `Kind` and `Screen` always come from English;
    - the guide's `Category` and `Note` come from the translation only when its
      `MetaSourceHash == HelpHash.Of(en.Category, en.Note)`.
  - `HelpGuideWriter`:
    - writes `kind:` for non-screen sections;
    - writes `screenshot`, `hotspots`, `screenshot-phone`, `hotspots-phone`, `tour` and `minutes`
      from English;
    - writes front matter `category` and `note` from translated keys `_category` and `_note`
      (constants `CategoryKey` and `NoteKey`) when present, plus `meta-source-hash`.
  - `HelpTranslationImport.Build` adds:
    - `"{id}: item {itemId} missing"` and `"{id}: item {itemId} is not in English"` for block
      sections;
    - the bold screen-name rule, applied to `TextLines` of block sections.

- [ ] **Step 1: Write the failing tests.**
  - `LocalizedMergesBlockLayout`: an English flow, plus a Tamil file with a changed lane and a
    Tamil title. `Localized("m","ta")` has the English layout and the Tamil title.
  - `V1TranslationAgainstV2EnglishFallsBack`: a Tamil v1 file whose source hashes do not match.
    Every section is English, `Problems` is empty, and nothing throws.
  - `WriterKeepsKindAndScreenMeta`: write, then parse. `Kind == "flow"` and the screen hotspots
    equal the English ones.
  - `ImportReportsMissingBlockItem` and `ImportChecksBoldInBlockText`: an FAQ answer translated
    `**Clear All**` → one problem naming `Clear All`.

- [ ] **Step 2: Run the harness.** Expected: the new checks FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run the harness.** Expected: PASS; still 9 failures.
- [ ] **Step 5: Commit**
  `feat(help): translations keep English layout for v2 blocks and screen meta`

### Task 4: Renderer (tables, callouts), speech and tour-step voice

**Files:**
- Modify: `SPIC.Core/Help/HelpMarkdown.cs`, `SpicAPI/Controllers/HelpController.cs`,
  `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/Services/HelpApi.cs`
- Test: `SPIC.RegressionHarness/HelpMarkdownTests.cs` (new, registered), plus additions to
  `HelpSpeechTests.cs`

**Interfaces:**
- `HelpMarkdown.ToHtml`:
  - a pipe table with a `|---|` row becomes `<table class="hc-fields">` with `<th>` and `<td>`;
  - `> Tip: x` becomes `<div class="hc-callout hc-tip"><b>Tip</b> x</div>`;
  - `> Careful: x` becomes the same with `hc-warn` and `Careful`.
  - Labels are matched in English only. Translations keep `Tip:` and `Careful:` as fixed English
    keywords; the page shows a localized label from a small lookup in the component.
- `HelpMarkdown.ToSpeech` reads a table row as `"<cell1>: <cell2>."` and a callout as
  `"Tip. x"`.
- `HelpController`:
  - the 3 calls to `HelpMarkdown.ToSpeech(s.Title, s.Body)` become `HelpBlocks.SpeechText(s)`;
  - new `GET audio/key/{module}/{sectionId}/{lang}/step/{n}` and
    `GET audio/{module}/{sectionId}/{lang}/step/{n}` speak `HelpSteps.Of(body)[n-1]`. They return
    404 when out of range, and use the same cache and headers as the section clip;
  - `WarmAudio` also queues step clips for sections whose `Screen.Tour` is not empty.
- `HelpApi`: `AudioKeyAsync(string module, string section, string lang, int? step = null)` and
  `AudioUrl(…, int? step = null)`.

- [ ] **Step 1: Write the failing tests.**
  - `TableToHtml` (one header row and 2 body rows).
  - `CalloutsToHtml`.
  - `TableToSpeech`.
  - `StepTextForTour`: `HelpSteps.Of` on a body with a callout and a table between the steps
    still returns only the numbered items.
- [ ] **Step 2: Run the harness.** Expected: FAIL.
- [ ] **Step 3: Implement the renderer and the controller changes.**
- [ ] **Step 4: Run the harness.** Expected: PASS, still 9 failures.
- [ ] **Step 5: Smoke test against the local API.**
  - Start the API on 5131 against `spicone_dev`.
  - `GET /api/Help/audio/status` → `enabled:false`, because there is no Speech key locally.
  - The step endpoint returns 404 while voice is disabled, the same as the section clip.
- [ ] **Step 6: Commit**
  `feat(help): tables, callouts, block speech and tour-step voice clips`

### Task 5: Paths, support config and What's new

**Files:**
- Modify: `SPIC.Core/Help/HelpConfig.cs`, `SPIC.Core/Help/HelpLibrary.cs`,
  `SPIC.Core/HelpContent/help.config.json`
- Create: `SPIC.Core/Help/HelpPaths.cs`, `SPIC.Core/HelpContent/paths.json` (empty `paths`
  array for now)
- Test: `SPIC.RegressionHarness/HelpPathsTests.cs`

**Interfaces:**
- `HelpConfig` gains
  `HelpSupport? Support = null`, where
  `HelpSupport(string Phone, string WhatsApp, IReadOnlyDictionary<string,string> Hours)` is
  deserialized from `"support"`. Phone and WhatsApp are empty strings in the committed file.
- `paths.json`:
  `{ "paths": [ { "id": "rmdo", "roles": [], "designations": ["RMDO"], "title": { "en": "...", "ta": "..." }, "steps": [ { "module": "publicity", "section": "raise-request", "minutes": 5, "tour": true } ] } ] }`.
- Records:
  - `HelpPath(string Id, IReadOnlyList<string> Roles, IReadOnlyList<string> Designations, IReadOnlyDictionary<string,string> Title, IReadOnlyList<HelpPathStep> Steps)`;
  - `HelpPathStep(string Module, string Section, int Minutes, bool Tour)`.
- `HelpLibrary.Paths` and `HelpLibrary.Support`.
- `HelpWhatsNew.From(IEnumerable<HelpGuide> guides, int max) -> IReadOnlyList<HelpWhatsNewItem(Module, Title, DateOnly Updated, Note)>`:
  newest first, skipping guides without `Updated`.

- [ ] **Step 1: Write the failing tests.**
  - `LoadsPaths`: two paths with titles per language.
  - `SupportDefaultsEmpty`.
  - `WhatsNewOrdersNewestFirstAndSkipsUndated`.
- [ ] **Step 2: FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: PASS**, still 9 failures.
- [ ] **Step 5: Commit**
  `feat(help): role paths, support settings and what's new`

### Task 6: Coverage check v2

**Files:**
- Modify: `SPIC.Core/Help/HelpCoverage.cs`, `tools/help-tools/Program.cs`,
  `tools/help-tools/Repo.cs`
- Test: add to `SPIC.RegressionHarness/HelpCoverageTests.cs`

**Interfaces:**
- `CoverageIssue` gains `bool Warning { get; init; }`.
- `help-tools check` exits 1 only when a non-warning issue exists, and prints warnings marked
  `(warning)`.
- `HelpCoverage.Check` gains the parameters `IReadOnlySet<string> dataHelpIds` and
  `IReadOnlyList<HelpPath> paths`. It adds these issue kinds:
  - `block` (each `HelpBlocks.Validate` problem);
  - `missing-image` (desktop `screenshot:` file missing);
  - `missing-phone-image` (Warning; also when a screen has `screenshot` but no
    `screenshot-phone`);
  - `tour-target` (a `[data-help=x]` whose `x` is not in `dataHelpIds`);
  - `path` (a path step whose module or section does not exist, or whose section is not a
    screen).
- `Repo.DataHelpIds()` scans `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/**/*.razor` for
  `data-help="…"`.

- [ ] **Step 1: Write the failing tests**, one check per new kind, plus `PhoneImageIsWarning`.
- [ ] **Step 2: FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: PASS.** Then run `dotnet run --project tools/help-tools -- check`.
  Expected: the same 195 uncovered routes as before and no new non-warning issue, because the
  three guides are still v1.
- [ ] **Step 5: Commit**
  `feat(help-tools): check blocks, images, tour targets and paths`

---

## Part 2 — API

### Task 7: Feedback storage (V33) and endpoints

**Files:**
- Create: `SPIC.Core/Entities/HelpFeedback.cs`, `SPIC.Core/Help/HelpFeedbackRules.cs`,
  `SPIC.Core/DTOs/HelpFeedbackDtos.cs`
- Modify: `Spic.Infrastructure/Data/AppDbContext.cs` (DbSet and configuration next to
  `HelpReviewItems`), `SpicAPI/Controllers/HelpController.cs`,
  `SpicAPI/Controllers/HelpReviewController.cs`
- Create (generated): `Spic.Infrastructure/Migrations/*_V33_HelpFeedback.cs`
- Test: `SPIC.RegressionHarness/HelpFeedbackTests.cs`

**Interfaces:**
- The entity `HelpFeedback` has:
  - `int Id`;
  - `string Module` (64), `string? SectionId` (64), `string Language` (8);
  - `bool Helpful`, `string? Comment` (500);
  - `int EmployeeId`;
  - `DateTime CreatedAtUtc`, `DateOnly Day`.
  - Unique index on (EmployeeId, Module, SectionId, Day).
- `HelpFeedbackRules.Validate(HelpFeedbackDto dto, HelpLibrary library) -> string?` returns an
  error message or null:
  - the module must exist;
  - the section must be a section of that module, or null;
  - the language must be configured;
  - the comment must be 500 characters or fewer after trimming.
- `POST api/Help/feedback` (signed-in users) takes
  `HelpFeedbackDto(string Module, string? SectionId, string Language, bool Helpful, string? Comment)`.
  - It upserts today's row for the user, then returns `{ success: true }`.
  - It returns 400 with the rule message.
- `GET api/HelpReview/feedback?days=90` uses the same permission check as the existing GET
  endpoints in `HelpReviewController`. It returns
  `List<HelpFeedbackSummaryDto(string Module, string? SectionId, int Yes, int No, IReadOnlyList<string> RecentComments)>`,
  with up to 5 recent non-empty comments.

- [ ] **Step 1: Write the failing tests:** `ValidateRejectsUnknownModule`, `…UnknownSection`,
  `…UnknownLanguage`, `…LongComment`, `ValidateAcceptsModuleLevelVote`.
- [ ] **Step 2: FAIL.**
- [ ] **Step 3: Implement the entity, rules, DbSet and endpoints.**
  Then run
  `dotnet ef migrations add V33_HelpFeedback --project Spic.Infrastructure --startup-project SpicAPI`.
- [ ] **Step 4: Dry-run the migration locally.**
  - Run
    `dotnet ef migrations script <V32 id> V33_HelpFeedback --project Spic.Infrastructure --startup-project SpicAPI`
    and read the SQL. Expected: one `CREATE TABLE "HelpFeedbacks"` and one unique index, with no
    other table touched.
  - Apply it to `spicone_dev` with the local connection string.
- [ ] **Step 5: Smoke test against the local API.**
  - Sign in as the local test user.
  - POST a vote, then POST it again: the row count stays 1.
  - A comment over 500 characters → 400.
  - GET the summary → Yes=1.
- [ ] **Step 6: Add the Feedback tab.**
  - Add a "Feedback" tab to `Pages/Help/HelpReview.razor`, through
    `HelpApi.FeedbackSummaryAsync(int days)`.
  - The table shows module and screen, Yes, No, and the recent comments. Sort it by most "No"
    first.
  - Check it in the browser as the local reviewer user.
- [ ] **Step 7: Run the harness.** Expected: PASS, still 9 failures.
- [ ] **Step 8: Commit**
  `feat(help): store "was this helpful" feedback (V33) and show it to reviewers`

---

## Part 3 — Help centre UI (shared Razor library)

The visual reference for Tasks 9–13 is the three mockups. Port their CSS to the `hc-` class
prefix and keep the layout, sizes, colours and animations. Check each component at 1440px and
375px in the browser pane, using `spicone-web-local` against the local API.

### Task 8: Catalog v2 and reading progress

**Files:**
- Modify: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/Services/HelpCatalog.cs`
- Create: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/Services/HelpProgressState.cs`
  (registered next to `HelpLanguageState` in both hosts' DI)
- Test: add to `SPIC.RegressionHarness/HelpCatalogTests.cs`

**Interfaces:**
- Visibility:
  - a section with `Kind != "screen"` is visible only when its module has at least one visible
    screen section;
  - screen visibility is unchanged from v1.
- `HelpModuleView` gains:
  - `HelpGuide Guide`, the localized guide with its meta;
  - `IReadOnlyList<HelpSectionView> Screens`;
  - `HelpSectionView? Block(string kind)`;
  - `string Lead`, the overview lead, used as the banner text.
- New catalog methods:
  - `IReadOnlyList<(string Module, HelpFaqItem Item)> PopularFaq(string lang, int max = 6)`;
  - `HelpPathView? PathFor(string lang)`: the first path matching the login role or
    designation, with steps limited to visible screens. Returns
    `HelpPathView(string Title, IReadOnlyList<(HelpPathStep Step, HelpSectionView Section)> Steps)`;
  - `IReadOnlyList<HelpWhatsNewItem> WhatsNew(string lang, int max = 5)`, visible modules only;
  - `string? MyRoleCard(HelpModuleView m)`: the id of the roles-block card matching the login's
    role or designation;
  - `string? MyLane(HelpModuleView m)`: the same match for flow lanes.
- `Search` also matches `HelpBlocks.SpeechText` of visible blocks and the screens' step text.
- `HelpProgressState`:
  - `Task<IReadOnlySet<string>> ReadAsync()`;
  - `Task MarkReadAsync(string module, string section)`;
  - storage key `help.read.<employeeId>` in the same device store `HelpLanguageState` uses.

- [ ] **Step 1: Write the failing tests.**
  - `BlocksHiddenWhenNoScreenVisible`: a user who can open none of the module's routes sees no
    module, no PopularFaq from it, and no search hit in its FAQ.
  - `BlocksVisibleWithOneScreen`.
  - `PathMatchesDesignationAndDropsHiddenSteps`.
  - `MyRoleCardByDesignation`.
  - `WhatsNewOnlyVisibleModules`.
- [ ] **Step 2: FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: PASS**, still 9 failures.
- [ ] **Step 5: Commit**
  `feat(help): catalog v2 (blocks, paths, popular FAQ, what's new) and reading progress`

### Task 9: Styles, script and the animated banner

**Files:**
- Replace: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/wwwroot/css/Help.css`. Port from the
  mockups; prefix every class `hc-`; keep the v1 classes still used by `HelpPanel` and
  `HelpReview`.
- Create: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/wwwroot/js/help-centre.js`
- Modify: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/Components/App.razor` and
  `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/wwwroot/index.html`, to add the script after
  `app-interop.js`.
- Create: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/Components/Help/HelpBanner.razor`

**Interfaces:**
- `window.helpCentre` functions:
  - `drawFlow(containerId)`, which draws SVG connectors between `[data-stage]` elements using
    `data-next`, `data-return`, `data-from` and `data-to`, and redraws on resize;
  - `observeToc(sectionIds, dotnetRef)`, which calls `OnTocChanged(id)`;
  - `observeRead(sectionIds, dotnetRef, ms)`, which calls `OnSectionRead(id)` after `ms`
    milliseconds in view;
  - `isNarrow()`, true when width is 900px or less;
  - `print()`;
  - the tour functions in Task 12.
- `HelpBanner` parameters:
  - `HelpModuleView Module`;
  - `IReadOnlyList<HelpStage> Stages`, drawn in `Col` order without exception stages;
  - `int Read`, `int Total`;
  - `string? NextTitle`;
  - `bool ShowPdf`;
  - `EventCallback OnTour`, `EventCallback OnListen`, `EventCallback OnPdf`.
- The banner's example journey uses `Module.Guide.Example` and the stage labels. A module with
  no flow shows the icon tile only, with no journey card.

- [ ] **Step 1: Port the CSS and JS.** Put the banner on a temporary page
  `/Help/_banner-preview`, removed in Task 10, showing the publicity module.
- [ ] **Step 2: Check it in the browser at 1440px and 375px.** Expected:
  - it matches the mockup banner, including the motion;
  - the floating cards are hidden at 375px;
  - nothing scrolls sideways;
  - with reduced motion emulated through `matchMedia` in devtools, nothing moves and all the
    stages show as lit.
- [ ] **Step 3: Commit**
  `feat(help): help centre styles, script and animated banner`

### Task 10: Module guide page

**Files:**
- Replace: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/Pages/Help/HelpGuide.razor`
- Create in `Components/Help/`:
  - `HelpFlow.razor`: a swimlane, plus a vertical timeline below 900px (CSS shows one of the
    two), and a step detail panel;
  - `HelpStatusList.razor`;
  - `HelpRoleCards.razor`;
  - `HelpScreen.razor`;
  - `HelpFaqList.razor`;
  - `HelpFeedbackBox.razor`;
  - `HelpToc.razor`.
- Modify: `Components/Help/HelpSectionBody.razor`, so that block sections render their text
  lines. The review screen keeps working.

**Interfaces:**
- Consumes: catalog v2 (Task 8), `HelpBlocks` views (Task 2), the banner and script (Task 9),
  feedback POST (Task 7) through `HelpApi.SendFeedbackAsync(HelpFeedbackDto)`.
- `HelpScreen` parameters: `HelpSectionView Screen`, `string Module`, `bool Phone`.
  - `Phone` is true when `IFormFactor.GetPlatform()` is not the web host, or when
    `helpCentre.isNarrow()` is true.
  - The image comes from `/_content/SPIC.MauiBlazorApp.Shared/help/<module>/images/<file>`. On a
    phone it uses `PhoneImage ?? Image`, and the hotspots that go with whichever image is shown.
  - Hotspots are placed with `left/top/width/height` in %. Tapping a step or a marker sets the
    active step, which moves the ring.
  - With no image, only the steps show.
  - On a `{id}` route the "Tour" button is replaced by the note "Open a request to take this
    tour". "Open screen" goes to the first route without parameters, or the module list page.
- Page order (spec §2): banner, then overview, flow, statuses, roles, screens, FAQ, feedback,
  then previous and next module.
  - The left menu becomes chips below 900px. The right rail holds the contents list, the voice
    player and quick actions, and is hidden below 1180px.
- `/Help/{module}/{section}` scrolls to that screen.

- [ ] **Step 1: Build the components and the page.** Delete `/Help/_banner-preview`.
- [ ] **Step 2: Check in the browser** as the local RMDO test user, on publicity. Until Task 15
  lands it uses a v2 copy of publicity kept in the harness test data; if that is not practical,
  do this check after Task 15. Check:
  - at 1440px:
    - the swimlane has connectors;
    - tapping a stage changes the detail panel;
    - the TOC follows scrolling;
    - a section is marked read after 5 seconds, and the tick shows in the left menu;
  - at 375px: a vertical timeline, a chip menu, and no sideways scroll;
  - the phone picture falls back to the desktop one when missing, with the markers still on
    target;
  - feedback Yes sends a POST that the local API stores.
- [ ] **Step 3: Run the harness.** Still 9 failures.
- [ ] **Step 4: Commit**
  `feat(help): module guide page with flow, statuses, roles, screens, FAQ and feedback`

### Task 11: Help centre home

**Files:**
- Replace: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/Pages/Help/HelpDesk.razor`

**Interfaces:**
- Consumes catalog v2 and `HelpProgressState`.
- Sections follow `index.html`:
  - hero: search box, which feeds the existing search results list, and the language picker
    (`HelpLanguagePicker`). No microphone; it is a non-goal.
  - stats strip, built from:
    - the number of visible modules;
    - the number of visible screens;
    - the number of languages;
    - whether voice is enabled (`HelpApi` audio status);
  - "Your path" from `PathFor`. It is hidden when null, and shows "Start with a module" instead.
  - module cards: colour, read count from progress, and "NEW" when `Updated` is within 14 days;
  - tour cards: up to 3 path steps with `Tour`, then other screens with a tour that are not
    record routes;
  - popular questions from `PopularFaq`;
  - What's new;
  - the support box, only when `Library.Support` has a phone or WhatsApp number.
- No assistant button; it is a non-goal.

- [ ] **Step 1: Build.**
- [ ] **Step 2: Check in the browser at 1440px and 375px** as RMDO and as a lab user:
  - each sees only their modules;
  - the path shows for RMDO;
  - the support box is hidden while `help.config.json` has empty numbers.
- [ ] **Step 3: Commit**
  `feat(help): help centre home page`

### Task 12: Guided tour

**Files:**
- Create in `Components/Help/`: `HelpTourHost.razor`, rendered at the layout root next to
  `HelpPanelHost` in `Layout/MainLayout.razor`.
- Create: `Services/HelpTourState.cs`, registered in both hosts.
- Modify: `Components/Help/HelpButton.razor` and `Components/Help/HelpPanel.razor`, to add the
  "Take the tour" entry when `ForPath(current)` has `Screen.Tour`.
- Add the tour functions to `wwwroot/js/help-centre.js`.

**Interfaces:**
- `HelpTourState`:
  - `Start(string module, string sectionId)`;
  - `Next()`, `Back()`, `End()`;
  - `Current`, which is `(HelpSectionView Section, int Index, IReadOnlyList<string> Steps)?`;
  - `event Action? Changed`.
- `helpCentre.tour`:
  - `show(selector) -> {found:bool}`: scrolls the target into view, places the highlight and
    positions `#hc-tour-card` with the same edge rules as `tour.html`;
  - `hide()`;
  - `bindKeys(dotnetRef)`: Esc calls `OnTourEnd`; ArrowRight and ArrowLeft call
    `OnTourNext` and `OnTourBack`.
- The host:
  - when `found` is false, skips to the next step and shows the line "This part isn't on your
    screen right now";
  - moves focus to the card, with `role="dialog"`, `aria-live="polite"` and a step counter;
  - the Listen button plays the step clip (Task 4);
  - the language switch inside the tour changes `HelpLanguageState`.
- The guide page "Tour" button navigates to the section's first route without parameters, then
  calls `Start`.

- [ ] **Step 1: Build.** Temporarily add a `data-help` attribute to two elements on
  `/PublicityRequest`, kept by Task 16.
- [ ] **Step 2: Check in the browser.**
  - Start the tour from **?** and from the guide page.
  - Next, Back and Esc work, and the arrow keys work.
  - A target removed with devtools gets the skip note, and nothing is highlighted.
  - At 375px the card fits with 12px margins.
  - In the MAUI layout the overlay sits above the top bar and tab bar. Check this in the web host
    at 375px with the phone layout; the MAUI build is checked in Task 19.
- [ ] **Step 3: Commit**
  `feat(help): guided tour on the real screen`

### Task 13: Print / Save as PDF (web only)

**Files:**
- Add `@media print` rules to `Help.css`: hide navigation, banner animation, rail, chips,
  feedback and tour; expand all FAQ answers; avoid page breaks inside screen blocks.
- Wire the banner `ShowPdf` to "web host only" with `IFormFactor.GetPlatform()`. `OnPdf` calls
  `helpCentre.print()`.

- [ ] **Step 1: Implement.** Check the browser's print preview of publicity. Expected:
  - only the visible sections are printed, in the current language;
  - pictures are included;
  - there is no sideways cut-off.
- [ ] **Step 2: Commit**
  `feat(help): print or save a guide as PDF on the web`

---

## Part 4 — Content and tools for the three existing guides

### Task 14: Module map

**Files:**
- Create: `docs/help-module-map.md`
- Modify: `tools/help-tools/coverage-ignore.txt`

- [ ] **Step 1: Write the map.**
  - Assign every `@page` route (the output of the route listing used for coverage) to exactly
    one module.
  - Group the modules as `Layout/NavMenu.razor` groups them, and give each its category, colour
    and icon.
  - The three existing modules keep their ids.
  - Login, register, verify, not-found, privacy and the help pages go to `coverage-ignore.txt`.
- [ ] **Step 2: Check.** `help-tools check` shows the ignored routes are gone from the uncovered
  list, and the map has every remaining route exactly once. Use a small PowerShell count:
  routes in the map compared with the uncovered list.
- [ ] **Step 3: Commit**
  `docs(help): module map for the remaining guides`
  Tell the user the map is ready for them to confirm. Stage 4 waits on it; this plan does not.

### Task 15: Three English guides in v2, plus role paths

**Files:**
- Modify: `SPIC.Core/HelpContent/{publicity,sas-sample-collection,lab}/guide.en.md`,
  `SPIC.Core/HelpContent/paths.json`

- [ ] **Step 1: Rewrite each guide.**
  - Front matter: `category`, `color`, `example` (demo text), `updated: 2026-11-16`, `note`.
  - Blocks: `overview` (lead plus 3 cards), `flow` (lanes and stages from today's overview
    section), `statuses` (every status the module's screens show), `roles`, and `faq` (the
    current "Common problems", 2–3 marked popular).
  - Keep the existing screen section ids, routes and permissions. Add `minutes`, and add
    `screenshot` and `hotspots` names, with values filled in Task 17.
  - Add `tour` only for main tasks, about 3–5 per module:
    - publicity: requests list, new request, notifications;
    - sample collection: collection list, new collection, consignment upload;
    - lab: consignment list, analysis tracking, test entry list.
- [ ] **Step 2: Write `paths.json`** for these roles:
  - RMDO: publicity and sample collection;
  - MDO and JMDO: sample collection;
  - Farmer: my samples and reports;
  - Lab coordinator and Lab analyst.
  - Use designation names exactly as they are in the local database.
- [ ] **Step 3: Run `help-tools check`.** Expected:
  - no `block` or `path` issues;
  - `missing-image` and `tour-target` issues only, which Tasks 16–17 fill;
  - translations listed as stale, which Task 18 fills.
- [ ] **Step 4: Run the harness.** Still 9 failures.
- [ ] **Step 5: Commit**
  `docs(help): Publicity, Sample Collection and Lab guides in format v2`

### Task 16: Tour markers on the real pages

**Files:**
- Modify the Razor pages behind the tour sections of Task 15, under `Pages/Publicity`,
  `Pages/Sas` and `Pages/Lab`. Add only `data-help="<id>"` attributes on the target elements;
  change no behaviour.

- [ ] **Step 1: Add the markers.** Run `help-tools check`. Expected: no `tour-target` issues.
- [ ] **Step 2: In the browser, run each tour once** as the matching local test user. Expected:
  every step highlights its element.
- [ ] **Step 3: Commit**
  `feat(help): tour markers on Publicity, Sample Collection and Lab screens`

### Task 17: Demo data and the `shoot` command

**Files:**
- Create: `tools/help-demo/users.json`, which maps module and section to a demo login, the URL
  to open (a demo record id for `{id}` routes) and an optional CSS selector to crop to.
- Create: `tools/help-demo/{publicity,sas-sample-collection,lab}.sql`, or a C# seeder in
  `tools/help-tools/DemoSeed.cs` if passwords must use the app's hasher.
  - Seeds are self-contained: demo users, designation grants, master rows and records in every
    status the screens show.
  - Seeds are idempotent: every insert checks first.
- Create: `tools/help-tools/Shooter.cs`
- Modify: `tools/help-tools/Program.cs`, `tools/help-tools/HelpTools.csproj`
  (`<PackageReference Include="Microsoft.Playwright" Version="1.49.0" />`)

**Interfaces:**
- `help-tools seed --module m`:
  - refuses unless the connection string has `Host=localhost` and `Database=spicone_dev`;
  - runs the module's seed.
- `help-tools shoot --module m [--phone-only|--desktop-only]`:
  - refuses unless the web base URL is `http://localhost:5027`;
  - launches the installed Edge (`Channel = "msedge"`, so no browser download);
  - signs in through the login form as the demo user;
  - opens each URL at 1280×800 for desktop, and at 390×844 with `IsMobile` and touch for phone;
  - waits for network idle;
  - saves the PNG for desktop, and the WebP for phone (PNG converted with ImageSharp, already
    referenced, or saved as PNG if not available — ledger a ruling).
  - The files go to `wwwroot/help/<module>/images/` with the names in the guide.
- Demo passwords are local test values, recorded in `users.json`. They exist only in
  `spicone_dev`.

- [ ] **Step 1: Write the seeds and run them for the three modules** against `spicone_dev`. In
  psql, check that the expected statuses exist.
- [ ] **Step 2: Implement `shoot`** and run it for the three modules, both sizes, with the local
  API on 5131 and `spicone-web-local` on 5027.
- [ ] **Step 3: Write the hotspots.** Open each image, write the `hotspots:` and
  `hotspots-phone:` values (%) for each step, and check them on the guide page: each marker sits
  on its element.
- [ ] **Step 4: Run `help-tools check`.** Expected: no `missing-image`, and no
  `missing-phone-image` for the three modules.
- [ ] **Step 5: Commit**
  `feat(help-tools): demo data, screenshot command and pictures for three guides`

### Task 18: Translations of the three v2 guides

- [ ] **Step 1: Translate each language.**
  - For each of ta, te, mr and hi, one subagent per language translates a copy of each English
    guide.
  - Text fields, lead, bullets, titles, `_category` and `_note` are translated. Layout lines,
    bold screen names, `Tip:` and `Careful:` stay English.
  - Use the same instructions as the 2026-11-16 v1 translation run: simple everyday words for
    field staff, dealers and farmers.
- [ ] **Step 2: Import each file** with
  `dotnet run --project tools/help-tools -- import <draft> --module m --lang xx`.
  Fix and re-import until every file imports with no problems.
- [ ] **Step 3: Run `help-tools check`.** Expected: no stale or missing translations for the
  three modules.
- [ ] **Step 4: Check in the browser.** Open publicity in Tamil and in Hindi at 375px:
  - the text is translated;
  - the layout is the same as English;
  - the "Machine translated" note shows.
- [ ] **Step 5: Commit**
  `docs(help): Tamil, Telugu, Marathi and Hindi for the v2 guides`

### Task 19: Docs, full check, PR

**Files:**
- Modify: `docs/help-desk.md` (format v2, blocks, screen keys, tours, `seed`, `shoot`, the
  feedback tab) and `tools/help-tools/README.md`.
- Revert: the `help-mockup` entry in `.claude/launch.json`, which must not be committed.

- [ ] **Step 1: Update the docs.**
- [ ] **Step 2: Run the full checks.**
  - The harness: still 9 failures.
  - `help-tools check`: only uncovered routes of modules without guides, plus warnings.
  - `dotnet build` of the MAUI head for Windows: it builds. Then open `/Help` in the Windows
    MAUI app if it runs locally.
- [ ] **Step 3: Check in the browser** as RMDO, lab analyst and farmer, at 1440px and 375px:
  - home;
  - module page;
  - a tour;
  - search;
  - feedback;
  - print preview.
- [ ] **Step 4: Commit.** Push `Satham`. Open the PR into `azure-deploy` with deploy notes:
  - V33 migration;
  - `POST /api/Help/audio/warm` after deploy;
  - support numbers still to fill in `help.config.json`.
