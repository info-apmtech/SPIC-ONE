# SPIC ONE UI sweep

Automated smoke sweep of every routed page in `SPIC.MauiBlazorApp.Shared/Pages`, on the
Android app (through the WebView's DevTools socket) and on the web host (headless Chrome
or Edge at phone, tablet and desktop widths). Each route is visited, measured and
screenshotted; the result is a `results.json`, a `report.md` with one flag column per
surface, and PNGs. `compare.py` diffs two runs so a change can be checked for regressions,
including a pixel diff of the 1920 px desktop view that live web users see.

Everything is plain Python 3 (3.11+) plus the `websocket-client` package. No Node, no
Playwright, no Selenium. Pillow is optional (pixel diff only).

## Files

| File | Purpose |
|---|---|
| `routes.py` | Derives the route list from `@page "..."` directives. Excludes parameterised routes unless `--include-params sample-ids.json`. |
| `sweep_device.py` | Android sweep over `adb forward` + Chrome DevTools Protocol against the running, logged-in app. |
| `sweep_web.py` | Web sweep: launches headless Chrome/Edge per viewport, logs in through the real form, visits every route, records JS/console/network errors. |
| `compare.py` | Before/after comparison of two runs: new flags, fixed flags, coverage changes, pixel diff of screenshots. |
| `sweepcore.py` | Shared CDP client, in-page metrics script, flag rules, report writers. |
| `sample-ids.json` | Sample values for `{Id}` / `{dealerId}` route parameters. |
| `out/` | Default output folder (git-ignored). |

## Prerequisites

- Python 3.11+ with `pip install websocket-client` (and `Pillow` for the pixel diff).
- Web sweep: Google Chrome or Microsoft Edge installed in the usual location
  (`C:\Program Files\Google\Chrome\Application\chrome.exe`,
  `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`), or pass `--browser <exe>`;
  the web app running, by default at `http://localhost:5027`:
  ```
  dotnet build SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/SPIC.MauiBlazorApp.Web.csproj -c Debug
  dotnet run --project SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web --launch-profile http
  ```
- Device sweep: Android SDK platform-tools (`adb.exe`, default path
  `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`, or `--adb` / `ANDROID_HOME`);
  a phone with **USB debugging** on; the SPIC ONE app installed as a **Debug** build (the WebView
  debug socket `webview_devtools_remote_<pid>` only exists when WebView debugging is enabled),
  running in the foreground and **already logged in**.
  - Xiaomi / Redmi / POCO (MIUI, HyperOS): Developer options need **USB debugging (Security
    settings)** switched on as well, otherwise injected taps and some DevTools input calls are
    refused. Also disable "MIUI optimisation" if the debug socket is not visible.
  - Only one device attached, or pass `--serial <id>` from `adb devices`.
- Test login for the web sweep: environment variables `SPICONE_TEST_USER` and
  `SPICONE_TEST_PASS` (a staging account; the sweep posts them to the API the web app is
  configured with). Without them the sweep checks only the public routes `/login` and `/privacy`.

## Running

Route list (should print about 120 routes; the three parameterised ones are listed as skipped):

```
python tools/ui-sweep/routes.py
python tools/ui-sweep/routes.py --include-params tools/ui-sweep/sample-ids.json
python tools/ui-sweep/routes.py --filter "Stock|Dashboard" --with-files
```

Web sweep (default widths 375x812, 768x1024, 1280x800, 1920x1080):

```
$env:SPICONE_TEST_USER = 'staging-user'; $env:SPICONE_TEST_PASS = '...'
python tools/ui-sweep/sweep_web.py
python tools/ui-sweep/sweep_web.py --widths 375 1280 --out tools/ui-sweep/out/web-before
python tools/ui-sweep/sweep_web.py --base-url https://spicone.in --widths 1920 --filter "Dashboard|Stock"
python tools/ui-sweep/sweep_web.py --size 1366x768 --full-page --wait 4
```

Device sweep:

```
python tools/ui-sweep/sweep_device.py
python tools/ui-sweep/sweep_device.py --package com.apmiot.spicone --wait 4 --filter "Logistics|Guest" --out tools/ui-sweep/out/device-2026-09-19
```

Before/after comparison (regressions and pixel diff):

```
python tools/ui-sweep/sweep_web.py --out tools/ui-sweep/out/web-before      # on the current live build
python tools/ui-sweep/sweep_web.py --out tools/ui-sweep/out/web-after       # on the branch under test
python tools/ui-sweep/compare.py tools/ui-sweep/out/web-before tools/ui-sweep/out/web-after
python tools/ui-sweep/compare.py before after --diff-label all --pixel-threshold 1.0
```

Common options for both sweeps: `--wait` seconds after each navigation (3 to 4 s for pages
that load data), `--filter REGEX`, `--routes-file`, `--include-params`, `--no-screenshots`.

Exit codes: `0` clean, `1` at least one flag (or regression for `compare.py`), `2` setup
failure (no device, app not running, browser or server not found, login failed).

## What is measured per route

After `Blazor.navigateTo(route)` (falling back to a full page load when Blazor is not on the
window) the sweep waits `--wait` seconds, then keeps polling for up to 15 s while the body is
still empty, then evaluates the metrics script:

| Flag | Rule | Usual cause |
|---|---|---|
| `OVERFLOW` | `document.documentElement.scrollWidth > innerWidth + 1`. Offending elements (top five by right edge, excluding fixed elements and anything inside a horizontally scrolling or clipping container) are listed. | Wide tables without a scroll wrapper, fixed-width cards, long unbreakable text, `min-width` on forms. |
| `REDIRECT` | `location.pathname` after navigation differs from the route. `/` and `/login` are exempt when logged in (they bounce to the landing page by design). | Role guard (`PageGuard`) sent the user elsewhere; page navigates away in `OnInitialized`; login lost. |
| `BLANK` | Fewer than 40 characters of visible body text. | Unhandled exception during render, endless loading state, page waiting for data that never comes. |
| `NOTFOUND` | Body text starts with "not found" / "does not exist" / the router's not-found text. | Route not registered, typo in `@page`, route removed. |
| `JSERR` | `Runtime.exceptionThrown` or `console.error` while the route was open. | JS interop failures (Chart.js, Leaflet, Select2), missing elements passed to interop, circuit errors. |
| `ERRUI` | `#blazor-error-ui` is visible or the reconnect modal is showing. | Unhandled .NET exception in the circuit; circuit dropped. |
| `NETERR` (web only) | A request failed (`Network.loadingFailed`, not cancelled) or returned HTTP 400 or higher. | Missing static asset, API 401/403/500, CORS. |

`results.json` keeps the raw numbers (`scrollW`, `iw`, `textLen`, `title`, `offenders`,
`jsErrors`, `consoleErrors`, `networkFailures`) and the screenshot path for each check, keyed by
route and viewport label (`375x812`, ..., or `device`).

## Interpreting a report

1. Look at the summary table first: one row per route, one column per viewport, `ok` or the
   flags in bold.
2. The "Details for flagged checks" table gives the landed path, `scrollWidth/innerWidth`, the
   top offending elements and the first errors, with a link to the screenshot.
3. `REDIRECT` on many routes at once means the test account lacks the role, or the session
   expired: check the `authenticated` line in the header and the landed path (`/login`,
   `/Welcome`, `/Dashboard`).
4. `BLANK` together with `JSERR`/`ERRUI` is a crash; `BLANK` alone is usually a page still
   loading: rerun with a longer `--wait`.
5. `OVERFLOW` only at 375 is a phone layout task (card mode, scroll wrapper); `OVERFLOW` at 1920
   means a fixed width larger than the viewport and must be fixed before release.
6. For a before/after check use `compare.py`: anything under "Regressions" blocks the merge;
   pixel diffs above the threshold at 1920 need a look at the diff image (changed pixels in
   red on a faded copy of the "after" screenshot). Dynamic content (dates, counters, charts)
   produces small diffs; raise `--pixel-threshold` or compare a static route to calibrate.

## Mapping to the screen checklist

`docs/screen-checklist.md` defines twelve checks per page. The sweep covers, or partly covers,
these:

| Check | Coverage by the sweep |
|---|---|
| 1 Renders with data, no blank body, no error boundary | `BLANK`, `ERRUI`, `JSERR`, screenshot. Data presence itself is judged from the screenshot. |
| 2 No horizontal scroll, no clipped content | `OVERFLOW` with offenders at each width; clipping under system bars is visual (device screenshot). |
| 3 Navigation: deep link opens, back works | `REDIRECT` / `NOTFOUND` cover the deep link; back and menu reachability are manual. |
| 4 Loading, empty and error states | Not covered (needs a throttled network or a stopped API on staging). |
| 5 Primary actions work | Not covered; the sweep only navigates. |
| 6 Forms and keyboards | Not covered (manual on device). |
| 7 Tables: card mode on phone, sticky header on tablet, full table on desktop | Screenshots at 375, 768, 1280, 1920 for visual review; `OVERFLOW` catches tables that widen the page. |
| 8 Drawers and modals | Not covered. |
| 9 Touch targets at least 44 px | Not covered yet (a DOM audit can be added to the metrics script). |
| 10 Role checks | Partly: run the web sweep once per test login; `REDIRECT` shows which pages the role is bounced from. |
| 11 Session | Not covered. |
| 12 Desktop web unchanged | `compare.py` on two 1920 runs: flag regressions plus the pixel diff. |

## Limitations

- The device sweep does not log in; the app must already be on an authenticated page. It also
  runs on whatever account is signed in, so role coverage comes from the web sweep.
- Blazor Server keeps one circuit for the whole run; a page that crashes the circuit shows
  `ERRUI` and the following routes may all show `BLANK` or `REDIRECT` until the reconnect
  succeeds. Rerun with `--filter` from the failing route to confirm.
- `Blazor.navigateTo` is an in-app navigation, so per-route network capture starts at the
  navigation and includes requests still in flight from the previous route when `--wait` is short.
- Screenshots are viewport-only by default (stable size for the pixel diff); use `--full-page`
  on the web sweep to capture the whole scroll height.
- Chrome headless enforces a minimum window width, so the viewport is set through
  `Emulation.setDeviceMetricsOverride` (mobile emulation below 768 px); rendering can differ
  slightly from a real phone browser (fonts, scrollbars).
- Parameterised routes need real ids in `sample-ids.json`; nonexistent ids produce pages with
  an empty or error state, which is reported but expected.
