# Help Centre redesign — visual, flow-based, screen-by-screen guides

Date: 2026-11-16
Status: Draft — reviewed, awaiting approval
Builds on: `2026-11-16-help-desk-design.md` (Help Desk v1, PR info-apmtech/SPIC-ONE#2)
Mockups (open in a browser): `docs/superpowers/specs/help-centre-mockups/` — `index.html` (home),
`module.html` (module guide), `tour.html` (guided tour)

## 1. Intent and why

Help Desk v1 works (role filtering, 5 languages, voice, review), but each guide renders as plain
headings and lists. The customer said it looks too simple for a professional product. People who
do not read long text still do not understand a screen.

The redesign keeps everything v1 built underneath and changes how a guide **looks and teaches**:

- the request journey drawn as a flow, with coloured status tags and who acts at each step;
- a real screenshot of each screen with numbered markers that match the numbered steps;
- a guided tour that highlights parts of the real screen, step by step;
- a help centre home page that starts each person on the path for their role.

It is the top of every page a customer sees for help, so it must look polished and move a little,
on desktop and on phone.

## 2. Goals

- **Help Centre home** (`/Help`), as in `index.html`:
  - animated hero, search, language switch;
  - number strip (modules, screens, languages, voice);
  - "Your path as <role>" with progress;
  - module cards with colour, screen count and read progress;
  - guided-tour cards;
  - popular questions;
  - "What's new";
  - "Still stuck?" support box.
- **Module guide page** (`/Help/{module}`), as in `module.html`:
  - animated banner: icon with ripples, looping example journey through the module's stages,
    progress ring and bar, tour, listen and PDF buttons. The PDF button is "Print / Save as PDF",
    using a print stylesheet and the browser's print. It covers only the sections the user can
    see, in the current language, with screenshots. It appears on the web only and is hidden in
    MAUI. (My call. A server-made PDF would need Indic font embedding and a MAUI share path, and
    can come later.)
  - left menu: guide parts, plus the user's screens with read ticks;
  - right column: "On this page" contents that follow scrolling, voice player, quick actions;
  - one scrolling page, no hidden tabs, in this order:
    1. Overview cards.
    2. **Flow**: a swimlane on desktop, with one row per role and the user's row marked "You". On a
       phone it becomes a vertical timeline. Tapping a step explains it.
    3. Every status, with its colour.
    4. Who does what, with the user's own role highlighted.
    5. **Screen by screen**: screenshot and numbered markers, linked steps, tip and careful callouts,
       fields table, plus Tour, Listen and Open screen buttons.
    6. Problems and answers.
  - feedback ("Was this guide helpful?") and links to the previous and next modules.
- **Guided tour** on the real app screen, as in `tour.html`:
  - the screen is dimmed and a highlight moves to each part;
  - a card shows step N of M, with Listen, Back and Next;
  - the language can be changed during the tour, and the tour can be ended at any time.
  - The **?** button on any page offers both "Take the tour" and "Read the guide".
- **Phone first**: all three work at 375px wide, with no sideways scrolling, in the web host and
  the MAUI app (same Razor library).
- **Everything translatable** in all 5 languages (English, Tamil, Telugu, Marathi, Hindi), and
  everything readable aloud.
- **All 24 modules** get guides in this format, in batches, after the format and pages are built.

## 3. Non-goals (this design)

- **The AI help assistant** (chat button in `index.html`). It has a running cost per question and
  needs answer limits. It is a separate, later decision.
- **The voice search microphone.** Search stays typed for now.
- **Recorded video.** "Video tours" are the guided tours on the real screen.
- **Live data in guides.** The "live example" banner and the screenshots use demo data only.
- **Syncing reading progress across devices** (see §6.6).

## 4. Anti-goals

- **Guides drifting from the app.** The v1 rule still holds: the person who changes a screen
  updates its guide in the same change. The coverage check gains checks for screenshots and tour
  targets (§6.9).
- **Translators breaking the visuals.** Layout data — lanes, colours, icons, marker positions and
  tour targets — always comes from the English file, exactly as `routes:` does today. A
  translation can change words only.
- **Showing a user screens their role cannot open.** The flow can show other roles' steps, because
  knowing who acts next helps. But other roles' screen guides, tours and "Open screen" links stay
  hidden, as in v1.
- **Motion that gets in the way.** All animation stops under `prefers-reduced-motion`. Nothing
  flashes, and no animation blocks reading or tapping.

## 5. Approaches considered

1. **Extend the v1 guide files with structured blocks and build new Razor pages (chosen).**
   - Flow, roles, statuses, screenshots, hotspots, tours and FAQ become small blocks in the same
     `guide.<lang>.md` files.
   - The parser, translation import, review, voice and coverage tools all keep working, and only
     learn the new blocks.
   - Developers keep owning guides, as the user chose for v1.
2. **Admin-editable help content in the database (CMS).** This was rejected in v1, because guides
   must change with the code and in the same pull request. Nothing has changed that.
3. **A third-party help tool** (a hosted knowledge base plus a tour library). Rejected:
   - none of them gives role filtering against our permission model;
   - none handles our 5 languages, our review flow and our cached voice;
   - none works inside the offline MAUI app;
   - they add a monthly cost.

Picked by: me (Claude). The user chose the look through three rounds of mockups and asked for
mobile fixes before going ahead.

## 6. Design

### 6.1 Guide file format v2

v1 files stay valid. v2 adds module front-matter keys and new block types.

**Front matter (module level), new keys:**

```
summary:  one line under the title (banner)
category: Marketing                      # banner eyebrow, home grouping
color:    #0b6b3a                        # module accent (cards, banner)
example:  IND-1042 · Banner 6x3          # label on the banner's example journey (demo text)
updated:  2026-11-14                     # shown on banner and in What's new
note:     Transfer of extra stock added  # What's new line for this update
```

**Blocks.** Every block is translatable text plus layout keys. Layout keys are copied from
English, the way `routes:` is today.

```
::: overview
- icon: bi-pencil-square | color: #0b6b3a
  title: You start it
  text: Raise a request with the items, sizes and quantities.
:::

::: flow
lanes: rmdo=RMDO (Your HQ), approver=Approvers (RM · SMD · SMM), ho=Head office (ADMIN-MD CS), vendor=Vendor
stage raise    | lane: rmdo     | col: 1 | icon: bi-pencil-square | color: #64748b | status: Draft | screen: new-request
  title: Raise and submit
  text: Fill the request form ... press **Submit Request**.
stage approve  | lane: approver | col: 2 | ... | next: final | return: raise
stage exception| lane: ho       | col: 6 | ... | kind: exception | from: ack | to: ship
:::

::: statuses
- Draft | color: #64748b : Saved, not sent. Only you see it.
:::

::: roles
- RMDO | roles: … | designations: RMDO | color: #0b6b3a
  - Raise requests
  - Acknowledge delivery
:::

::: faq
- q: I submitted, and now there is no pencil icon | popular: true
  a: After you submit, only an approver can return the request to you ...
:::
```

**Screen sections** (the v1 `::: section`) gain:

```
screenshot: requests-list.png               # wwwroot/help/<module>/images/
hotspots:   1 @ 2,4,96,18 | 2 @ 2,26,96,9 | 3 @ 80,40,18,30 | 4 @ 62,88,36,9   # n @ x,y,w,h in % of the image
tour:       1 > [data-help=pub-status-cards] | 2 > [data-help=pub-search] | ...
minutes:    1
```

- In the body, the first numbered list is the step list. Step N goes with hotspot N and tour
  step N.
- `> Tip:` and `> Careful:` lines become callouts.
- A Markdown table (`| Field | What to enter |`) becomes the fields table. The renderer learns
  simple tables.

Rules the parser enforces, reported as `Problems` the way v1 reports them:

- every flow `next`, `return`, `from`, `to` and `screen` points to something that exists;
- the hotspot count equals the number of steps;
- tour steps equal the number of steps, or are omitted;
- lanes and roles are unique.

### 6.2 Pages and components (shared Razor library)

| Piece | Where | Notes |
|---|---|---|
| Help home | `Pages/Help/HelpDesk.razor` (replaced) | Sections in the order of §2. Module cards only for visible modules. |
| Module guide | `Pages/Help/HelpGuide.razor` (replaced) | Layout of `module.html`. `/Help/{module}/{section}` scrolls to that screen. |
| Banner | `Components/Help/HelpBanner.razor` | The example journey is drawn from the module's flow stages: stages in column order, the `example:` label, the module colour. |
| Flow | `Components/Help/HelpFlow.razor` | Swimlane on screens wider than 900px, vertical timeline below that. Connector lines drawn by a small JS helper from element positions. |
| Screen guide | `Components/Help/HelpScreen.razor` | Image plus hotspot layer in % so it scales; ring follows the active step. |
| Tour | `Components/Help/HelpTour.razor` + `wwwroot/js/help-tour.js` | Overlay on the current page. JS finds `[data-help=…]`, scrolls it into view and positions the highlight and card. |
| "?" button | `Components/Help/HelpButton.razor` (extended) | Shows "Take the tour" when the current screen's section has a `tour:`. |

All styles live in one stylesheet, `wwwroot/css/help.css`, using the brand tokens already in
`app.css`: `--primary` #0b6b3a, `--accent` #2bb673, and Poppins as already loaded.

### 6.3 "Your path" per role

- `SPIC.Core/HelpContent/paths.json` maps a role or designation to an ordered list of
  `module/section` steps, each with minutes and a tour flag. It is translatable through the same
  import route as the guides.
- A user with no path entry sees "Start with a module" instead.

### 6.4 What's new

- The list is built from `updated:` and `note:` in each module's front matter, newest first, and
  shows only modules the user can see.
- No separate file, so it stays honest.

### 6.5 Search

- The existing client-side search is extended to step text and FAQ, across the guides the user
  can see.
- Popular questions on the home page are FAQ items marked `popular: true` in the modules the user
  can see.

### 6.6 Reading progress (my call)

- A screen counts as "read" when its section has been in view for 5 seconds, or when its tour
  finishes.
- Progress is stored per user on the device (local storage key `help.read.<userId>`), with no
  server change.
- Cost if wrong: progress doesn't follow a user to another phone. It can move to the server later
  without changing the screens.

### 6.7 Feedback

- "Was this guide helpful?" sends module, section, language, yes/no and an optional comment to a
  new endpoint, which stores them in a new table `HelpFeedback` (migration V33).
- Admins with Help translation review rights see the counts in `/Help/Review`, on a new
  "Feedback" tab.
- Kept (user's decision). The migration is dry-run locally before prod.

### 6.7a Support box

- "Still stuck?" reads `support` from `help.config.json`: `{ "phone": "", "whatsapp": "",
  "hours": { "en": "Mon–Sat, 9 to 6", ... } }`.
- The box hides itself while the numbers are empty.
- The user will supply the numbers. Until then the box stays hidden, and adding the numbers needs
  no code change.

### 6.8 Screenshots and demo data

- A new `help-tools shoot --module m` command uses Playwright for .NET.
- It signs in to the **local** web app (`spicone-web-local` against the local API and the local
  `spicone_dev` database) as each demo user in `tools/help-demo/users.json`.
- It opens each section's route at 1280×800 and saves the screenshot named in the guide. For tall
  screens it crops to the main panel.
- Demo data for each module comes from SQL seed scripts in `tools/help-demo/<module>.sql`. They
  are run only against `spicone_dev`, and the tool refuses any other connection string.
- **Seeds are self-contained (my call).** They assume a freshly migrated `spicone_dev` plus the
  app's own startup seeding, and nothing else. Each script creates its own demo users,
  designation grants and master rows, and can be re-run safely.
- Demo users' passwords are local-only test values kept in `tools/help-demo/users.json`. They
  exist only in the local database.
- Cost: more work per module, but any developer can re-run `shoot` and get the same pictures.
- Hotspot positions are written by hand in % (the mockup shows the shape). `check` warns when an
  image is missing or older than the page it shows.
- Images stay English. One image serves all languages, because the markers and steps carry the
  language.
- **Phone and desktop pictures (user's decision).**
  - Each screen gets a desktop picture (1280×800) and a phone picture (390×844, phone
    emulation), described in the guide as:

    ```
    screenshot:       requests-list.png
    hotspots:         1 @ ... | 2 @ ...
    screenshot-phone: requests-list.phone.png
    hotspots-phone:   1 @ ... | 2 @ ...
    ```

  - `HelpScreen` shows the phone picture below 900px wide and in the MAUI app, and the desktop
    picture otherwise. If one is missing it falls back to the other.
  - `check` reports a missing phone picture as a warning, not an error.
  - Phone pictures are compressed WebP, to keep the app small.

### 6.9 Tours and `data-help` markers

- A tour step targets `[data-help="<id>"]` on the real page. Developers add these attributes to
  the elements a tour points at. That is the only change inside module pages.
- `help-tools check` scans the Razor pages and reports any tour target that no page contains.
- If a target is missing at runtime (hidden by role, or not loaded), the tour skips that step and
  says so in the card. It never highlights empty space.
- **Screens that need a record** (`{id}` routes such as acknowledge, review or dispatch) handle
  tours as follows (my call):
  - their tour is offered only from the **?** button while the user is already on such a record;
  - on the guide page these sections show the screenshot and steps, with the note "Open a
    request to take this tour";
  - "Open screen" goes to the module's list page.
  - Home tour cards and "Your path" tour flags use list or entry screens only.
  - Cost: fewer one-tap tours. Nothing ever opens a real record on its own.

### 6.10 Motion and accessibility

- Animations:
  - banner: background drift, megaphone ripple, title shine, entrance slide-up, example journey
    loop;
  - progress ring and bar fill;
  - hover lifts.
- All of them stop under `prefers-reduced-motion`. The example journey then shows its final state.
- Tour: keyboard support (Esc ends the tour, arrow keys move between steps), focus moves to the
  card, and the card is announced to screen readers.
- Colour is never the only signal: status tags always have text, and flow steps always have
  numbers.

### 6.11 Phone and MAUI

- Breakpoints:
  - at 1180px the right column is dropped;
  - at 900px the layout becomes one column, the left menu becomes a sticky row of chips, and the
    swimlane becomes a vertical timeline;
  - at 520px the banner is compacted, the floating cards are hidden, and the buttons go full
    width.
- These were tested in the mockups at 375px.
- In MAUI the tour overlay sits above the app's top bar and tab bar. The "?" button stays where
  v1 put it.

### 6.12 Translations, review and voice

- The new translatable text (flow stage titles and text, statuses, roles, FAQ, overview, paths)
  goes through the same `import` checks.
- The text is hashed per block, so a change marks only that block stale.
- `/Help/Review` shows the new blocks as reviewable items.
- **Listen** reads in this order: the overview, the flow stages in order, then each screen's steps.
  Voice clips stay cached per text hash, as in v1.
- Tour steps get their own short clips.

### 6.13 Delivery order

1. **Format and pages.** Parser v2, the new pages and components, `help.css`, and the tour engine.
   The three existing guides (Publicity, Soil Sample Collection, Lab) move to v2, with flows,
   roles, statuses and FAQ.
2. **Screenshots and tours for those three.** Demo seed data, the `shoot` command, hotspots, and
   `data-help` markers on their pages.
3. **Translations** of the three in v2 (import route), plus review items.
4. **Remaining modules in batches** of about 4 modules: guide, demo data, screenshots, tours,
   translations. The order follows what customers use first: dealers, schemes and sales, then
   the rest.

**Branch and release (my call).**

- v1 (PR info-apmtech/SPIC-ONE#2) is merged into `azure-deploy` and can go to prod on its own,
  as already planned.
- v2 is built on the `Satham` branch (the user's branch, level with `azure-deploy` on 2026-11-16). Stages 1–3 ship together in one
  PR, so prod never shows v2 pages with English-only fallbacks.
- v2 restructures the three guides, so their section hashes change. Any Tamil, Telugu, Marathi or
  Hindi approvals given in prod on v1 text reset to "Not reviewed" when v2 ships.
- Stage 4 batches each go out as their own PR.

**Module map.** No list of modules exists in the repo today, so the plan's first content step
writes `docs/help-module-map.md`:

- every `@page` route is assigned to exactly one guide module, grouped as the side menu
  (`Layout/NavMenu.razor`) groups them;
- each module gets its category and colour;
- routes that need no guide (login, not-found, privacy) go to `tools/help-tools/coverage-ignore.txt`.

The user confirms the map before stage 4 starts. The "24 modules" in the mockups is a
placeholder until then.

## 7. Constraints

- .NET 10 Blazor shared library (web and MAUI), no new front-end framework. JS interop is limited
  to `help-tour.js` and the flow-connector helper.
- Guides are embedded in `SPIC.Core` (`WithCulture="false"`), as in v1.
- Local test data only. Never the appsettings database, never live data. The user runs prod
  deploys, after a what-if.
- Migrations are dry-run locally before prod.
- No API keys in the repository.

## 8. Left to the builder

- Exact component split, CSS class names, and how the JS draws connectors (SVG paths are fine).
- Icons: Bootstrap Icons, already used by the app.
- Parser details for the block grammar above, as long as every rule in §6.1 is checked and
  reported.
- The crop region for each screenshot.

## 9. Decisions log

| Decision | Who | Answer |
|---|---|---|
| Look of home, module page, tour, and the mobile layout | User | The mockups in `help-centre-mockups/` |
| Screenshots | User | Phone **and** desktop picture per screen, shown by device (§6.8) |
| Tours | User | Main tasks only: about 3–5 per module, for each role's main jobs. Other screens are guide-only. |
| "Was this guide helpful?" | User | Save it and show admins (§6.7) |
| Support box | User | Show phone, WhatsApp and hours. The user will supply the numbers, and the box stays hidden until then (§6.7a). |
| PDF | Claude | Print / Save as PDF, web only (§2) |
| Demo seeds | Claude | Self-contained, local `spicone_dev` only (§6.8) |
| Tours on record screens | Claude | Only from **?** while the user is on a record (§6.9) |
| Branch and release | User (branch), Claude (release order) | Work on `Satham`. Stages 1–3 ship together. v1 can go to prod first (§6.13). |
| Module map | Claude drafts, user confirms | `docs/help-module-map.md`, confirmed before stage 4 (§6.13) |
| AI help assistant and voice search | Deferred | Not in this design (§3) |
