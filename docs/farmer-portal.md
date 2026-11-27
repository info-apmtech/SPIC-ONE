# Farmer Portal

Farmers sign in to SPIC ONE with their mobile number and get one farmer home with advice, dealers,
their SPIC team and tools. Design: `docs/superpowers/specs/2026-11-27-farmer-portal-design.md`;
plan: `docs/superpowers/plans/2026-11-27-farmer-portal.md`. Each task appends its own section.

## 1. Phone OTP sign-in and farmer profile (Task 1, 2026-11-27)

### What it does

- **Sign in** at `/farmer/login` (linked from the normal login page: a "Farmer? Sign in with mobile
  number" entry under the card and a "Farmer sign in" button in the web top bar). Step 1 mobile
  number (`+91` shown), step 2 the 6-digit SMS code with a 30 s resend countdown. Errors are shown
  inline in plain words. The session is stored exactly like the normal login
  (`Services/SessionPersistence.cs`, now also used by `LoginCard.razor` and `Login.razor`).
- **First code = new account.** The first verified code creates an Identity user: `Role = Farmer`,
  `UserName = "f" + mobile`, `PhoneNumber` confirmed, `IsActive = true`, a random 32-byte password
  nobody knows (OTP-only account). An existing Farmer account with the same mobile (for example
  one an admin made for SAS) is reused instead. An unlinked `SasFarmers` row with the same mobile
  is linked to the account (`UserId`), so soil samples / reports appear.
- **Staff / dealer numbers are refused** (409, "This number is registered to a SPIC staff or dealer
  account. Please use the normal login."). Matching checks `PhoneNumber` in the spellings `98..`,
  `098..`, `9198..`, `+9198..`, `+91 98..`, `+91-98..` and a `UserName` equal to the number.
  Each such answer counts against the caller's per-IP limit (a dead "probe" row), so it cannot be
  used freely to find staff and dealer numbers; verify gives the 409 only for a number with a live code.
- **Onboarding** `/farmer/onboarding` (first time) and **My profile** `/farmer/profile`: one form
  (`Components/FarmerPortal/FarmerProfileForm.razor`). Required: name, state, district, village,
  pincode. Optional: email, taluk, GPS ("Use my location"), crops (chips + "Another crop"), land in
  acres, water source, language for AI answers. Typing a pincode fills state / district / taluk
  when they are still empty (existing `api/PincodeLookup`).
- **Farmer home** `/farmer`: placeholder for Task 2 ("Welcome, name", village / district, Edit my
  profile, soil test links when linked, "coming soon" card). An incomplete profile is sent to
  onboarding. After ANY login a Farmer lands on `/farmer` (`LoginState.LandingPage`).
- **Access**: `RouteAccess` gates `/farmer/*` by role: Farmer, Admin, SuperAdmin (support view;
  they have no farmer profile). `/farmer/login` is open. Staff are redirected to their own home. A
  farmer without a designation never sees the staff Welcome / dashboard pages (redirect to
  `/farmer`); `/Sas/MySamples` and `/Sas/MyReports` are open to every Farmer (the API scopes them).
- **Shell**: Farmer tabs are Home, My Samples + My Reports (when the account is linked to a SAS
  farmer or the designation grants SampleCollection), Profile. Farmers get no staff entries in the
  tab bar, rail or More sheet. Desktop sidebar: Home, My Profile (+ the SAS entries).
- `FarmerSession` (scoped) loads `GET api/Farmer/profile` once per session and is the place later
  tasks read the farmer's state / district / crops / language from.

### Endpoints

| Method | Route | Who | Notes |
|---|---|---|---|
| POST | `api/FarmerAuth/otp/request` `{ phone }` | anonymous | 200 `{ success, expiresInSeconds: 300, resendInSeconds: 30, devCode? }`; 400 bad number; 409 staff number; 429 `{ message, retryAfterSeconds }`; 502 SMS failed; 503 Production with `Sms:Provider = Log` |
| POST | `api/FarmerAuth/otp/verify` `{ phone, code }` | anonymous | 200 = normal `LoginResponseModel` + `profileComplete`, `isNewUser`; 400 wrong / expired / too many tries; 403 inactive; 409 staff number |
| GET | `api/Farmer/profile` | Farmer | 200 `FarmerProfileDto` (with `stateName`, `districtName`, `profileComplete`, `sasLinked`) or 404 `{ profileComplete: false, sasLinked, phone }` |
| PUT | `api/Farmer/profile` | Farmer | validates, saves, copies the Name (never the email) to the Identity user; 400 `{ message, errors: { field: text } }` |

Contracts: `SPIC.Core/DTOs/FarmerPortalDtos.cs`. `devCode` is returned ONLY in the Development
environment with the Log provider.

### OTP rules (`SPIC.Core/FarmerPortal/FarmerOtpRules.cs`, pinned by the harness)

Indian mobiles only (`^[6-9][0-9]{9}$`, ASCII digits only, after stripping spaces, `+91` / `0091` /
`91` (12 digits), leading `0`). 6-digit code from `RandomNumberGenerator`; stored as hex SHA-256 of
`code:phone:pepper`; constant-time compare; 5 minute expiry; single use; 5 wrong tries kill the
code ("Too many tries, request a new code"); 30 s resend cooldown; 5 codes per number per rolling
hour; `FarmerPortal:OtpPerIpHourlyCap` (default 20) codes and staff-number probes per client IP per
hour; `FarmerPortal:OtpGlobalHourlyCap` (default 500) codes per hour across ALL numbers (429 "Many
farmers are signing in right now", logged as a warning). Attempts and consumption are updated with
conditional `ExecuteUpdate`. The send limits are counted and the new row inserted inside one
transaction holding PostgreSQL advisory locks (`pg_advisory_xact_lock(hashtextextended(key, 0))` on
the number, then the IP, then a global key), so parallel requests and both API replicas wait for
each other and the caps hold exactly; the SMS goes out after the commit. Challenge rows older than
a day are deleted by the next request. Phones are logged only masked (`98xxxxxx21`).

**Client IP.** The web host is Blazor Server, so every web farmer reaches the API from the web
container. The web host therefore sends the browser's IP (taken from the hub connection: last
`X-Forwarded-For` entry, else the socket) in `X-Spic-Client-Ip` with `X-Spic-Client-Key` =
`FarmerPortal:TrustedClientKey`, on `otp/request` only (`SPIC.MauiBlazorApp.Web/Services/FarmerClientIp.cs`).
The API trusts that header only when its own `FarmerPortal:TrustedClientKey` is non-empty, the key
matches exactly (constant-time) and the value is a valid IP. Otherwise the IP is the LAST
`X-Forwarded-For` entry (the one the ingress appended), else the socket address. The MAUI app calls
the API directly, so its farmers are bucketed by their own (often carrier-shared) public IP.

### Config keys (placeholders in `SpicAPI/appsettings.json`, values from env / Key Vault)

| Key | Meaning |
|---|---|
| `Sms:Provider` | `Log` (default; code written to the API log) or `Msg91` |
| `Sms:Msg91:AuthKey` | MSG91 auth key (secret) |
| `Sms:Msg91:TemplateId` | DLT-approved flow template; its OTP variable must be named `otp` (`##otp##`) |
| `Sms:Msg91:SenderId` | optional DLT sender id |
| `FarmerPortal:OtpPepper` | mixed into every OTP hash; falls back to `Jwt:Key` when empty |
| `FarmerPortal:TrustedClientKey` | shared secret, the SAME value on the API and the web host (both appsettings have an empty placeholder); empty = the web host's browser-IP header is never sent / trusted |
| `FarmerPortal:OtpPerIpHourlyCap` | codes + staff-number probes per client IP per hour; empty = 20 |
| `FarmerPortal:OtpGlobalHourlyCap` | codes per hour across all numbers (SMS pumping backstop); empty = 500 |

MSG91 is called through the Flow API (`POST https://control.msg91.com/api/v5/flow`, JSON body, 10 s
timeout); the code never appears in a URL or a log line.

### Tables (migration `V34_FarmerPortalAuth`)

- `FarmerOtpChallenges` (Id bigint, Phone, CodeHash, CreatedAtUtc, ExpiresAtUtc, Attempts,
  ConsumedAtUtc, RequestIp); indexes (Phone, CreatedAtUtc), (RequestIp, CreatedAtUtc), CreatedAtUtc.
- `FarmerProfiles` (Id, UserId unique, Name, Email, Phone, StateId, DistrictId, Village, Taluk,
  PinCode, Latitude, Longitude, PrimaryCrops (comma separated), LandAcres numeric(10,2),
  IrrigationType (text: Rainfed / Canal / Borewell / Drip / Sprinkler / Mixed), PreferredLanguage
  (en / ta / te / hi / mr / kn / ml), CreatedAtUtc, UpdatedAtUtc); index on DistrictId.

### Other code moved

- `SpicAPI/Services/TokenIssuer.cs` (`ITokenIssuer`): JWT + refresh token creation moved out of
  `AuthenticationController`; the normal login and refresh responses are unchanged.

### Tests

- Harness: `FarmerPortalAuthTests` (phone, codes, hash / compare, attempts / expiry, send limits,
  configurable per-IP and global caps, client IP and the trusted web-host header, profile validation) and `FarmerPortalRouteTests` (routes, landing page, shell tabs,
  SAS-link flag lifetime).
- Smoke: `python tools/farmer-portal-check/smoke_task1.py` against the local API (Development,
  local `spicone_dev` only; refuses non-localhost URLs). With the API started with
  `FarmerPortal__TrustedClientKey=local-smoke-key FarmerPortal__OtpPerIpHourlyCap=5`, run it with
  `--client-key local-smoke-key --ip-cap 5` to also check parallel requests against the locks and
  the trusted-header acceptance / rejection; `--staff-phone` checks the 409 and that probes count.

### Left / for the owner

- Production needs `Sms__Provider=Msg91`, `Sms__Msg91__AuthKey`, `Sms__Msg91__TemplateId` (DLT
  template with variable `otp`) and ideally `FarmerPortal__OtpPepper` before farmers can sign in;
  until then the request answers 503 "SMS is not set up yet".
- **Before go-live:** set `FarmerPortal__TrustedClientKey` (a long random secret from Key Vault) to the
  SAME value on the API and the web host. Without it every web farmer shares the web host's IP, i.e.
  one per-IP bucket (`OtpPerIpHourlyCap`, default 20 per hour) for the whole web portal. Review
  `FarmerPortal__OtpPerIpHourlyCap` (carrier NAT puts many app users behind one IP) and
  `FarmerPortal__OtpGlobalHourlyCap` (default 500/hour) against the expected sign-up rate and the
  MSG91 budget, and confirm nothing (CDN / Front Door) sits in front of the API ingress, which would
  change which `X-Forwarded-For` entry is the client.
- Prod migration `V34_FarmerPortalAuth` (owner runs prod).
- Task 1b restyles these screens with the shared `Fp*` components (six-box OTP input, steps).

## Design system (Task 1b, 2026-11-28)

Every farmer screen composes these pieces instead of ad-hoc markup. Spec:
`docs/superpowers/specs/2026-11-27-farmer-portal-visual-design.md`. Live gallery of every component
and state: **`/dev/farmer-portal`** (Admin / SuperAdmin only; sample data).

### Rules

- Page root: `<div class="fp fp-page my-page">`. `.fp` carries the tokens; `.fp-page` is the centred
  column (max 1120 px, 20 px gaps). Narrow it in your page's `.razor.css` (`.my-page { max-width: 760px; }`).
- Global styles live in `SPIC.MauiBlazorApp.Shared/wwwroot/css/farmer-portal.css` (loaded after
  `tokens.css` in both hosts). **Every selector starts with `.fp`**, so staff pages never change.
  Page-specific layout goes in the page's own `.razor.css` (use `::deep` for classes you pass into
  `Fp*` components through `CssClass`).
- Start every page with `FpHero` (it is the h1 and sets the tab title). Do not use `PageHeader` on farmer pages.
- Every screen designs its loading (`FpSkeleton`), empty (`FpEmpty`) and error (`FpError`) states.
- Estimates and AI answers end with `FpDisclaimer`.
- Plain words; never a danger button for farmers.
- 44 px tap targets on phone: every Fp control already has one (`fp-btn--sm` grows to 44 px on phone,
  `fp-link-btn` keeps its text size but has an invisible 44 px hit area, the slider input is 44 px
  tall). Keep it that way for anything new you add.
- `Layout.css` forces every placeholder to the text colour with `!important`; inside `.fp` placeholders
  are a lighter grey (`#6b7a72`, 4.6:1) so an example like "98765 43210" does not look typed.

### Tokens (CSS custom properties on `.fp`)

`--fp-green-900/800/700/600/500/100/50`, `--fp-gold-700/500/50`, `--fp-soil-600/50`, `--fp-sky-600/50`,
`--fp-violet-600/50`, `--fp-teal-600/50`, `--fp-rose-600/50`, `--fp-ink-900/600/400`, `--fp-line`,
`--fp-line-strong`, `--fp-bg`, `--fp-surface`, `--fp-low/-50` (red), `--fp-medium/-50` (green),
`--fp-high/-50` (violet), `--fp-warn`, `--fp-radius` (16), `--fp-radius-input` (12), `--fp-radius-pill`,
`--fp-shadow`, `--fp-shadow-lift`, `--fp-focus`, `--fp-hero-gradient`, `--fp-hero-pattern`.
Hues for icons, tiles and chips: components take the `FpHue` enum (Green, Gold, Soil, Sky, Violet,
Teal, Rose, Neutral); in plain markup add `fp-hue-{green|gold|...}` (`FpUi.HueCss(hue)`), which sets
`--fp-hue` and `--fp-hue-bg`.

### CSS classes

| Class | Use |
|---|---|
| `fp-btn` + `fp-btn--primary` / `--secondary` / `--ghost` / `--gold` / `--light` (on green) | buttons; `--block` full width, `--sm` small. Primary is 52 px tall and full width on phone |
| `fp-actionbar` | the row holding a form's main buttons; sticky above the tab bar on phone |
| `fp-action-pills` > `fp-action-pill` (`--whatsapp`) | icon + label actions on list cards (Call, WhatsApp, Directions, Request call back) |
| `fp-field`, `fp-label`, `fp-input` (input / select), `fp-input--short`, `is-invalid`, `fp-req`, `fp-optional`, `fp-hint`, `fp-hint--ok` | form fields; errors through `<FpFieldError Text="..." />` |
| `fp-input-group` > `fp-input-addon` + `fp-input` | fixed prefix ("+91") |
| `fp-check` | checkbox row |
| `fp-alert` + `--error` / `--info` / `--success` / `--dev` | inline message strips |
| `fp-form-row`, `fp-grid-2` | two columns (one on phone) |
| `fp-section`, `fp-section-head`, `fp-section-title`, `fp-section-sub`, `fp-section-link` | a titled block of a page |
| `fp-tile-grid` | 2 / 3 / 4 column grid (phone / tablet / desktop) for `FpTile` |
| `fp-chips`, `fp-row`, `fp-stack` | wrapping rows and stacks |
| `fp-h1`, `fp-h2`, `fp-text`, `fp-caption`, `fp-muted`, `fp-num` (tabular digits), `fp-sr-only` | type |
| `fp-icon-circle` (`--sm`), `fp-avatar`, `fp-spinner`, `fp-link-btn` | small pieces |
| `fp-fade-up`, `fp-fade-up-2`, `fp-fade-up-3` | 8 px fade-up on first render (staggered); off with reduced motion |

### Components (`Components/FarmerPortal/`, prefix `Fp`)

| Component | Key parameters | Usage |
|---|---|---|
| `FpHero` | `Title`, `Subtitle`, `Eyebrow`, `BackHref`, `BackLabel`, `DocumentTitle`, `Compact`, `Aside`, `ChildContent` (chips / steps / weather strip) | `<FpHero Title="Dealers near you" BackHref="/farmer" Compact="true"><FpChip Icon="bi-geo-alt-fill" Text="@place" /></FpHero>` |
| `FpCard` | `Title`, `Subtitle`, `Icon`, `Hue` (`FpHue`), `Variant` (`FpCardVariant.Default/Highlight/Tinted/Flush`), `HeaderActions`, `ChildContent`, `Footer`, `Animate`; method `FocusTitleAsync()` | `<FpCard Title="Soil test values" Icon="bi-sliders" Hue="FpHue.Soil">...</FpCard>` |
| `FpTile` | `Title`, `Description`, `Icon`, `Hue`, `Href` or `OnClick`, `Badge`, `Soon` (gallery only: the home never shows unbuilt features) | inside `<div class="fp-tile-grid">` |
| `FpChip` | `Text` / `ChildContent`, `Icon`, `Emoji`, `Hue`, `Small`; with `OnClick` it is a toggle (`Selected`, `aria-pressed`) | `<FpChip Text="Paddy" Emoji="@FpUi.CropEmoji("Paddy")" Selected="@on" OnClick="Toggle" />` |
| `FpStatus` | `Level` (`FpLevel.Low/Medium/High/Unknown`), `Text`, `Large` | `<FpStatus Level="FpLevel.Low" />` |
| `FpZoneSlider` | `Id`, `Label`, `Unit`, `@bind-Value` (double?), `Min`, `Max`, `Step`, `LowBelow`, `HighAbove`, `Decimals`, `Hint`, `Disabled` | `<FpZoneSlider Id="n" Label="Nitrogen (N)" Unit="kg/ha" Max="700" LowBelow="280" HighAbove="560" @bind-Value="_n" />` |
| `FpBigNumber` | `Label`, `Value` (formatted string), `Unit`, `Caption`, `Level` / `LevelText`, `Low` / `Expected` / `High` / `RangeUnit` / `RangeFormat` (range bar), `Confidence` 0-100 / `ConfidenceLabel` (ring) | `<FpBigNumber Label="Expected harvest" Value="4.2" Unit="t / acre" Low="3.6" Expected="4.2" High="4.8" Confidence="72" />` |
| `FpSteps` | `Steps`, `Current` (0-based), `OnSelect` (back to a done step), `AriaLabel` | `<FpSteps Steps="@names" Current="@_step" OnSelect="GoTo" />` (white on green inside `FpHero`) |
| `FpEmpty` | `Art` (`FpArtKind.Sprout/Field/Offline/Search/Team`), `Title`, `Message`, `ChildContent` (buttons), `Card`, `Compact` | `<FpEmpty Art="FpArtKind.Search" Title="No dealers within 25 km" Message="...">buttons</FpEmpty>` |
| `FpError` | `Title`, `Message`, `OnRetry`, `RetryLabel`, `Art`, `Compact` | `<FpError Message="@_error" OnRetry="LoadAsync" />` |
| `FpArt` | `Kind` | inline SVG illustrations (no image files) |
| `FpSkeleton` | `Variant` (`FpSkeletonVariant.Home/Hero/Tiles/Cards/Form`), `Count`, `LoadingText` | `<FpSkeleton Variant="FpSkeletonVariant.Cards" Count="3" />` |
| `FpDisclaimer` | `Text` / `ChildContent`, `Icon` | `<FpDisclaimer Text="This is an estimate..." />` |
| `FpOtpInput` | `Id`, `@bind-Value`, `OnComplete`, `Length`, `Invalid`, `Disabled`, `ReadOnly` (use while checking, keeps the keyboard open), `AriaDescribedBy`; method `FocusAsync()` | six boxes drawn over one real input (`one-time-code`, numeric); pasting "482 913" or a whole SMS fills it (`FpOtpInput.razor.js`) |
| `FpFieldError` | `Text` | inline field error (renders nothing when empty) |

Pure helpers in `FpUi` (`FpUi.cs`, checked by `SPIC.RegressionHarness/FarmerPortalUiTests.cs`):
`Greeting(utcNow)` (IST: 05:00-11:59 morning, 12:00-16:59 afternoon, otherwise evening), `IstDateLine`,
`ToIst`, `ShortName`, `Initials`, `NormaliseOtp`, `ZoneLevel`, `Fraction`, `Pct`, `LevelLabel`,
`Countdown`, `CropEmoji`.

### Home tiles (`Services/FarmerFeatureTiles.cs`)

The home grid is `FarmerFeatureTiles.For(login)` in registry order. **The home shows only features that
exist**: no "coming soon" tiles or blocks. Today the registry holds Dealers near me and My SPIC team
(Task 2), My soil reports and My soil samples (only for farmers with soil tests) and My profile. **A task adds its own entry in the same change that
ships its page**, never earlier:
`new FarmerFeatureTile(id, title, description, "bi-icon", FpHue.X, "/farmer/route", visiblePredicate)`
(optional `Badge`, e.g. "New"). The harness checks ids are unique, every route opens for a farmer and
every route is a real `@page` in the app. Task 2 adds its own "Your SPIC team" block to the home; the
weather strip (Task 3) goes in the home `FpHero`'s ChildContent.

### Screenshots

`python tools/farmer-portal-check/shoot.py --out .superpowers/sdd/<plan>/shots/task-<N> --set farmer|onboarding|gallery|staff|all [--sizes phone,tablet,desktop]`
signs in to the LOCAL web host (refuses other hosts) as the local test farmer through the devCode and
saves 375x812 and 1280x800 (and, with `--sizes`, 768x1024 tablet) pictures (plus `-full` pictures of long pages) with the installed Edge.
Each sign-in uses one of the number's 5 codes per hour; the onboarding set creates two new local test
farmers per run; the gallery and staff sets sign in as the local `qa.admin` (password from
`tools/help-demo/shots.json`; that local user is normally inactive, activate it in `spicone_dev` first).
Add your own screens to the script or reuse its sign-in helper.

## 2. Farmer home, dealers near me and My SPIC team (Task 2, 2026-11-28)

### What it does

- **Home** `/farmer`: two new tiles first in the registry (**Dealers near me**, **My SPIC team**), then the
  soil and profile tiles. Below the tiles a **Your SPIC team** block shows up to three people (the first
  JMDO, MDO and RM when there are such; `FarmerConnectUi.Preview`) with Request call back: a swipeable row
  on phone, three columns on desktop. It loads after the page, so the home never waits for it.
- **Dealers near me** `/farmer/dealers`: starts from the farm location saved in the profile; "Use my
  location" asks the device (`window.getCurrentPosition`, js/location.js, as the profile form does); with
  no GPS it lists the farmer's district. Radius 10 / 25 / 50 / 100 km, a shop-name / village search, list or
  map (Leaflet + OpenStreetMap tiles as on ContactUs / Logistics; pins are divIcons, popups are built with
  `textContent`; `Pages/FarmerPortal/FarmerDealers.razor.js`). Cards: distance, Call (`tel:`), WhatsApp
  (`https://wa.me/91...`), Directions (Google Maps directions to the GPS point, else the dealer's own map
  link, else a Maps search for the firm + village). A plain-words note explains a fallback ("No SPIC dealer
  within 25 km of you, so we show the dealers in Thoothukudi."). The GPS point is rounded to 3 decimals
  (about 110 m) before it is sent (`FarmerGeo.Coarse`).
- **My SPIC team** `/farmer/team`: the JMDO / MDO / RM / RMD cards (role in plain words, headquarter or
  region, open requests), a note when the team came from a fallback, Request call back, and Call / WhatsApp
  only when phone numbers are switched on. A "My requests" link on top.
- **Request a call back** (`Components/FarmerPortal/FarmerCallBackSheet.razor`, shell BottomSheet): topic
  (Fertilizer, Crop problem, Soil test, Product, Dealer, Other), optional message (required for Other, max
  500), preferred time (Morning 9-12, Afternoon 12-4, Evening 4-7, Any time), and a line saying the farmer's
  name, mobile and village go to that officer only.
- **My requests** `/farmer/requests`: the farmer's requests, newest first, with status (Open / Contacted /
  Closed). The staff note is not shown to the farmer.
- **Farmer Call-backs** `/farmer-requests` (staff, title "Farmer Call-back Requests"): JMDO / MDO / RM / RMD
  see the requests addressed to them (farmer name, full phone with `tel:`, village, district, topic, message,
  preferred time) with Open / Contacted / Closed / All tabs and counts, and mark them Contacted / Closed (or reopen)
  with a note in a sheet. Admin / SuperAdmin see every request, read-only, with the addressee; for them the
  farmer's phone is masked (`98xxxxxx21`, no call link) unless they are the addressed officer
  (`FarmerContactRequestRules.PhoneForStaffView`, `farmerPhoneMasked` in the DTO). Menu entry
  "Farmer Call-backs" in the desktop sidebar, the mobile sidebar and the More sheet (More only: it never
  takes a phone tab or rail slot), all through `ShellNavigation.ShowFarmerRequests`.
- **Farmer shell**: Dealers near me, My SPIC team and My call-back requests are in the farmer More sheet
  (`ShellNavigation` FarmerDealers / FarmerTeam / FarmerMyRequests, Farmer only) and the desktop sidebar
  (NavMenu), next to Task 3's entries; the phone tab bar is unchanged.
- **Access**: `/farmer/dealers|team|requests` sit under the role-gated `/farmer` prefix. `/farmer-requests`
  is role-gated in `RouteAccess` (no PagePermission): JMDO / MDO / RM / RMD with a designation, Admin and
  SuperAdmin through their bypass; everyone else goes to their landing page; staff with no designation still
  see only /Welcome.

### Endpoints

| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `api/Farmer/dealers?lat&lng&radiusKm&districtId&q&page&pageSize` | Farmer | `FarmerDealerListDto`: `scope` Nearby (0) / District (1) / State (2) / None (3), `radiusKm` (default 25, 1..100), `usedLocation`, `districtName`, `stateName`, paging (default 20, max 50), `items` (`FarmerDealerDto`). `districtId` defaults to the profile district (unknown id = 400); 404 without a profile |
| GET | `api/Farmer/contacts` | Farmer | `FarmerTeamDto`: `basis` District / NearestDealer / State / None, `nearestDealerKm`, `phonesShown`, `members` (`contactId`, name, role, roleLabel, areaName, areaKind Headquarter / Region, phone / whatsApp only when switched on, openRequests) |
| POST | `api/Farmer/contacts/{contactId}/request` `{ topic, message, preferredTime }` | Farmer | 200 `FarmerContactRequestDto`; 400 `{ message, errors }`; 404 not on the farmer's team; 409 already 3 open requests to that person |
| GET | `api/Farmer/contacts/requests` | Farmer | the caller's own requests (newest 100), without the staff note |
| GET | `api/FarmerRequests?status&page&pageSize` | JMDO / MDO / RM / RMD (own), Admin / SuperAdmin (all, `adminView`) | `StaffFarmerRequestListDto` with per-status counts; Open first (oldest waiting first); other roles 403 |
| PUT | `api/FarmerRequests/{id}` `{ status, note }` | the addressed staff member only | 200 `StaffFarmerRequestDto`; 404 for anyone else (ids cannot be probed); Admin 403 |

Contracts: `SPIC.Core/DTOs/FarmerConnectDtos.cs`. Enums travel as numbers like the rest of the API.

### Rules (`SPIC.Core/FarmerPortal`, pinned by the harness)

- **Which dealers** (`FarmerDealerRules.ListableForFarmers`, EF-translatable): `Status == Active`; not
  `InactiveProposal` Terminated ("to be terminated") or NotTraceable; `IsDealer` and `DealerType` null or
  Dealer (no Department / Institution registrations); a firm name; and a Create New Dealer registration
  (`IsNewDealerRegistration`) only after its final AVP approval gave it a code (`AVPApproved == true` OR
  `FinalApprovalDate` set, and a `DealerCode`; the credit-limit maintenance cycle resets `AVPApproved` to
  null on an approved dealer, `FinalApprovalDate` is never cleared), because its `Status` is the enum default Active while it is still in the approval flow
  (the controller sets Active at the final AVP approval). Existing / bulk-loaded dealers are trading dealers,
  so their Status alone decides (their RM / SM / AVP flags describe a later credit-limit review).
- **Public fields only**: the dealer query projects firm name, shop / street, sub-village, village, taluk,
  district, pincode, official contact number, WhatsApp number, map URL and GPS. PAN, bank, GST, Aadhaar,
  owner and credit data never leave the database. Numbers are normalised (`FarmerContactLinks`: a mobile as
  10 digits, a landline with its STD code; junk gives no button); a map URL must be absolute http(s).
- **Fallback chain**: GPS and a dealer within the radius (bounding box in SQL, exact haversine in memory) ->
  Nearby, nearest first; else the district (address `DistrictId`), nearest first with GPS (dealers without
  GPS last) or by name; else the state (`DealerStateId`, else the business `StateId`); else None.
  `FarmerDealerRules.Select` is the in-memory form of the same chain.
- **Team** (`FarmerTeamRules.Resolve`): listable dealers in the farmer's district -> their `HQ` and
  `Region` ids (a dealer without a Region takes its Headquarter's region) -> active `Employeelogins` (login
  and user account both active, and the login role equal to the account role `AppUsers.Role`, which the
  token and the staff page use: `FarmerTeamRules.IsTeamLogin`) with role JMDO / MDO whose `HeadquartersId` or `HeadquartersIds` match, and
  RM / RMD whose `RegionId` or `RegionIds` match. No dealer in the district: the nearest listable dealer with
  GPS within 100 km of the farmer's saved location. Nobody found: the RM / RMD logins of the farmer's
  state. Ordered JMDO, MDO, RM, RMD, then by how many of the area's dealers their HQ / region serves, then
  name; one card per person; at most 8. MO is not on the list (the brief names JMDO / MDO / RM / RMD).
  Names come from `EmployeeInformation.Name`, else the account name. Phones, when shown, are the
  **official** number only (never the personal one).
- **Requests** (`FarmerContactRequestRules`): the topic must be one of the six, message max 500 (required
  for Other), preferred time one of the four keys, at most 3 **Open** requests from one farmer to one
  officer (counted and inserted under a PostgreSQL advisory lock on farmer + officer), the officer must be on
  the farmer's resolved team at that moment, staff note max 500.

### Config keys

| Key | Meaning |
|---|---|
| `FarmerPortal:ShowStaffPhoneNumbers` | `true` shows the officers' official numbers (Call / WhatsApp) on My SPIC team; default `false` (placeholder in `SpicAPI/appsettings.json`) |

### Table (migration `V35_FarmerPortalConnect`)

- `FarmerContactRequests` (Id, FarmerUserId, StaffUserId, StaffRole (text, the role when asked), Topic
  (text), Message varchar(500), PreferredTime varchar(40)?, Status (text: Open / Contacted / Closed),
  StaffNote varchar(500)?, CreatedAtUtc, UpdatedAtUtc); indexes (FarmerUserId, CreatedAtUtc) and
  (StaffUserId, Status, CreatedAtUtc). No foreign keys, like the Task 1 tables.

### Tests and tools

- Harness: `FarmerPortalConnectTests` (haversine, bounding box, listable dealer rule, radius / paging,
  fallback chain, links, card address, team resolution with fallbacks / multi-assignment / cap, request and
  update validation, routes and menu for every role, IST times, scope / basis notes, home preview) and the
  updated tile expectations in `FarmerPortalUiTests`.
- Local data: `tools/farmer-portal-check/seed_task2_local.sql` (refuses any database but `spicone_dev`)
  adds nine "Demo ..." dealers around Kovilpatti / Thoothukudi / Tirunelveli (three that must never be
  listed), "Demo ..." employee records for the QA staff logins, a JMDO login for `qa.jmdo`, and activates
  the QA users it needs; `cleanup_task2_local.sql` undoes it.
- Smoke: `python tools/farmer-portal-check/smoke_task2.py` (local API only: scopes, exclusions, no private
  fields, the cap, staff scoping, Admin read-only, 401 / 403s).
- Screenshots: `python tools/farmer-portal-check/shoot_task2.py --out .superpowers/sdd/<plan>/shots/task-2`
  (fakes the device location at Kovilpatti; sends one "[demo]" request).

### Left / for the owner

- Decide `FarmerPortal__ShowStaffPhoneNumbers` (off by default; the design call says owner to confirm).
- Prod migration `V35_FarmerPortalConnect` (owner runs prod).
- Data quality decides how useful this is: dealers need `DistrictId`, GPS and the official / WhatsApp
  numbers filled; staff logins need the right HQ / Region (and an `EmployeeInformation` name / official
  number). Bulk-loaded dealers have no address or GPS, so they show only in district / state lists.
- No notification to the officer when a request arrives (they see it on Farmer Call-backs); a push / SMS
  could follow.

## 3. Trends in my area, the home weather strip and Community near me (Task 3, 2026-11-28)

### What it does

- **Home weather strip** (`/farmer`, inside the `FpHero`): the 7-day forecast as day pills (Today, Tmrw, Mon
  ...: icon, max / min, rain), the most important farm advisory under it and a "Farm advice" link to
  `/farmer/trends`. It loads after the page (like the SPIC team block). No GPS on the profile: "Add your
  location to see weather" with a **Use my location** button that opens `/farmer/profile?focus=location`
  (the profile page scrolls to "Farm location"). Forecast unavailable: the strip is left out.
  Component: `Components/FarmerPortal/FarmerWeatherStrip.razor`.
- **Trends in my area** `/farmer/trends` (home tile, More sheet, desktop sidebar): the weather strip in the
  hero, **Farm advice for this week** (the advisories + disclaimer), then four cards from SPIC ONE data of
  the last 90 days in the farmer's district (the state when the district has too little):
  **What farmers near you grow** (crop share bars), **Soil problems near you** (share of soil samples the
  lab marked Low per nutrient), **Talked about near you** (top 5 discussions, linking to the discussion)
  and **SPIC dealers** (count, "Dealers near me"). Each card says when it fell back to the state and has a
  designed empty state; a footer states the privacy rule.
- **Community near me** `/farmer/community` (home tile, More sheet, desktop sidebar): Knowledge Community
  discussions whose authors are near the farmer, newest activity first, "Show more" paging, "Ask a
  question" (`/Community/new`) and "All discussions" (`/Community/discussions`). Cards
  (`FarmerDiscussionCard`) open the existing discussion page. Farmers also get a **Near Me** tab on
  `/Community/discussions` (`?tab=near`).
- **Farmers can open the Knowledge Community**: `RouteAccess` now allows every `/Community/*` route for the
  Farmer role (it is open to every signed-in user anyway, `OpenAccessPages`); before, a farmer without a
  designation was sent home.
- **Farmer authors**: a discussion or reply by an OTP farmer carries the farmer profile's name and
  "Village, District" (`CommunityController.ResolveAuthorAsync`); staff keep "HQ, State".

### What "near" means (no migration)

Discussions have no location column and none was added. A discussion is near when its **author** is
(`SpicAPI/Services/CommunityNearResolver.NearPosts`, scope choice in `SPIC.Core/FarmerPortal/CommunityNearRules.cs`):

- a farmer whose farmer profile (else linked `SasFarmers` record) is in the district;
- an active SPIC staff login (`Employeelogins`) whose headquarter (`HeadquartersId` or `HeadquartersIds`) is
  one of the headquarters of the listable SPIC dealers registered in the district (the "My SPIC team"
  mapping); a login without any headquarter matches by region;
- state level: also every farmer and staff login of the state.

**Scope:** the district when it has at least 3 near discussions (`CommunityNearRules.MinDistrictPosts`),
else the state (which includes the district's), else nothing; `scope` / `areaName` in the answer say which
applied, and the page shows a note when it is the state. "Talked about near you" uses the same rule on the
discussions active in the 90-day window.

The caller's area is the farmer profile (district + state); for staff it is the `spic:state_id` claim (state
only). The author test is a SQL sub-query (`AuthorUserId IN (profiles ...) OR IN (SAS farmers ...) OR IN
(staff logins ...)`, array overlap `&&` for multi-HQ / multi-region logins), so counting, ordering and paging
run in the database: per request one small dealer-HQ lookup, at most two COUNTs and the page query; nothing
is loaded into memory per author (fix round 1). Old discussions count too (matched by their author's current
place). Dealer accounts are not linked to a dealer registration, so dealers' posts never count as near.

### Endpoints

| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `api/Farmer/trends` | Farmer | `FarmerTrendsDto`: `districtName`, `stateName`, `windowDays` (90), `areaComputedAtUtc`, `weather`, `crops`, `soil`, `talked`, `dealers`; each card has `scope` District (0) / State (1) / None (2) and `areaName`. 404 without a profile; staff 403 |
| GET | `api/Farmer/weather` | Farmer | `FarmerWeatherDto`: `status` Ok (0) / NoLocation (1) / Unavailable (2), `days` (date, weatherCode, max / min deg C, rain mm, rain chance %, wind km/h), `advisories` (code, level Info 0 / Caution 1 / Good 2, icon, title, text), `fetchedAtUtc` |
| GET | `api/Community/near?page&pageSize` | any signed-in user | `CommunityNearPageDto`: `scope`, `areaName`, `items` (`DiscussionSummaryDto`), paging (default 10, max 50) |
| GET | `api/Community/discussions?tab=near&...` | any signed-in user | the existing list with the same "near" rule |

Contracts: `SPIC.Core/DTOs/FarmerTrendsDtos.cs`. Client: `Services/FarmerAreaApi.cs` (no toasts).

### Data sources and rules (`SPIC.Core/FarmerPortal/FarmerTrendRules.cs`, pinned by the harness)

| Card | Source | Rule |
|---|---|---|
| What farmers near you grow | `FarmerProfiles.PrimaryCrops` (all profiles of the area) + `SampleItems.Crop1/Crop2` of SAS samples collected in the last 90 days (not Draft) | one count per farmer (account user id, else SAS farmer id); district if it has >= 5 farmers with crops, else state, else hidden; a crop is listed only when >= 2 farmers grow it; top 6 |
| Soil problems near you | `LabReports` (Soil, generated in the last 90 days) -> `SampleBatches` -> `SampleItems` -> `SampleCollections` -> `SasFarmers` (district / state; deleted batches, samples, collections and farmers excluded) -> `SampleLabResults` for N, P, K, Zn, Organic Carbon, S, Fe, B, Mn, Cu with a value | the lab's own `Status` (`Deficient` = Low); one result per sample and nutrient; district if it has >= 5 samples, else state, else hidden; a nutrient tested on < 5 samples is hidden (counted in `hiddenForFewSamples`); nutrients with no Low sample are not listed; top 6 by Low share |
| Talked about near you | `CommunityPosts` active in the last 90 days by "near" authors + their `CommunityPostReplies` of the window | score = recent replies x 3 + likes + views / 10; discussions with no activity left out; scored, ordered and limited in SQL; top 5; the district when it has >= 3 such discussions, else the state |
| SPIC dealers | `DealerRegistrations` with `FarmerDealerRules.ListableForFarmers` | count in the district, else in the state (`DealerStateId ?? StateId`), else none |

Only these totals leave the API: never a farmer's name, phone, village, GPS or user id.

### Weather (`SpicAPI/Services/FarmerWeatherService.cs`, `SPIC.Core/FarmerPortal/FarmerWeatherRules.cs`)

- Open-Meteo `https://api.open-meteo.com/v1/forecast` (free, no key; overridable with
  `FarmerPortal:WeatherUrl`), daily `weather_code, temperature_2m_max/min, precipitation_sum,
  precipitation_probability_max, wind_speed_10m_max`, `timezone=Asia/Kolkata`, 7 days. The only thing sent
  is the profile GPS **rounded to 0.1 deg** (about 11 km). Named HttpClient `OpenMeteo`, **8 s timeout**.
  Coordinates are never logged.
- No GPS on the profile: no call, `NoLocation` (the codebase has no district coordinates, so no guessing).
- Caches (`IMemoryCache`, per API replica): a forecast **1 h per rounded point** (a failure 5 min); the trends
  area numbers **1 h per district + state**.
- Advisories (at most 3, in this order):

| Code | When | Advice |
|---|---|---|
| `heavy-rain` | rain over the next 3 days > 50 mm | avoid fertilizer top-dressing and spraying before the rain; keep drains open |
| `rain-soon` | not heavy, and today or tomorrow >= 10 mm or >= 70 % | spray only with ~6 dry hours ahead; top-dress after the rain |
| `heat` | max >= 38 deg C in the next 3 days | water early morning / evening; no afternoon spraying |
| `wind` | wind >= 30 km/h today or tomorrow | do not spray in strong wind |
| `dry-week` | 7-day rain < 5 mm and no day >= 40 % | plan irrigation; fertilizer on moist soil |
| `spray-window` | none of heavy / soon / wind, and today and tomorrow each < 1 mm, < 30 %, wind < 20 km/h | good days for spraying and top-dressing |

### Config keys

| Key | Meaning |
|---|---|
| `FarmerPortal:WeatherUrl` | optional forecast endpoint (default Open-Meteo); not in appsettings because the default is right |

### Shell

- Home tiles (registry order): Dealers near me, My SPIC team, **Trends in my area**, **Community near me**,
  (soil tiles), My profile.
- More sheet: `ShellNavigation.FarmerTrendsKey` / `FarmerCommunityKey` (farmer only; never a phone tab or rail
  slot, the farmer tab bar is unchanged). Desktop sidebar: "Trends in my area" and "Community near me" under
  My Profile.

### Tests and tools

- Harness: `FarmerPortalTrendsTests` (0.1 deg rounding, weather codes, every advisory threshold, order and cap,
  Open-Meteo parsing incl. nulls and error bodies, the soil minimum-sample privacy rule, crop minimums and
  de-duplication, talked-about ordering, the "near" author rule, routes / menu / tiles, text); updated tile
  and More-sheet expectations in `FarmerPortalUiTests` / `FarmerPortalRouteTests`.
- Local data: `tools/farmer-portal-check/seed_task3_local.sql` (after `seed_task2_local.sql`; refuses any
  database but `spicone_dev`) adds five discussions tagged `demo-task3` by the local test farmers and
  qa.mdo / qa.jmdo, with eight replies; `cleanup_task3_local.sql` removes them (and any `[smoke]` post).
- Smoke: `python tools/farmer-portal-check/smoke_task3.py [--phone <no-GPS farmer>] [--gps-phone <GPS farmer>]`
  (local API only: 401 / 403s, NoLocation, each card's scope and minimums, no personal fields, the 1 h
  caches, near paging / tab / staff state, the farmer author's name and place, the live forecast).
- Screenshots: `python tools/farmer-portal-check/shoot_task3.py --out .superpowers/sdd/<plan>/shots/task-3`
  (two sign-ins: a farmer without and one with a farm location).

### Left / for the owner

- No migration. The API now calls `api.open-meteo.com` over HTTPS; check that prod egress allows it
  (otherwise the weather is simply left out).
- Open-Meteo's free API is meant for non-commercial use (attribution given in the disclaimer). The owner
  should confirm that SPIC's use qualifies, or take their commercial plan (then set `FarmerPortal:WeatherUrl`
  to the customer endpoint; an API key would need a small change).
- The caches are per replica, so two replicas mean up to two forecast calls per point per hour.
- Prod data decides how full the cards are: farmer profiles start near zero, so "What farmers near you grow"
  leans on SAS sample crops at first; staff logins need the right HQ for "near" to find staff posts.

## 4. Fertilizer advice (Task 4, 2026-11-28)

### What it does

- **Fertilizer advice** `/farmer/fertilizer` (first home tile, first More sheet entry, desktop sidebar under My
  Profile). Two input cards like the owner's sketch: **Soil test values** (Available Nitrogen / Phosphorus /
  Potassium in kg/ha and soil pH on `FpZoneSlider`s with live Low / Medium / High chips; pH shows the lab's
  Acidic / Neutral / Alkaline) and **Crop-wise recommendations** (crop, type chips, soil type, area in acres; number
  of plants / palms for banana and coconut). Defaults N 100, P 5, K 50, pH 7.0; area from the profile (else 1);
  crop from the profile crops when one has advice. Desktop: soil card left, crop card and the button right; phone:
  stacked with the sticky button.
- **Use my soil report** (farmers linked to a SAS farmer record): fills N / P / K / pH from the latest completed
  SPIC soil report (`LabReports`, soil, not deleted), converting kg/acre or ppm to kg/ha if a row says so; the
  run records the report id (checked against the caller's own reports). Changing a value afterwards drops the
  link ("this advice uses your own numbers").
- **Result** (`Components/FarmerPortal/FertilizerResultView.razor`): status and need (kg N / P2O5 / K2O per acre)
  per nutrient with the adjustment ("+25 % because your soil is Low"), pH; **What to buy** for the whole area
  (DAP route, or SSP instead), kg and bags (urea 45 kg, others 50 kg; "Sold as SPIC Urea / SPIC DAP"); **When to
  apply** (each split with its days and products, grams per plant for banana / coconut); pH correction; **water,
  soil and nutrient roadmap** (`FertilizerTipCard`, each tip with its TNAU source link); "How we worked this out"
  (TNAU dose, your dose, the rule, the source); Ask my MDO / Find a SPIC dealer / My past advice; disclaimer
  "General guidance from TNAU/ICAR norms. For exact advice take a SPIC soil test or ask your MDO." with a My SPIC
  team link.
- **My past fertilizer advice** `/farmer/fertilizer/history` (latest 20) and `/farmer/fertilizer/history/{id}` (one
  saved run in full). `FpZoneSlider` gained optional `LowText` / `MediumText` / `HighText` (chip words).

### Endpoints (`SpicAPI/Controllers/FarmerFertilizerController.cs`, Farmer role, scoped to the caller)

| Method | Route | Notes |
|---|---|---|
| GET | `api/Farmer/fertilizer/options` | `FertilizerOptionsDto`: crops with variants (`normId`, `soilType` when the norm is soil-specific, water regime, basis, plants/ha, spacing), soil types, N / P / K / pH ranges (`lowBelow`, `highAbove`, labels, `fromLabMaster`), products |
| GET | `api/Farmer/fertilizer/soil-report` | `FertilizerSoilReportDto`: `available`, `reportId`, `reportCode`, `generatedAt`, `n` / `p` / `k` (kg/ha), `ph`, `notes` (conversions, unreadable values); `message` when there is none |
| POST | `api/Farmer/fertilizer/recommend` `{ normId, soilType, areaAcres, plantCount?, n, p, k, ph, soilReportId? }` | 200 `FertilizerRecommendationDto` (saved as a run, `runId`); 400 `{ message, errors }` |
| GET | `api/Farmer/fertilizer/history` | latest 20 `FertilizerRunSummaryDto` |
| GET | `api/Farmer/fertilizer/history/{id}` | one of the caller's runs in full; 404 for anyone else's |
| GET | `api/Farmer/fertilizer/latest` | the caller's latest run in short, 204 when none. **For the chatbot context and the yield forecast NPK prefill**: `crop`, `variant`, `soilType`, `areaAcres`, `soilN/P/K/Ph`, `nStatus/pStatus/kStatus` (Low / Medium / High), `phBand`, `needN/P/KKgPerAcre` (after the soil test adjustment), `fromSoilReport`, `summary` (one line) |

Contracts: `SPIC.Core/DTOs/FertilizerDtos.cs`. Client: `Services/FarmerFertilizerApi.cs` (no toasts).

### Rules (`SPIC.Core/FarmerPortal/FertilizerAdvisor.cs`, pinned by the harness)

- **Rating = the soil report's.** Thresholds come from the lab parameter master (`S-N`, `S-P`, `S-K` via
  `Sas:Lab:NutrientParameters`, and `S-PH`) and are applied exactly like `LabAutoResultEngine`: below RangeMin Low
  (Deficient), above RangeMax High (Excess), RangeMin..ModerateFrom Medium (Moderate) when set, else Medium (Normal).
  Seeded master: N 280-560, **P 22-56, K 110-280** kg/ha, pH 6.5-7.5. The brief's numbers (N 280/560, P 11/22,
  K 118/280) are only the fallback when a master row is missing.
- **Adjustment = the soil report's dose factors** (`LabFertilizerSchedule.FactorFor`, `Sas:Lab:DoseFactors`):
  Deficient 1.25, Moderate 1.10, Normal 1.00, Excess 0.75 by default (the soil-test rating approach: low +25 %,
  high -25 %, ICAR Indian Farming 70(3) 2020). Every split is scaled by its nutrient's factor.
- **Products per split:** DAP (18-46-0) for the P2O5 but never more DAP-N than the split's N; the rest of the P2O5
  as SSP (16 % P2O5); urea (46 % N) for the N left; MOP (60 % K2O) for K2O. Alternative SSP route: SSP + urea +
  MOP. Product kg rounded to whole kg (one decimal under 0.5 kg), bags to one decimal.
- **pH bands:** < 5.5 strongly acidic (lime as per the lime requirement test), below the lab minimum (6.5) slightly
  acidic, up to the lab maximum (7.5) neutral, up to 8.5 slightly alkaline (organic matter; gypsum only for a sodic
  soil, as per the gypsum requirement test), > 8.5 strongly alkaline / sodic (TNAU reclamation). No lime or gypsum
  dose is computed: TNAU ties both to a soil test.
- **Units:** report values in kg/acre x 2.47105, ppm / mg/kg x 2.24 (0-15 cm plough layer); other units are not used.
- **Area:** 1 ha = 2.47105 acres. Per-plant crops: plants = acres / 2.47105 x the source's plants per hectare
  (banana 3,080 at 1.8 x 1.8 m, coconut 175 at 7.5 x 7.5 m) unless the farmer enters a count.
- Validation: area 0.1-1000 acres, plants 1-500,000, N 0-2000, P 0-500, K 0-2000 kg/ha, pH 3-11, a known soil type,
  a soil-specific norm only on its soil.

### Tables (migration `V36_FertilizerAdvice`)

- `FertilizerCropNorms` (Id, Crop, Variant, WaterRegime, SoilType?, Basis PerHectare / PerPlant, N, P2O5, K2O
  numeric(10,2), PlantsPerHectare?, Spacing?, DurationDays?, Notes, Source, SourceUrl, SortOrder, IsActive): 27 rows.
  `FertilizerNormSplits` (NormId, Seq, Stage, DayFrom?, DayTo?, DayBasis Sowing / Planting / Calendar = named stage,
  N, P2O5, K2O; the splits add up to the norm): 65 rows. `CropSoilTips` (Crop?, SoilType?, WaterRegime, Category
  Water / Soil / Nutrient, Text, Source, SourceUrl): 66 rows. Seed and sources: `Spic.Infrastructure/Data/FertilizerAdviceSeed.cs`.
- `FertilizerRecommendationRuns` (Id bigint, FarmerUserId, NormId, Crop, Variant, SoilType, AreaAcres, SoilN/P/K/Ph,
  N/P/KStatus, PhBand, NeedN/P/KKgPerAcre, FromSoilReport, SoilReportId, Summary, InputsJson, ResultJson,
  CreatedAtUtc); index (FarmerUserId, CreatedAtUtc). No foreign keys, like the other farmer tables.

Crops: Paddy (medium, long, short Cauvery delta / Coimbatore, short other areas), Cotton (irrigated varieties,
irrigated hybrids, rainfed), Maize (irrigated varieties, irrigated hybrids, rainfed on black soil, rainfed on red
soil), Groundnut (rainfed), Black gram and Green gram (rainfed, irrigated), Soybean (irrigated, rainfed), Wheat,
Chilli, Tomato (varieties), Onion (small, Bellary), Turmeric, Banana (garden land: Robusta-type and Nendran, per
plant), Coconut (from the 5th year, per palm). **Left out on purpose** (sources disagree or lack a schedule):
sugarcane, irrigated groundnut, hybrid rice, hybrid tomato.

### Tests and tools

- Harness: `FarmerPortalFertilizerTests` (ratings at 279/280/560/561, 10.9/11/22/22.1, 117/118/280/281 and on the
  lab master, Moderate, factors equal to the soil report's incl. configured ones, unit conversion, DAP-first
  conversion incl. the N cap, SSP route, pH bands, seed integrity and the brief's doses, the sketch case end to
  end, per-plant crops, validation, the soil report reader, slider ranges, UI helpers, tile / menu / routes);
  updated tile and More-sheet expectations in `FarmerPortalUiTests` / `FarmerPortalRouteTests`.
- Local data: `tools/farmer-portal-check/seed_task4_local.sql` (spicone_dev only) links a "Demo Task4 Murugan" SAS
  farmer and a completed soil report `DEMO-T4-RPT-1` (N 180, P 18, K 95 kg/ha, pH 8.1) to the local test farmer
  6777777703; `cleanup_task4_local.sql` removes it and the local test farmers' runs.
- Smoke: `python tools/farmer-portal-check/smoke_task4.py [--phone 6777777703]` (local API only).
- Screenshots: `python tools/farmer-portal-check/shoot_task4.py --out .superpowers/sdd/<plan>/shots/task-4 [--phone ...]`.

### Left / for the owner

- Prod migration `V36_FertilizerAdvice` (owner runs prod). No config keys: thresholds and factors are the lab's.
- An agronomist (SPIC agri services) should sign off the seeded norms and tips before go-live; the doses are TNAU's
  blanket recommendations, read from the TNAU Agritech pages named on every row.
- Sugarcane, irrigated groundnut, hybrid rice and hybrid tomato need a confirmed source before they are added.
- SPIC complex fertilizers (SPIC Jyoti, Sangamam ...) are not offered because no grade is recorded anywhere in the
  code or the product master; add them when the master carries their grade.

## 5. Ask SPIC AI, the farmers' chatbot (Task 5, 2026-11-28)

### What it does

- **Ask SPIC AI** `/farmer/assistant` (second home tile "Ask SPIC AI", More sheet after Fertilizer advice, desktop
  sidebar under Fertilizer advice). A chat: the farmer types a farming question (max 1000 letters) or taps a starter
  question for their crops; SPIC AI answers **in the profile's PreferredLanguage** (the screen itself stays English),
  grounded in the published Digital Library items retrieved for the question and the farmer's own context. Answers
  show the library items used as link chips ("From the SPIC library", opening `/DigitalLibrary/view/{id}`), three
  follow-up chips and the time in IST; a typing indicator shows while waiting; under the composer the disclaimer
  "SPIC AI can make mistakes. Check doses on the product label or with your SPIC team before you use them." with a
  My SPIC team link.
- **My chats**: the farmer's conversations, newest first, grouped Today / Yesterday / Earlier (IST), with delete
  (confirm dialog; soft delete). Desktop / tablet: a column left of the chat; phone: `/farmer/assistant/chats`
  ("My chats" button). `/farmer/assistant/{id}` opens one chat.
- **Daily cap**: 30 questions per farmer per IST day (midnight to midnight IST, computed from UTC). The hero shows
  "N of 30 questions left today"; at the limit the composer is replaced by a "That's all for today" card with the
  API's message and Ask my SPIC team / Community near me buttons.
- **No Anthropic key** (local, or until the owner sets one): every answer is the **basic answer**, built from the
  matching library items and always starting "Basic answer from the SPIC library: ..."; the bubble carries a gold
  "Basic answer from the library" tag, the empty chat says "For now SPIC AI gives basic answers from the SPIC library,
  in English", and a non-English farmer is told basic answers are in English only. A pesticide question adds the
  label / safety line. The same basic answer is used when the Anthropic call fails or is refused.
- Access: Farmer role (API `[Authorize(Roles = Farmer)]`, page under `/farmer`); Admin / SuperAdmin see a "viewing as
  support staff" note. Farmers may now open `/DigitalLibrary/view/{id}` (the source links); the rest of the library
  keeps its rules (`RouteAccess.FarmerRoute`).

### Endpoints (`SpicAPI/Controllers/FarmerAssistantController.cs`, Farmer role, scoped to the caller)

| Method | Route | Notes |
|---|---|---|
| GET | `api/Farmer/assistant/status` | `FarmerAssistantStatusDto`: `dailyLimit`, `usedToday`, `leftToday`, `resetsAtUtc` (next IST midnight), `maxQuestionChars`, `fullAnswers` (key configured), `language`, `languageName`, `starters` |
| GET | `api/Farmer/assistant/conversations` | the caller's conversations (newest 50): `id`, `title`, `createdAtUtc`, `updatedAtUtc`, `questionCount` |
| GET | `api/Farmer/assistant/conversations/{id}` | `FarmerChatDetailDto` with `messages` (`role`, `text`, `createdAtUtc`, `sources`, `suggestions`, `provider`); 404 for anyone else's |
| DELETE | `api/Farmer/assistant/conversations/{id}` | 204; soft delete; its questions still count today |
| POST | `api/Farmer/assistant/ask` `{ conversationId?, question }` | 200 `FarmerAskResponse` (`conversationId`, `conversationTitle`, `question`, `answer`, `leftToday`, `resetsAtUtc`); 400 empty / over 1000 letters; 404 unknown conversation; 429 `{ message, retryAfterSeconds, resetsAtUtc, dailyLimit }` + `Retry-After` |

Contracts: `SPIC.Core/DTOs/FarmerAssistantDtos.cs`. Client: `Services/FarmerAssistantApi.cs` (no toasts).

### How an answer is made

1. Validate (`FarmerAssistantRules.Validate`) and load the conversation (the caller's, not deleted).
2. Load the farmer context (`SpicAPI/Services/FarmerAssistantContextLoader.cs`): profile crops, district and state
   names, land acres, water source, language; the **ratings only** (the lab's label, else Low / Medium / High from the
   status) of the latest completed SPIC soil report (same report rule as the fertilizer controller); the one-line
   summary of the latest fertilizer advice run. Never the name, phone, village, GPS or user id.
3. Under `pg_advisory_xact_lock(hashtextextended('fai:' + userId, 0))`: count the farmer's questions since IST midnight
   (`FarmerChatMessages` with Role user, deleted chats included); at the limit 429; else store the question (and a
   new conversation) and commit. The lock is released before the answer is made, so parallel taps and both API
   replicas cannot pass the cap (smoke: 8 parallel asks at 26 used give exactly 4 answers and 4 x 429).
4. Retrieve library items (`LibraryRetriever`, published only, top 4) and call `IAssistantProvider` in
   `AssistantMode.Farmer` with the farmer system prompt (`FarmerAssistantRules.SystemPrompt(language)`), the context
   block (`FarmerAssistantRules.ContextBlock`), the last 10 earlier messages (`FarmerAssistantRules.History`: starts
   with a question; back-to-back same-role messages joined) and `Assistant:FarmerMaxTokens` (1500).
5. Store the answer (sources, follow-ups, provider, language) even if the farmer has left the page.

The Anthropic call now lives in `Spic.Infrastructure/Services/Assistant/AnthropicMessagesClient.cs` (raw HTTP as
before; content blocks incl. `AnthropicContent.ImageBase64` for the leaf-photo check; HTTP errors, `refusal` and empty
replies return a failed result and the provider falls back). The library assistant keeps its own prompt, 800 tokens
and behaviour.

### Config keys

| Key | Meaning |
|---|---|
| `Assistant:AnthropicApiKey` | (existing) empty = basic answers only; in Azure `Assistant__AnthropicApiKey` from Key Vault |
| `Assistant:Model` | (existing) default `claude-sonnet-5`, used by both assistants |
| `Assistant:FarmerMaxTokens` | farmer answer length, default 1500 (Indian scripts take more tokens) |
| `Assistant:Endpoint` | optional Messages API URL (a gateway / proxy, or the local stub); empty = `https://api.anthropic.com/v1/messages`. The key is sent there |
| `FarmerPortal:AssistantDailyLimit` | questions per farmer per IST day; empty = 30 |

### Tables (migration `V37_FarmerAssistant`)

New tables instead of a discriminator on `LibraryConversations` / `LibraryMessages`: those store local `DateTime.Now`
times and int ids, and every query of the live library controller (list, detail, delete, ask) would have needed a
scope filter.

- `FarmerChatConversations` (Id bigint, FarmerUserId, Title varchar(80), CreatedAtUtc, UpdatedAtUtc, IsDeleted); index
  (FarmerUserId, UpdatedAtUtc).
- `FarmerChatMessages` (Id bigint, ConversationId -> conversation with cascade, FarmerUserId, Role varchar(16), Text,
  SourcesJson, SuggestionsJson, Provider varchar(20), Language varchar(8), CreatedAtUtc); indexes (ConversationId, Id)
  and (FarmerUserId, Role, CreatedAtUtc) for the cap.

### Tests and tools

- Harness: `FarmerPortalAssistantTests` (the IST day boundary at 18:29:59 / 18:30:00 UTC, reset, counts-today, limits,
  cap message, validation 1000 / 1001, titles, the system prompt per language and its safety rules, the context block
  and that the facts type has no personal fields, soil rating words, the Anthropic request (system, history, context
  first, no thinking / sampling fields, image block shape), response parsing (thinking skipped, refusal), Tamil
  suggestions, the history window, the labelled basic answer incl. duplicates / pesticide / Tamil, the keyword
  provider in farmer mode, starters, the screen helpers, tile / More sheet / routes); updated tile and More-sheet
  expectations in `FarmerPortalUiTests` / `FarmerPortalRouteTests`.
- Smoke: `python tools/farmer-portal-check/smoke_task5.py [--phone ...] [--cap-phone ...]` (local API without a key:
  401 / 403, validation, basic answers, sources, another farmer's 404s, delete, the cap incl. 8 parallel asks) and
  `--mode stub --record <file>` against `tools/farmer-portal-check/fake_anthropic.py`, a local stand-in for the
  Messages API (the Anthropic path, the request body, history, failure and refusal fallback). The stub never calls
  Anthropic.
- Screenshots: `python tools/farmer-portal-check/shoot_task5.py --out ... --set basic|stub`.
- `tools/farmer-portal-check/cleanup_task5_local.sql` removes the local test farmers' chats (resets their allowance).

### Left / for the owner

- Prod migration `V37_FarmerAssistant` (owner runs prod).
- Set `Assistant__AnthropicApiKey` for full answers in the farmer's language; until then farmers get labelled basic
  English answers from the library. Cost ceiling: 30 questions x 1500 output tokens per farmer per day.
- The library has few published items today; answers (the basic ones especially) are only as good as the library.
- An agronomist should read a sample of real answers (Tamil especially) before go-live.
