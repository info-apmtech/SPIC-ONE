# SPIC ONE: module and feature status

Source: module sheet shared by the product owner on 2026-09-19 (Google Sheet "modules list").
"Documentation" = functional documentation; "Design" = Figma design
(https://www.figma.com/design/HkdquY0LylyLn9Yo7m3xkR/SPIC). Blank means not started.

Implementation priority stated on 2026-09-19: **SAS Portal** and **Digital Library**
(Chatbot, AI Videos, Product Information / product browser, Product Brochure); design all
their pages first from Figma, then implement functionality one by one. The Figma file is too
heavy for browser automation; page designs come as screenshots from the product owner.

| Module | Feature | Documentation | Design |
|---|---|---|---|
| Subsidy Management System | Pending Acknowledgement | Completed | Completed |
| Subsidy Management System | Stock Aging | Completed | Completed |
| Subsidy Management System | Notifications | Not started | |
| Subsidy Management System | Customized Reports | In progress | In progress |
| SDWA | Room Bookings | Completed | Completed |
| SDWA | Welfare Scheme | Completed | Completed |
| SDWA | Dealer Registration | Completed | Completed |
| SDWA | Reports | Completed | Completed |
| Digital Library | Chatbot | Completed | Completed |
| Digital Library | AI Videos | Completed | Completed |
| Digital Library | Product Information | Completed | Completed |
| Digital Library | Product Brochure | Completed | Completed |
| SAS | Soil Sample Collection and Analysis | Completed | Completed |
| SAS | Budgeting Activities | In progress | |
| SAS | MSTL Van Tracking | In progress | |
| SAS | Field Programs Tracking | In progress | |
| SAS | Farm Activities Tracking | Completed | In progress |
| SAS | SPC Renewal Reminders | In progress | |
| SAS | Reports | In progress | |
| SAS | SAS Report in 6 Languages | In progress | |
| MD Portal | Budgeting, CSR-1 and CSR-2 | Completed | Completed |
| MD Portal | MD Report | Completed | Completed |
| MD Portal | Sale Audit | Completed | Completed |
| MD Portal | Demo Documentation | Completed | Completed |
| Publicity | Indent creation, Approval, Dispatch, Tracking, Acknowledgement | In progress | |
| Dealer Portal | Dealer Enrolment | Completed | Completed |
| Dealer Portal | Dealer Outstanding Notification | | |
| Dealer Portal | Reports: SOA / Collections Since | | |
| Dealer Portal | Order Placement | | |
| Compliance | Dealer License | | |
| Compliance | State License | | |
| Compliance | AV Van License | | |
| Specialty Products | Indent Creation | Completed | Completed |
| Specialty Products | Trade Terms | Completed | Completed |
| Farmer Portal | Farmer Chatbot | | |
| Farmer Portal | Farmer Community Connect | | Completed |
| Farmer Portal | Predictive Analysis based on AI | | |
| Service Portal | Farmer Scheme | Completed | Completed |
| Service Portal | Drone Seva | Completed | Completed |
| Service Portal | Dealer Scheme | Completed | Completed |
| Service Portal | MO / MDO incentive | | |
| Service Portal | AV Van Tracking | | |
| Service Portal | Evaluation: JMDO / MO / MDO | | |
| Dashboard | SDWA, SAS, MD and CS, Sales Portal, Subsidy, Dealer Portal, Farmer Portal, Customized Main Dashboard | | |
| Sales Portal | Indent creation | Completed | Completed |
| Sales Portal | SO Generation and Approval | Completed | Completed |
| Sales Portal | STO Creation and Approval | Completed | Completed |
| Sales Portal | Automated POs for Third Party | Completed | Completed |
| Sales Portal | SAP Integration | In progress | In progress |
| Sales Portal | Customized Reports | Completed | Completed |
| Imports | Customized reports, Automated reminders | | |
| Industrial Products | Customized reports, Automated reminders | | |

## Mapping to the existing app

Already implemented in `SPIC.MauiBlazorApp.Shared/Pages`: Subsidy (Pending Acknowledgement,
Stock Aging, Liquidation, Company Sales, Ageing), SDWA (dealer registration, welfare schemes,
guest house bookings, lucky draw, gallery), MD Portal (budget, CSR-1, CSR-2, final report),
Dealer enrolment and credit limits, logistics, settings.

Not yet in the app: Digital Library (4 features), SAS (8 features), Specialty Products,
Service Portal, Sales Portal, Farmer Portal, Publicity, Compliance, Imports, Industrial
Products, the cross-module dashboards.

## Digital Library implementation log

- 2026-09-20: landing page `/DigitalLibrary` built from the Figma frame (hero with assistant
  search and suggestion chips, AI Videos / Product Information / Product Brochures cards,
  featured strip, content grid with publish/edit/delete/archive). Sample data only; the
  content API comes next. Sub-sections (`/DigitalLibrary/videos|products|brochures|chat|add`)
  are placeholder screens until their frames are implemented. Menu entry is admin-only until
  `PagePermission` gains a `DigitalLibrary` key (API + permission catalogue change).
- Figma automation: the file loads in the browser pane but frames render as low-resolution
  tiles, so designs are taken from full-size screenshots supplied by the product owner.

## Phone bottom menu (approved 2026-09-20)

Five positions: Home | three role destinations | More. "Ask SPIC AI" (`/DigitalLibrary/chat`)
and "Alerts" (`/Alerts`) sit in the phone/tablet top bar for every role (sparkle and bell) and
in the More sheet, so they take no bottom slot. Source of truth: `Services/ShellNavigation.cs`.

| Role | Home | Slot 2 | Slot 3 | Slot 4 |
|---|---|---|---|---|
| Dealer | Dealer Dashboard | Welfare Schemes | Guest House | My Bookings |
| MO / MDO / JMDO | Dashboard | My Activities (`/Activities`) | Dealers (Sub Dealer Master) | Farmers (`/Farmers`) |
| RM / RMD | Dashboard | Dealers | RMD Validation Queue | Reports Center |
| SMD / SMM | Dashboard | SMM Approvals | Dealers | Reports Center |
| Admin / CorporateAdmin | Dashboard | AVP Approvals | Reports Center | Digital Library |
| Director / AVP | Dashboard | AVP Approvals | Reports Center | next accessible page |
| SpecialAdmin | Logistics | Logistics Master | Logistics Report | User Profile |

Hub pages (`/Activities`, `/Farmers`, `/Alerts`) only link to pages the user can already open
(every tile is filtered by `LoginState.CanAccess`), so `PageGuard` opens them to every signed-in
user with a designation. My Activities holds the SAS field-work placeholder, the approval queues,
guest house booking and reports; Farmers holds the Farmer Portal placeholder plus Ask SPIC AI and
the library content; Alerts lists every approval queue and booking page until push notifications
arrive with the store release. Guest House / My Bookings stay in the More sheet for staff (few
employees book rooms); Dealers keep them on the bar. The phone top bar shows the app-icon logo
instead of the "SPIC" wordmark. The desktop sidebar (live) is unchanged.

## Knowledge Community (Community Connect) implementation log

- 2026-09-20: built from the product owner's Figma screenshots, sample data only
  (`Services/CommunityModels.cs`, `CommunitySampleData`): landing `/Community` (hero with
  question search, stats strip, Recent Discussions with grid/list toggle, Popular Product,
  New Here panel), `/Community/discussions` (stat tiles, tabs, filters in the URL, paged list),
  `/Community/discussion/{id}` (question card with attachments beside the text on desktop and
  below it on phone/tablet, nested replies, composer), `/Community/new` (form, similarity check:
  "Your question looks unique" or "We found similar discussions", Post Anyway), and
  `/Community/posted/{id}` (success summary, View Discussion / Back to Community). Admin-only in
  the sidebar and More sheet until `PagePermission` gains a `Community` key; also linked from the
  Farmers hub. Discussion API, uploads and the similarity service come next.
- 2026-09-20 (evening): both modules are API-backed. `CommunityController` (discussions, replies,
  like/save/follow, attachments, similar search with stop-words removed, stats, products, lookups)
  and `LibraryController` + `LibraryAssistantController` (content CRUD, status, cover/video/PDF
  uploads with range requests, lookups, stats, conversations, ask) on PostgreSQL via migration
  `V5l_DigitalLibraryAndCommunity`; client services `CommunityApi` / `DigitalLibraryApi`; sample
  data removed. SPIC AI answers from published content (keyword provider) and switches to Claude
  when `Assistant__AnthropicApiKey` is set. Menu rules now use `CanAccess("Community")` /
  `CanAccess("DigitalLibrary")`. Verified end to end against a local database (see
  docs/community-library-implementation-plan.md); production needs `migrate.ps1` then `deploy.ps1`.
- 2026-09-20 (night): access model per the product owner: the Community is usable by every
  signed-in user and Digital Library content is viewable by everyone; only adding/editing content
  needs the DigitalLibrary page in the user's designation (menus, PageGuard, page controls and the
  API all follow it; reader stats count published items only). Community additions: Mark as
  Resolved / Reopen (author or admin), reply delete, reply images (up to 3) and pasted links,
  profile photos (`POST/DELETE api/Community/me/avatar`), product images on Popular Product cards
  (editors), migration `V5m_CommunityReplyAttachments`. Chat additions: Attach picks a library item
  as context for the question; Voice uses the browser's speech recognition (en-IN) with a fallback
  toast where unsupported. Android Debug builds may use plain HTTP to localhost only (network
  security config) so the phone can be tested against a local API through `adb reverse`.
