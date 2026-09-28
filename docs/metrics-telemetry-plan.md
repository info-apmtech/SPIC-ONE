# Metrics & error telemetry (2026-09-28)

Product ask: "a metrics page: how many users are using, and any error must be recorded" and
it must be **automatic and independent of pages** - a page added next month is covered without
anyone remembering to add anything to it.

## 1. Design in one paragraph

Every capture point is *global*: an ASP.NET middleware on the API (one row per request, one row
per unhandled exception), an `ILoggerProvider` on all three hosts (anything any code logs at
Error or above becomes an error row), the one `ErrorBoundary` in the layouts (page render and
event-handler errors), a `NavigationManager.LocationChanged` subscription started once from
`Routes.razor` (page views), and a `window.onerror` / `unhandledrejection` hook in
`app-interop.js` (JavaScript errors). No page, controller or service is edited and none ever
needs to be. Rows land in five PostgreSQL tables through a bounded in-memory channel and a
background writer, so telemetry never slows a request; an hourly rollup keeps the `/Metrics` page
fast and a nightly purge keeps the tables small. Application Insights is wired in as well
(both server hosts), inert until `APPLICATIONINSIGHTS_CONNECTION_STRING` is set by the bicep
template, for the engineers' deep view (dependency timings, live metrics, 90-day traces).

## 2. Capture points (all automatic)

| # | Where | What | Identity |
|---|-------|------|----------|
| 1 | API `RequestTelemetryMiddleware` (after UseAuthorization) | one `AppRequestLogs` row per request: method, route **template** (`api/Lab/batches/{id}`, from endpoint metadata; falls back to the path with numeric / guid segments replaced by `{id}`), status, duration, user, role, app, app version, IP, trace id. Skips `/health`, `/swagger*`, `/`, OPTIONS. Never stores query strings or bodies. | JWT claims |
| 2 | API `ExceptionTelemetryMiddleware` (first in pipeline) | unhandled exception -> `AppErrorLogs` row (Source Api) + `500 { Success:false, Message, TraceId }` in the project's JSON convention; also logs through ILogger so the container console keeps it | JWT claims |
| 3 | API `TelemetryLoggerProvider` | any `LogError` / `LogCritical` from controllers and services (most controllers catch and log) -> `AppErrorLogs` (Source Api), with the current request's route / user from `IHttpContextAccessor`. Deduped per fingerprint for 60 s. | JWT claims |
| 4 | Web host `TelemetryLoggerProvider` (same class, Shared) | circuit crashes, SignalR / renderer errors, anything the host logs at Error+ -> `POST api/Telemetry/batch` with `X-Telemetry-Key` (Source WebHost). Circuit id -> user / route enrichment through `TelemetryCircuitRegistry` (best effort). | registry |
| 5 | MAUI host `TelemetryLoggerProvider` | same, Source Client, posted with the signed-in user's token when there is one | token |
| 6 | `TelemetryErrorBoundary` (subclass of `ErrorBoundary`) in `MainLayout` and `EmptyLayout` | page render / event handler exceptions -> `ClientTelemetry.ReportAsync` (Source Client) with user, route, app. Does **not** call the base logger (would duplicate through #4/#5). The existing "Try again / Go to dashboard" UI stays. | LoginState |
| 7 | `ClientTelemetry` (scoped, started from `Routes.razor`) | `LocationChanged` -> `AppPageViews` row (route normalised: `/Lab/Tracking/{id}`); batched, flushed every 20 s / 25 items / on error / on dispose; only while signed in | LoginState |
| 8 | `window.spic.telemetry` in `app-interop.js` | `error` + `unhandledrejection` -> queued until `attach(dotnetRef)` from `ClientTelemetry`, then `ReportJsError` (Source Js) | LoginState |
| 9 | `AuthHttpMessageHandler` | adds `X-Spic-Client: web|android|ios|windows|maccatalyst` and `X-Spic-Version` to every API call from a `ClientInfo` singleton, so #1 knows which app made the request | - |

"Users using the app" = distinct `UserId` with a request (#1) or page view (#7) in the period,
split by app and role. Total registered users comes from `AspNetUsers` (active).

## 3. Storage (`Spic.Infrastructure`, migration `V5t_Telemetry`)

| Table | Row | Retention (config `Telemetry:Retention`) |
|-------|-----|------------|
| `AppRequestLogs` | one API request | 30 days |
| `AppPageViews` | one client navigation | 30 days |
| `AppErrorLogs` | one error occurrence, `Fingerprint` = SHA-256 of source + exception type + normalised message + top frame; `IsResolved` per fingerprint | 90 days |
| `AppUsageDaily` | day x app x role (`""` = all): active users, requests, page views, errors, avg / p95 ms | 400 days |
| `AppRouteDaily` | day x app x route (client routes **and** API templates, `Kind` tells them apart): views / requests, users, errors, avg / p95 ms | 400 days |

Writer: `TelemetryChannel` (bounded 10 000, drop-oldest with a dropped counter) -> `TelemetryWriter`
`BackgroundService` (batches of 200 or 2 s, own DbContext scope, failures logged and dropped).
Rollup: `TelemetryRollupService` every 15 min recomputes today and yesterday (delete + insert those
days), purges at 03:00 server time, first run 1 min after start. Today's numbers on the page come
from the raw tables (live), older days from the rollups.

The `Metrics` page key is appended to `PagePermission` and seeded with the same idempotent
`INSERT ... WHERE NOT EXISTS` SQL as V5s (EF will scaffold `InsertData` for id 105; replace it).

## 4. API (`SPIC.Core/DTOs/MetricsDtos.cs` is the contract; route list in its header)

Access to `api/Metrics/*`: page key `Metrics` (designation) **or** role SuperAdmin / Admin /
CorporateAdmin - the normal `CanAccess` rule, not the designation-only rule of the lab.
`api/Telemetry/batch` accepts a bearer token (user-attributed), `X-Telemetry-Key` (server-
attributed, Source WebHost) or nothing (anonymous: errors only, at most 5 per batch, 2 000-char
messages, 60 batches / minute / IP).

## 5. Client

* `Services/Telemetry/ClientInfo.cs`, `ClientTelemetry.cs`, `TelemetryCircuitRegistry.cs`,
  `TelemetryLoggerProvider.cs`, `TelemetryErrorBoundary.cs` (Shared); `MetricsApi.cs` wrapper.
* `Pages/Admin/Metrics.razor` + `.razor.css` + `Components/Metrics/*`: tabs Overview (KPIs,
  30-day line chart of active users / errors, by-app and by-role tiles), Users (last seen, app,
  role, counts, search), Pages (top routes), API (slowest / most-failing endpoints), Errors (grouped
  by fingerprint, filters, detail sheet with stack trace and Resolve). Days filter 7 / 30 / 90,
  app filter. Chart through the vendored Chart.js (`wwwroot/js/metrics-chart.js`).
* Menu: `NavMenu.razor` (desktop) under the admin Settings group, `ShellNavigation` (phone /
  tablet) in `GroupAdminTools`; route `/Metrics` is covered by PageGuard's generic first-segment
  check (`CanAccess("Metrics")`).

## 6. Azure

`infra/azure/main.bicep`: `Microsoft.Insights/components` (workspace-based on the existing Log
Analytics workspace) and `APPLICATIONINSIGHTS_CONNECTION_STRING` on the api and web container
apps; `Telemetry__IngestKey` (generated like `jwtKey`, Key Vault secret `telemetry-ingest-key`)
on both. `deploy.ps1 -Quick` does not run bicep: the product owner applies the infra with
`provision.ps1` when convenient; until then both stay unset and everything else works.

## 7. Build plan

Phase 0 (coordinator, done): this document, entities, DTO contract, enum key.
Phase 1, in parallel:
* **API agent**: DbContext + migration, options, channel, writer, rollup, middlewares, logger
  provider, `TelemetryController`, `MetricsController`, Program.cs wiring, App Insights package,
  bicep, `tools/metrics-api-check/metrics_api_check.py`.
* **Client agent**: everything in section 5 plus host registrations (Web `Program.cs`, MAUI
  `MauiProgram.cs`), `AuthHttpMessageHandler` headers, `app-interop.js` hook, `Routes.razor`,
  both layouts.
Phase 2 (coordinator): build all, run the check script and the UI sweep, phone install, docs.

## 8. Status log

- 2026-09-28: phase 0 written.
