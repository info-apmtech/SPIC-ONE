# Testing SPIC ONE: UI verification harness

This page describes the automated UI sweep in `tools/ui-sweep/` and how it fits the
per-page checklist in `docs/screen-checklist.md`. The harness answers, for every routed
page and on each surface, "does it open, does it fit the screen, does it error?", and lets
a before/after run prove that the desktop web layout is unchanged.

Detailed options for each script are in `tools/ui-sweep/README.md`.

## 1. Prerequisites

| Need | Why |
|---|---|
| Python 3.11+ and `pip install websocket-client` (optional `Pillow`) | All scripts are Python; the DevTools protocol runs over a websocket; Pillow enables the pixel diff. |
| Chrome or Edge on Windows | The web sweep launches it headless with `--remote-debugging-port`. |
| The web host running (`dotnet run --project SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web --launch-profile http`, port 5027) or a deployed URL | Target of the web sweep (`--base-url`). |
| `SPICONE_TEST_USER` / `SPICONE_TEST_PASS` | Staging login used by the web sweep. Without them only `/login` and `/privacy` are checked. |
| Android phone with USB debugging, Debug build of the app, logged in and in the foreground | Target of the device sweep. Xiaomi / Redmi / POCO phones also need **USB debugging (Security settings)** enabled in Developer options, otherwise taps and some DevTools calls are refused. |
| `adb.exe` (Android SDK platform-tools) | Port forward to the WebView's debug socket and screenshots. |

Never point a sweep with a real user's credentials at the live API: the login is a real
login and the pages issue real read calls. Use the staging environment
(`infra/azure/staging.parameters.json`) or a dedicated test account.

## 2. Commands

```
# 1. route list from the source (about 120 routes; three parameterised routes are skipped
#    unless sample ids are supplied)
python tools/ui-sweep/routes.py
python tools/ui-sweep/routes.py --include-params tools/ui-sweep/sample-ids.json

# 2. web sweep at 375, 768, 1280 and 1920 px against the local web host
$env:SPICONE_TEST_USER = '<staging user>'; $env:SPICONE_TEST_PASS = '<password>'
python tools/ui-sweep/sweep_web.py --out tools/ui-sweep/out/web-after

# 3. device sweep on the attached phone (app open and logged in)
python tools/ui-sweep/sweep_device.py --out tools/ui-sweep/out/device-after

# 4. regression check between two runs (desktop pixel diff at 1920 by default)
python tools/ui-sweep/compare.py tools/ui-sweep/out/web-before tools/ui-sweep/out/web-after
```

Each sweep writes `results.json`, `report.md` and `shots/` into its output folder and exits
with `1` when any flag is present, so it can gate a pipeline step. `compare.py` exits with `1`
when a check gained a flag or a desktop screenshot changed more than the threshold.

## 3. Flags

| Flag | Meaning | Checklist item |
|---|---|---|
| `OVERFLOW` | Page wider than the viewport (`scrollWidth > innerWidth`); the report names the widest elements. | 2, 7 |
| `REDIRECT` | The route landed on another path (role guard, login lost, page navigated away). `/` and `/login` are exempt when logged in. | 3, 10 |
| `BLANK` | Under 40 characters of visible text after the wait. | 1, 4 |
| `NOTFOUND` | The router's not-found page or "does not exist" text. | 3 |
| `JSERR` | JavaScript exception or `console.error` during the route. | 1 |
| `ERRUI` | Blazor error banner or reconnect modal visible. | 1, 11 |
| `NETERR` | Failed request or HTTP 400+ (web sweep only). | 1, 4 |

## 4. How the sweep maps to the 12 checks

| # | Check (from `docs/screen-checklist.md`) | Automated | Still manual |
|---|---|---|---|
| 1 | Renders with data, no blank body, no error boundary | `BLANK`, `ERRUI`, `JSERR`, `NETERR`; screenshot per route and width | Judge "with data" from the screenshot |
| 2 | No horizontal scroll, nothing clipped or under system bars | `OVERFLOW` plus offending elements at 375 / 768 / 1280 / 1920 and on the device | Clipping under status and navigation bars: device screenshot |
| 3 | Reachable from menu, back returns, deep link opens | Deep link: `REDIRECT` / `NOTFOUND` | Menu reachability and back gesture |
| 4 | Loading, empty and error states | Only the failure symptom (`BLANK`, `NETERR`) | Throttle the network or stop the staging API and repeat |
| 5 | Primary actions work (save, approve, export, upload, ...) | None (the sweep only navigates) | Against staging; the phase 1 plan extends the sweep to click primary actions |
| 6 | Forms: validation visible, keyboard does not hide fields, native pickers | None | On device |
| 7 | Tables: card mode on phone, sticky header on tablet, full table on desktop | Screenshots at the three widths; `OVERFLOW` for tables that widen the page | Visual review of the screenshots |
| 8 | Drawers and modals: bottom sheet on phone, side drawer elsewhere | None | Manual |
| 9 | Touch targets at least 44 px, no hover-only controls | None yet (a DOM audit can be added to `METRICS_JS` in `sweepcore.py`) | Visual |
| 10 | Role checks | Run the web sweep once per role login; compare the `REDIRECT` sets | Confirm the menu hides the same pages |
| 11 | Session survives restart, expiry warning, 401 to login | `ERRUI` shows a dropped circuit | Manual |
| 12 | Desktop web unchanged for existing users | `compare.py`: no new flags at 1920 and the pixel diff below threshold | Look at the diff images when the threshold is exceeded |

Suggested cadence: run the web sweep on every merged workstream branch (Phase 2 QA agent
role in the programme plan) and keep the previous run as the `before` folder; run the device
sweep before each internal-testing build.

## 5. Reading a report

`report.md` opens with the header (target, browser or device, authenticated or not, wait
seconds, commit) and one table with a row per route and a column per viewport:

```
| # | Route | 375x812 | 768x1024 | 1280x800 | 1920x1080 |
|---|---|---|---|---|---|
| 000 | `/` | ok | ok | ok | ok |
| 014 | `/CreditLimitSales` | **OVERFLOW** | ok | ok | ok |
| 052 | `/Logistics` | **OVERFLOW** **JSERR** | **OVERFLOW** | ok | ok |
```

The "Details for flagged checks" table lists the landed path, `scrollWidth/innerWidth`, the
top offending elements (`table.spic-table`, `div.card-row.flex-nowrap` ...) and the first
errors, each with a link to its screenshot. Fix order: `ERRUI`/`JSERR`/`BLANK` first (the page
is broken), then `OVERFLOW` on phone, then `REDIRECT` (check the role of the test login).

## 6. Known limits

- The device sweep never types credentials; sign in on the phone first. The account signed
  in on the phone decides which pages redirect.
- One Blazor Server circuit is used for a whole run; a crash on one page can make later
  routes show `BLANK` or `REDIRECT` until the reconnect finishes. Rerun with `--filter` from
  that route to confirm.
- Screenshots are viewport-only by default so that the pixel diff compares equal sizes; add
  `--full-page` to capture the entire scroll height on the web sweep.
- Headless Chrome emulates the phone viewport (`Emulation.setDeviceMetricsOverride`); fonts and
  scrollbars differ slightly from a real device, which is why the device sweep exists.
- The harness reports symptoms; it does not click through actions, forms, drawers or role
  matrices (checks 4 to 9 and 11 remain manual for now).
