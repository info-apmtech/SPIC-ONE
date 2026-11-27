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
