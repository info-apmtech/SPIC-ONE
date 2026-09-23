# Digital Library and Knowledge Community: implementation plan

Owner: product owner (deploys) and the coordinating agent (builds). Started 2026-09-20.
Goal: replace the sample-data prototypes of both modules with a real API, PostgreSQL storage,
file uploads and a working SPIC AI assistant, without touching the live database or the live
desktop experience until the product owner runs the deploy.

## 1. Principles

- **No live data.** Development and testing run against a throwaway PostgreSQL 17 container
  (`spicone-pg`, port 5544, database `spicone_dev`), never against the connection string in
  `SpicAPI/appsettings.json`. The API is started locally with
  `ConnectionStrings__DefaultConnection` pointing at the container.
- **Additive schema only.** One EF Core migration (`V5l_DigitalLibraryAndCommunity`) creates new
  tables and re-seeds the `ApplicationPages` catalogue with the new `DigitalLibrary` key, which was
  appended at the END of `PagePermission` so no existing seed id changes. No existing table or
  column is altered.
- **Production deploy is the product owner's.** Order: `deploy\azure\migrate.ps1 -Environment prod`
  (migration first), then `deploy\azure\deploy.ps1 -Environment prod -Quick` (API + web). The
  Android/iOS apps pick the API up on next launch; no store release is needed for the API.
- **Contracts first.** DTOs in `SPIC.Core/DTOs/LibraryDtos.cs` and `CommunityDtos.cs`, entities in
  `SPIC.Core/Entities/DigitalLibrary.cs` and `Community.cs`, are fixed by the coordinator before
  the agents start, so API and client work proceeds in parallel against the same shapes.
- **Same conventions as the rest of the API**: `[Authorize] [ApiController] [Route("api/[controller]")]`,
  `Task<IActionResult>`, `{ Success, Message }` envelopes for writes, DTO projection with
  `AsNoTracking()`, audit from `User.Identity?.Name`, files under `Uploads/<Module>/{id}/` served by
  a `file/{*path}` endpoint that is on the `access_token` allowlist in `Program.cs`.

## 2. Architecture

| Layer | Digital Library | Knowledge Community |
|---|---|---|
| Entities (`SPIC.Core/Entities`) | `LibraryContent` (one table, three kinds), `LibraryConversation`, `LibraryMessage` | `CommunityPost`, `CommunityPostReply`, `CommunityPostAttachment`, `CommunityReaction`, `CommunityProductMember` |
| DbContext | `LibraryContents`, `LibraryConversations`, `LibraryMessages` | `CommunityPosts`, `CommunityPostReplies`, `CommunityPostAttachments`, `CommunityReactions`, `CommunityProductMembers` |
| API | `LibraryController` (CRUD, status, uploads, file view, lookups, stats) + `LibraryAssistantController` (`api/Library/assistant/...`) | `CommunityController` (list/detail/create/update/delete, replies, reactions, attachments, similar, stats, products, lookups) |
| Assistant | `IAssistantProvider` with `KeywordAssistantProvider` (always available: retrieval over published content, answers composed from Overview/Usage/Features with sources) and `AnthropicAssistantProvider` (Claude Messages API, used when `Assistant:AnthropicApiKey` is configured; the retrieved content is the context, the reply cites sources) | `SimilarDiscussions`: title/body word overlap scored in SQL-friendly LINQ (shared words of 3+ letters, weighted by title matches), top 3 |
| Client services (`Shared/Services`) | `DigitalLibraryApi` (maps DTOs to the existing `DigitalLibraryItem` / `ContentDraft` view models) | `CommunityApi` (maps DTOs to the existing `CommunityDiscussion` / `CommunityReply` view models) |
| Pages | existing pages switch from `DigitalLibrarySampleData` to `DigitalLibraryApi`; uploads via `InputFile` | existing pages switch from `CommunitySampleData` to `CommunityApi` |
| Permissions | `PagePermission.DigitalLibrary` (new, appended); menu rules use `CanAccess("DigitalLibrary")`; reads of published content and the assistant are open to all signed-in users | `PagePermission.Community` (already existed); every signed-in user can read and post; menu rule uses `CanAccess("Community")` |

Author identity in the community comes from the JWT (`NameIdentifier`, `Name`, `Role`); display role
is "SPIC Expert" for every staff role, "Dealer" for dealers, "Farmer" for farmers. Location is the
user's HQ/state name when the user record has one, else empty.

## 3. Phases and agents

| Phase | Agent (Opus) | Owns | Depends on |
|---|---|---|---|
| 0 | coordinator | plan, DTOs, entities, DbContext registration, `PagePermission.DigitalLibrary`, `Program.cs` allowlist, local database | – |
| 1a | API-Community | `SpicAPI/Controllers/CommunityController.cs`, `Spic.Infrastructure/Migrations/*V5l*` (the single migration for BOTH modules), `Spic.Infrastructure/Services/CommunityService.cs` if wanted | phase 0 |
| 1b | API-Library | `SpicAPI/Controllers/LibraryController.cs`, `LibraryAssistantController.cs`, `Spic.Infrastructure/Services/Assistant/*`, `SPIC.Core/Interfaces/IAssistantProvider.cs`, `Program.cs` service registrations only, `appsettings.json` `Assistant` section | phase 0 |
| 1c | Client-Library | `Shared/Services/DigitalLibraryApi.cs`, all `Shared/Pages/DigitalLibrary*.razor(.css)`, `Shared/Services/DigitalLibraryItem.cs`, `ContentDraft.cs`, `ShellNavigation.cs` + `NavMenu.razor` rule for DigitalLibrary only | phase 0 (compiles against DTOs; runs end to end after 1b) |
| 1d | Client-Community | `Shared/Services/CommunityApi.cs`, `CommunityModels.cs`, all `Shared/Pages/Community*.razor(.css)`, `Shared/Components/Community/*`, `Shared/Pages/Farmers.razor` tile rule, `ShellNavigation.cs` + `NavMenu.razor` rule for Community only | phase 0 (runs end to end after 1a) |
| 2 | coordinator | migration applied to the local container, API run locally, end-to-end pass of every page on web (phone + desktop widths) and on the phone against the local API, fixes, docs, commit, push | 1a–1d |
| 3 | product owner | `migrate.ps1`, `deploy.ps1`, assign the DigitalLibrary/Community pages to designations, optional `Assistant__AnthropicApiKey` in Key Vault + bicep | phase 2 |

`ShellNavigation.cs` and `NavMenu.razor` are touched by two client agents in disjoint lines
(each edits only its own module's rule); the coordinator resolves any overlap at merge.

## 3a. Status (2026-09-20)

Phases 0, 1a-1d and 2 are done: API + client built, migration applied to the local database,
end-to-end pass on the web host (phone and desktop widths) with the coordinator's own run of the
community post flow (unique and similar paths, view counting) and the assistant (source filter,
stored provider). Phase 3 (production) is with the product owner.

Round 2 (same day): reply images, avatars, product images, resolve/reopen, reply delete, chat
attach and voice, and the open-access model (community for all, library content for all, content
addition by designation). Verified with one QA login per AppRole (`qa.<role>` in the local
database, designation "QA All Pages") through the ui-sweep harness at phone width on the web and
on the phone against the local API.

## 4. Acceptance

- `dotnet build SpicOne.sln` clean; migration applies to an empty database and to a copy of the
  current schema (idempotent re-run is a no-op).
- Library: create product/video/brochure through the wizard with cover, video and PDF uploads;
  publish/unpublish; related content resolves; detail page views count; assistant answers with
  sources from published content (keyword provider) and, when a key is present, via Claude.
- Community: post a discussion (similar check, unique path), attachments, reply and nested reply,
  like/save/follow toggles, status transitions, filters/tabs/paging from the URL, stats.
- Non-admin without the page in the designation: menus hidden, guard redirects; reads of the
  community remain open to every signed-in user (product decision: it is a shared forum).
- No live data created at any point; the local container is disposable.

## 5. Deploy runbook (product owner)

```powershell
# 1. migration (opens the DB firewall for your IP, closes it afterwards)
.\deploy\azure\migrate.ps1 -Environment prod
# 2. API + web
.\deploy\azure\deploy.ps1 -Environment prod -Quick
# 3. optional assistant key (Key Vault secret + container app env var Assistant__AnthropicApiKey)
```

Then in Designation, grant `Digital Library` and `Community` to the designations that should see
them; admins see them without assignment.

## 6. Open items

- Anthropic API key and model choice (`Assistant:Model`, default `claude-sonnet-5`); until set,
  the keyword provider answers.
- Farmer accounts (`AppRole.Farmer`) exist in the enum but there is no farmer sign-up yet
  (Farmer Portal / SAS next version), so the first community members are staff and dealers.
- Push notifications for replies (store release) and e-mail digests are out of scope here.
- The assistant key is never in `appsettings.json` (the `Assistant` section ships with an empty
  `AnthropicApiKey`). On Azure it is a Key Vault secret surfaced to the container app as the
  environment variable `Assistant__AnthropicApiKey`; `Assistant__Model` / `Assistant__MaxTokens`
  override the defaults the same way. With no key the offline keyword provider answers, and the
  Anthropic provider also falls back to it on any API error, so the assistant never fails closed.
