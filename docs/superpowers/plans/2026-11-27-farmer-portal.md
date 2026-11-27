# Farmer Portal implementation plan

Spec: docs/superpowers/specs/2026-11-27-farmer-portal-design.md (read its "Global design calls").
Worktree: `D:\GIT_C\GIT\SPIC-ONE\.claude\worktrees\farmer-portal`, branch `farmer-portal`.
Tasks run strictly one after another; each later task may build on earlier ones.

## Global Constraints

- Work and commit ONLY in the worktree above, on branch `farmer-portal`. Never push, never touch
  other branches, never run anything against production (no `migrate.ps1`, no `deploy.ps1`,
  no `az` writes). Never use the connection string in `SpicAPI/appsettings.json` (it is a LIVE
  server). For any database work export
  `ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234`
  first. If the local PostgreSQL service is stopped, start it with
  `Start-Process "D:\Program Files\PostgreSQL\17\bin\pg_ctl.exe" -ArgumentList 'start','-D','"D:\Program Files\PostgreSQL\17\data"'`
  (never pipe pg_ctl). `dotnet ef ... --context AppDbContext` is required (two DbContexts).
- Migrations: one EF migration per task that adds tables, named `V34_<Name>`, `V35_<Name>`, ...
  in task order (check the highest V number in `Spic.Infrastructure/Migrations` first). Before
  committing: `dotnet ef migrations list` and `dotnet ef database update` on the local DB must
  succeed, and `dotnet ef migrations has-pending-model-changes` must report none. Never edit or
  delete existing migrations.
- Never create test data through the live API. Local API: `dotnet run --project SpicAPI --urls http://localhost:5120`
  with the local connection string. Local web host: run with `--ApiBaseUrl=http://localhost:5120/`
  (the `.claude/launch.json` entry points at the LIVE API: do not use it signed in).
- Build gate for every task: `dotnet build SpicAPI` and `dotnet build SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web`
  succeed with no new errors. When searching for csproj files exclude `.claude\worktrees\` copies
  other than this one.
- Tests: add checks for every piece of pure logic (validation, calculators, rules) to
  `SPIC.RegressionHarness` following its existing check style, and run
  `dotnet run --project SPIC.RegressionHarness`. Baseline has 9 known failures (SalesAudit /
  ExtensionRequests / FarmDashboard open-access routes and "GpsSettings is the last
  PagePermission member"); no NEW failures allowed. If adding a PagePermission member makes the
  "last member" check change, update that check honestly. Smoke-test new endpoints against the
  local API with curl or a small script kept under `tools/farmer-portal-check/`.
- Access: farmer portal pages and APIs are gated by `AppRole.Farmer` (plus Admin / SuperAdmin
  for support). Route rules go in `SPIC.MauiBlazorApp.Shared/Services/RouteAccess.cs`, not in
  PageGuard.razor. API controllers use `[Authorize]` plus a Farmer role check; every farmer
  query is scoped to the caller's user id. Staff pages keep their existing rules.
- UI: phone first (<= 767 px), tablet 768-1199, desktop >= 1200 must not break other pages.
  Use the existing shell components (PageHeader, tokens.css, BottomSheet, StatCard, EmptyState,
  Skeleton, ToastService, ConfirmDialog) and follow an existing page's CSS isolation pattern.
  Plain words for farmers. Nothing in `OnAfterRenderAsync` may throw; re-check fields after
  every await.
- Dates: the prod container runs on UTC while phones send "+05:30". Store UTC
  (`DateTime.UtcNow`) for new timestamps and never call `.Date` on a client-sent DateTime
  without converting to IST first.
- Secrets: no keys in code or appsettings.json; read from configuration (`Sms:*`,
  `Assistant:*`, `FarmerPortal:*`). Placeholders with empty values in appsettings are fine.
- Personal data: never put phone numbers or names in URLs or logs (mask phone in logs as
  `98xxxxxx21`). External calls (Open-Meteo, Msg91, Anthropic) receive only what they need.
- Honesty: estimates and AI output carry a short disclaimer; when the AI key is missing say the
  answer is a rule-based fallback.
- Commits: small, message style like the repo history, each ending with
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Write a short `docs/farmer-portal.md` section for your feature (what it does, endpoints,
  config keys, tables, what is left), appending to the file other tasks create.

---

## Task 1: Phone OTP login and first-time farmer profile

Goal: any farmer can sign in with a mobile number and OTP; the first time they fill a profile;
then they land on `/farmer` (a minimal placeholder home in this task; Task 2 builds it out).

API (new `FarmerAuthController`, route `api/FarmerAuth`, `[AllowAnonymous]`):
- `POST otp/request` body `{ phone }` -> normalise (strip spaces, `+91`, leading `0`), require
  `^[6-9]\d{9}$`. Rate limits: 30 s cooldown per number (429 with `retryAfterSeconds`), max 5
  sends per number per rolling hour, max 20 per client IP per hour. Create a challenge row:
  table `FarmerOtpChallenges` (Id, Phone, CodeHash, CreatedAtUtc, ExpiresAtUtc = +5 min,
  Attempts, ConsumedAtUtc, RequestIp). Code = 6 digits from `RandomNumberGenerator`.
  CodeHash = SHA-256 of `code + ":" + phone + ":" + pepper` (pepper from `FarmerPortal:OtpPepper`,
  falling back to the JWT signing key when empty). Send through `ISmsSender`. Response
  `{ success, expiresInSeconds: 300, resendInSeconds: 30, devCode? }` where `devCode` is present
  ONLY when `IHostEnvironment.IsDevelopment()` and provider is `Log`.
  If the number already belongs to a non-Farmer user: do NOT send; return 409 with message
  "This number is registered to a SPIC staff or dealer account. Please use the normal login."
- `POST otp/verify` body `{ phone, code }` -> latest unconsumed, unexpired challenge for the
  phone; wrong code increments Attempts; at 5 attempts the challenge is dead ("Too many tries,
  request a new code"). Use a constant-time compare. On success mark consumed, then find or
  create the Identity user (`Role = AppRole.Farmer`, `UserName = "f" + phone`,
  `PhoneNumber = phone`, `PhoneNumberConfirmed = true`, `IsActive = true`, random 32-byte
  password nobody knows). Link an unlinked `SasFarmer` with the same Mobile (set UserId). Issue
  the same JWT + refresh token pair the normal login issues: move the token creation out of
  `AuthenticationController` into a reusable service (e.g. `Spic.Infrastructure` or `SpicAPI/Services`
  `ITokenIssuer`) used by both controllers, without changing the normal login's response shape.
  Response = the normal `LoginResponseModel` plus `profileComplete` (bool) and `isNewUser`.
  Inactive user -> 403.

SMS (`ISmsSender` in SPIC.Core/Interfaces, implementations in Spic.Infrastructure):
- `LogSmsSender`: logs "OTP for 98xxxxxx21: 123456" at Information (development only use).
- `Msg91SmsSender`: MSG91 OTP/flow API over HttpClient using `Sms:Msg91:AuthKey`,
  `Sms:Msg91:TemplateId`, `Sms:Msg91:SenderId`; 10 s timeout; logs failures without the code.
- `Sms:Provider` = `Log` | `Msg91` (default `Log`). In Production with `Log`, `otp/request`
  returns 503 `{ message: "SMS is not set up yet. Please try later." }`.
- Add empty placeholders for these keys in `SpicAPI/appsettings.json` (no values).

Profile (new `FarmerProfileController`, route `api/Farmer/profile`, Farmer role only):
- Table `FarmerProfiles` (Id, UserId unique, Name, Email?, Phone, StateId, DistrictId,
  Village, Taluk?, PinCode, Latitude?, Longitude?, PrimaryCrops (text, comma separated),
  LandAcres (decimal?), IrrigationType (enum: Rainfed, Canal, Borewell, Drip, Sprinkler,
  Mixed)?, PreferredLanguage ("en","ta","te","hi","mr","kn","ml"; default "en"), CreatedAtUtc,
  UpdatedAtUtc).
- `GET` -> profile or 404 with `{ profileComplete: false }`. `PUT` validates: Name 2-100,
  Email valid if given, StateId and DistrictId exist and district belongs to state, Village
  required, PinCode `^\d{6}$`, LandAcres 0-10000, lat/long ranges. Also copies Name/Email to
  the Identity user. Profile is complete when Name, State, District, Village, PinCode are set.
- Reuse the existing State/District lookup endpoints for dropdowns (find them in
  LocationControllers.cs) and `api/PincodeLookup/{pincode}` to prefill district/state if it helps.

App (SPIC.MauiBlazorApp.Shared):
- Login page: add a clear "Farmer? Sign in with mobile number" entry (link/button) that opens
  `/farmer/login` - do not change the existing username/password form behaviour.
- `/farmer/login`: step 1 mobile number (+91 prefix shown, numeric keyboard), step 2 6-digit
  code with resend countdown, errors shown inline in plain words. On success store the session
  exactly like the normal login does (reuse LoginState / session store code paths), then go to
  `/farmer/onboarding` when `profileComplete == false`, else `/farmer`.
- `/farmer/onboarding`: one short form (name, email optional, state, district, village, taluk,
  pincode, "use my location" button for GPS, crops multi-select or chips, land acres,
  irrigation, language). Save -> `/farmer`.
- `/farmer`: placeholder home "Welcome, <name>" with profile edit link (Task 2 replaces it).
- `/farmer/profile`: view/edit the same form.
- Farmer role: after any login a Farmer lands on `/farmer` (or onboarding when incomplete),
  never on the staff dashboard. Shell/bottom tabs for Farmer: Home (`/farmer`), Profile, and
  keep the existing SAS "My Samples"/"My Reports" entries when the farmer has those
  permissions or a linked SasFarmer. RouteAccess: `/farmer/login` anonymous, other `/farmer/*`
  Farmer (+Admin/SuperAdmin). Staff roles hitting `/farmer/*` are redirected to their home.

Tests: harness checks for phone normalisation/validation, OTP hash + compare, attempt/expiry
rules, cooldown/hour-limit decisions (make the rule logic a pure class so it can be tested
without a DB), profile validation. Smoke test request->verify->profile on the local API using
`devCode` (Development environment).

---

## Task 2: Farmer home, dealer locator, and "My SPIC team" contacts

Goal: `/farmer` becomes the farmer home; farmers find dealers near them and connect with their
JMDO / MDO / RM.

Home `/farmer`:
- Greeting with name and village/district, weather strip slot (filled by Task 3), and tiles:
  Dealers near me, My SPIC team, My soil reports (only if linked SasFarmer / permissions),
  plus a tile registry so Tasks 3-7 each add their tile (Trends & community, Fertilizer advice,
  Leaf check, Ask SPIC AI, Yield forecast). Do not show tiles for features that do not exist yet.

Dealer locator (`api/Farmer/dealers`, Farmer role):
- Query: `lat`, `lng` (optional), `radiusKm` (default 25, max 100), `districtId` (optional,
  defaults to the profile district), `q` (optional name/village search), paging.
- Only active, finally approved dealers (inspect `DealerStatus` and the register page code for
  which statuses mean active; exclude to-be-terminated / terminated / not traceable). Return only
  public business info: firm name, address lines, village, district, pincode, official contact
  number, WhatsApp number, Google map URL, lat/lng, distance km (haversine). Never return PAN,
  bank, GST, owner personal fields.
- With GPS: dealers within radius sorted by distance; when none, fall back to the farmer's
  district; when none, the state. Say which fallback applied in the response (`scope`).
- UI `/farmer/dealers`: "Use my location" (device GPS through the existing location service /
  JS geolocation pattern in the app), list cards with distance, Call (`tel:`), WhatsApp
  (`https://wa.me/91...`), Directions (Google Maps URL or `geo:`). Optional map view with the
  Leaflet setup already used in the app (find how ContactUs/Logistics load it). Empty state
  when nothing found.

My SPIC team (`api/Farmer/contacts`, Farmer role):
- Resolve: farmer district -> active dealers in that district -> their Region ids and
  Headquarter ids (inspect DealerRegistration for the HQ field) -> active employee logins with
  role JMDO / MDO in those headquarters and RM / RMD in those regions (inspect
  EmployeeRegistration / Employeelogins and how TeamController resolves teams). When the district
  has no dealer, use the nearest dealer by GPS; final fallback the state's RMs. Return per
  person: display name, role label ("Junior Market Development Officer" etc.), headquarter /
  region name, and phone ONLY when `FarmerPortal:ShowStaffPhoneNumbers` is true (default false).
  Return an opaque `contactId` (employee user id is fine since only staff ids, never other
  farmers).
- Call-back requests: table `FarmerContactRequests` (Id, FarmerUserId, StaffUserId, Topic
  (enum: Fertilizer, Crop problem, Soil test, Product, Dealer, Other), Message (max 500),
  PreferredTime?, Status (Open, Contacted, Closed), StaffNote?, CreatedAtUtc, UpdatedAtUtc).
  `POST api/Farmer/contacts/{contactId}/request` (Farmer; max 3 open requests per staff member
  per farmer; staff must be one of the farmer's resolved contacts). `GET api/Farmer/contacts/requests`
  (farmer's own). Staff side: `GET api/FarmerRequests` (requests addressed to the caller),
  `PUT api/FarmerRequests/{id}` (status + note, only by the addressed staff member; Admin can view all).
  Staff see the farmer's name, phone, village, district (the farmer asked to be called).
- UI: `/farmer/team` cards with role, HQ, "Request a call back" (BottomSheet with topic, message,
  preferred time) and Call/WhatsApp buttons when phone is returned; "My requests" list with
  status. Staff page `/farmer-requests` (title "Farmer Call-back Requests") for JMDO / MDO / RM /
  RMD / Admin: list, filter by status, mark Contacted/Closed with a note, `tel:` link. Add a menu
  entry for those staff roles following NavMenu / ShellNavigation patterns (add a PagePermission
  only if the existing pattern requires one; if so, Admin bypass must work).

Tests: harness checks for haversine distance, radius/fallback selection (pure function), contact
resolution rules with in-memory inputs, request validation. Local smoke test with seeded local
dealers/employees only.

---

## Task 3: Community near me and trends in my area

Goal: farmers see what is happening around them and join the community discussions near them.

Community near me:
- Inspect `CommunityController` / `Community.cs`: how location is stored on discussions and how
  "near" could work. Add a "Near me" filter that uses the farmer's district (fallback state) for
  farmer authors (FarmerProfile) and the existing headquarter/state mapping for staff authors.
  Make sure farmers can open the existing Community pages (OpenAccessPages) and that posts by
  OTP farmers show the farmer's profile name and village/district. If Community needs a
  location column on discussions, add it in this task's migration and set it on create from
  the author's profile (existing rows stay null and count as "not near").
- Farmer entry `/farmer/community` (or link into existing community pages with the near-me
  filter preselected) from the home tile.

Trends in my area (`api/Farmer/trends`, Farmer role, cached 1 h per district):
- Weather: 7-day forecast from Open-Meteo `https://api.open-meteo.com/v1/forecast`
  (daily max/min temp, precipitation sum, precipitation probability, wind) using the profile
  GPS or the district's approximate centre (if no GPS: skip weather rather than guessing),
  coordinates rounded to 0.1 deg, 8 s timeout, failure = section omitted. Add simple farm
  advisories from the forecast (e.g. heavy rain > 50 mm in 3 days: "avoid fertilizer
  top-dressing and spraying before the rain").
- Area numbers from SPIC ONE data in the farmer's district (fallback state), last 90 days:
  top crops among farmer profiles; most common soil deficiencies from SAS lab reports
  (share of samples Low in N / P / K / Zn etc., using the lab's own status results; only
  aggregates, minimum 5 samples else hide); hottest community discussions near the area (top 5
  by recent replies/views); count of SPIC dealers in the district. No individual farmer's
  data is ever shown.
- UI `/farmer/trends` cards (weather strip, "What farmers near you grow", "Soil problems near
  you", "Talked about near you", dealers count), and the weather strip on `/farmer` home.

Tests: harness checks for the advisory rules and the minimum-sample privacy rule.

---

## Task 4: Fertilizer recommendation (crop, soil type, area, N/P/K/pH)

Goal: crop-wise fertilizer, water and soil management roadmap from crop + soil type + area +
soil test values, consistent with the SAS soil report.

Inputs (screen matches the owner's sketch):
- Crop (e.g. "Cotton (Irrigated & Rainfed)", Paddy, Maize, Sugarcane, Banana, Groundnut,
  Black gram, Green gram, Chilli, Tomato, Onion, Turmeric, Coconut, Wheat, Soybean...),
  irrigated / rainfed where the crop differs, soil type (Black Cotton Soil, Red Soil, Alluvial
  Soil, Laterite Soil, Sandy / Sandy Loam, Clay Loam, Coastal Saline), area in acres.
- Sliders with live status chips: Available Nitrogen kg/ha (Low < 280, Medium 280-560,
  High > 560; slider 0-800, default 100), Available Phosphorus kg/ha (Low < 11, Medium 11-22,
  High > 22; slider 0-60, default 5), Available Potassium kg/ha (Low < 118, Medium 118-280,
  High > 280; slider 0-500, default 50), Soil pH (4.5 acidic - 9.5 alkaline, step 0.1,
  default 4.5 per the sketch is odd; use 7.0 default unless prefilled).
- "Use my soil report" button: prefill N/P/K/pH from the farmer's latest SAS soil report when
  linked (convert units if the report stores kg/acre or ppm; check LabParameter units).
- Use the lab parameter master's ranges (LabParameter RangeMin / RangeMax / ModerateFrom for N,
  P, K, pH) when present so the ratings match the soil report; the numbers above are the default.

Calculation (pure service, e.g. `FertilizerAdvisor` in SPIC.Core or Infrastructure):
- Crop norms table `FertilizerCropNorms` seeded in the migration: crop, variant (Irrigated /
  Rainfed / Hybrid), recommended N, P2O5, K2O kg/ha, split schedule (basal % and top dressings
  with days after sowing/planting), duration days, notes. Use Tamil Nadu Agricultural
  University (TNAU Agritech portal) / ICAR general blanket recommendations; put the source in a
  `Source` column. Examples to check against the source: Paddy (medium duration) 150:50:50,
  Cotton irrigated varieties 80:40:40 / hybrids 120:60:60, rainfed cotton 40:20:40, Maize hybrid
  250:75:75, Sugarcane 275:62.5:112.5, Groundnut rainfed 10:10:45, Banana per plant schedule.
- Soil test adjustment (rating approach used by TNAU soil-test-based recommendations): Low ->
  +25 %, Medium -> as recommended, High -> -25 %, per nutrient. If the lab master has dose
  factors (LabFertilizerSchedule / Nutrient on LabCropRecommendation), use those same factors so
  advice matches the soil report.
- pH: < 5.5 lime (dose by soil type, e.g. 1-2 t/acre agricultural lime, split), 5.5-6.5 mild
  liming / organic matter, 6.5-7.5 none, 7.5-8.5 organic matter + gypsum advice, > 8.5 gypsum
  per gypsum requirement guidance; plus micronutrient hints (Zn on alkaline/black soil, etc.).
- Products: convert nutrient kg to product bags for the area: Urea 46 % N, DAP 18-46-0 (account
  for DAP N), MOP 60 % K2O, SSP 16 % P2O5, plus SPIC complexes if the product library / LabCropRecommendation
  lists them (inspect FarmProduct / product master for SPIC fertilizer names and grades; never
  invent a product grade). Show kg and 45 kg / 50 kg bags per the product, per split stage.
- Soil type roadmap: water management (irrigation interval guidance by soil type and crop
  stage, drainage for black/clay, mulching for sandy), soil management (organic manure / FYM
  t/acre, green manure, gypsum for black soil cotton, etc.). Store as seeded tips table
  `CropSoilTips` (crop?, soil type, category Water/Soil/Nutrient, text, source).
- Output API `POST api/Farmer/fertilizer/recommend` (Farmer) and `GET api/Farmer/fertilizer/options`
  (crops, variants, soil types, ranges). Save each run to `FertilizerRecommendationRuns`
  (farmer, inputs JSON, result JSON, CreatedAtUtc) and `GET .../history` (latest 20).

UI `/farmer/fertilizer`: two steps like the sketch ("Soil test values" sliders card with Low /
High chips, then "Crop-Wise Recommendations" with crop and soil-type selects), result: status
per nutrient, nutrient need kg/acre, product schedule table per stage, pH amendment, water and
soil management roadmap, disclaimer "General guidance from TNAU/ICAR norms. For exact advice
take a SPIC soil test or ask your MDO." with a link to My SPIC team.

Tests: harness checks for ratings at boundaries (279/280/560/561, 10.9/11/22/22.1,
117/118/280/281), adjustment math, product conversion (e.g. 120 kg N with DAP first), pH bands,
unit conversion.

---

## Task 5: SPIC AI chatbot for farmers

Goal: farmers ask farming questions in their language and get grounded, safe answers.

- Reuse `IAssistantProvider` / Anthropic provider and the Digital Library retriever. Add a
  farmer mode: system prompt for an agronomy helper for Indian farmers (simple words, answer
  in the farmer's PreferredLanguage, short steps, metric units, recommend SPIC products only
  from retrieved library/product content, never invent prices or doses beyond label/norms, for
  pesticide questions remind to follow the label and safety, suggest contacting their MDO
  (My SPIC team) for field visits). Pass farmer context: crops, district, latest soil report
  summary (ratings only) and latest fertilizer run summary when present.
- Conversations: reuse the library assistant conversation tables with a scope/kind
  discriminator if they fit (add a column in this task's migration), otherwise new
  `FarmerChat*` tables; farmer sees only their own. API under `api/Farmer/assistant`
  (conversations list/detail/delete, ask) mirroring LibraryAssistantController.
- Cost/abuse: max 30 questions per farmer per IST day (429 with friendly message), max 1000
  chars per question, history last 10 turns.
- Keyword fallback when no API key: answer from library matches and label it clearly.
- UI `/farmer/assistant`: chat screen (reuse the Digital Library chatbot UI components if
  present), suggested starter questions per crop, sources shown as links, disclaimer line.

Tests: harness checks for the daily cap (IST day boundary from a UTC clock), prompt builder
includes language and context, fallback labelling.

---

## Task 6: Leaf photo deficiency diagnosis (computer vision canopy diagnostics)

Goal: early detection of nutrient gaps from a leaf photo using leaf colour and chlorosis
analysis.

- UI `/farmer/leaf-check`: pick crop, take photo or choose from gallery (`<InputFile accept="image/*" capture="environment">`
  works on Android WebView and web), preview, tips for a good photo (single leaf, daylight, plain
  background), submit; result card; history list.
- API `POST api/Farmer/leaf-check` (multipart, max 8 MB, jpeg/png/webp; verify magic bytes),
  Farmer role, cap 10 per IST day. Server steps:
  1. Decode with SkiaSharp (already available via PDFtoImage; add an explicit package reference
     to the version already resolved if needed), auto-orient, resize to max 1024 px, strip
     metadata (re-encode JPEG), store under the existing Uploads root
     `Uploads/FarmerLeaf/{userId}/{guid}.jpg` (follow CommunityController's upload root pattern).
  2. Colour metrics on leaf pixels (exclude background: keep pixels with green-ish hue or
     saturation above a threshold): mean hue, green ratio G/(R+G+B), share of yellow pixels
     (hue 40-70 deg), share of brown/necrotic pixels, a Leaf Colour Chart style score 1-6
     (IRRI LCC idea: darker green = higher), uniformity (interveinal vs whole-leaf yellowing
     hint from spatial variance). Pure, testable functions.
  3. Vision diagnosis through Anthropic Messages API with the image (base64) + crop + metrics,
     asking for strict JSON: `{ condition: "healthy"|"nutrient_deficiency"|"disease"|"pest"|"unclear",
     nutrients: [ { code: "N"|"P"|"K"|"Mg"|"Zn"|"Fe"|"S"|"B"|"Ca"|"Mn", likelihood: 0-1 } ],
     symptoms: [..], leafAge: "older"|"younger"|"all"|"unknown", confidence: 0-1, advice: [..],
     needsSoilTest: bool, notALeaf: bool }`. Validate/parse defensively; on parse failure or no
     key, fall back to rules from the metrics (e.g. uniform pale yellow on older leaves -> N
     likely; interveinal yellowing on younger -> Fe/Zn) and label it "basic colour check".
     Extend IAssistantProvider (or add a sibling vision service using the same AssistantOptions)
     rather than duplicating HTTP code.
  4. Corrective advice mapped per nutrient from a seeded table `NutrientCorrectives` (nutrient,
     crop?, product/fertilizer, foliar or soil, dose, note, source TNAU/ICAR), e.g. Zn: zinc
     sulphate 25 kg/ha soil or 0.5 % foliar; Mg: MgSO4 1 % foliar; Fe: FeSO4 0.5 % + citric acid;
     N: urea 1-2 % foliar / top-dress per Task 4 norms.
  5. Save `LeafDiagnoses` (farmer, crop, image path, metrics JSON, result JSON, provider,
     CreatedAtUtc). `GET api/Farmer/leaf-check/history`, `GET .../{id}`, `GET .../{id}/image`
     (owner only), `DELETE .../{id}` (soft).
- Result UI: condition with confidence bar, likely nutrients, what was seen (chlorosis pattern,
  leaf colour score), what to do (correctives), "Get a soil test / ask your MDO" buttons,
  disclaimer "AI suggestion from a photo. Confirm with a soil test or your MDO."

Tests: harness checks for colour metrics on small synthetic bitmaps (all green, half yellow,
brown edges), LCC score mapping, JSON parsing incl. malformed output, rule fallback, file
validation.

---

## Task 7: AI harvest yield prediction

Goal: forecast expected harvest tonnage and a confidence range for harvest and market planning.

Inputs (screen matches the owner's sketch): Crop (Paddy default), Cultivated area (acres,
default 3.0), Irrigation method (Canal / Borewell Irrigated, Rainfed, Drip, Sprinkler),
Satellite canopy vigour NDVI slider 0.35 (Sparse) - 0.92 (Dense), default 0.72, with a helper
"not sure? pick how your crop looks: sparse / medium / dense"; Seasonal rainfall mm slider
400-1400, default 850; Recommended NPK applied % slider 50 %-100 %+ (max 120), default 90 %.
Prefill crop/area/irrigation from the profile and NPK % from the latest fertilizer run.

Model (pure, documented service `YieldEstimator`):
- Seed table `CropYieldNorms` (crop, irrigation class Irrigated/Rainfed, typical yield t/ha
  for good management, typical range, optimal rainfall band mm, crop water need, harvest
  product name and unit, source: Tamil Nadu / India averages from DES / ICAR / TNAU crop
  production guides). Paddy irrigated about 5.5-6 t/ha good management etc.: verify against
  sources and record them.
- Expected yield/ha = norm x f_ndvi x f_rain x f_npk x f_irrigation, each factor a clear
  curve: f_ndvi piecewise-linear (0.35 -> ~0.45, 0.6 -> ~0.85, 0.72-0.85 -> 1.0, > 0.88 slight
  drop for lodging risk); f_rain from the crop's optimal band (irrigated crops are buffered:
  only deficits below the band weigh 30 % as much); f_npk diminishing returns (50 % -> ~0.80,
  75 % -> ~0.93, 100 % -> 1.0, 120 % -> 1.0 with an over-application warning). Tune values so
  results stay within realistic ranges for each crop; document every curve in code comments.
- Total = per-ha x acres x 0.4047; confidence range from input uncertainty (NDVI picked by
  look -> wider), e.g. +/- 12-25 %, plus a confidence label. Return factor breakdown (which
  factor costs most yield) and 2-3 plain tips ("raising NPK to 100 % could add about X t").
- API `POST api/Farmer/yield/predict` + `GET api/Farmer/yield/options` + history
  (`YieldPredictions` table, latest 20).
- UI `/farmer/yield`: inputs card like the sketch, result card with big expected tonnage,
  range bar, confidence, factor bars, tips, disclaimer "Estimate from crop norms and your
  inputs, not a guarantee." Label the page "AI Harvest Yield Prediction" as in the sketch but
  the disclaimer says it is a model estimate.

Tests: harness checks: monotonic in NDVI up to the optimum, monotonic in NPK %, rainfall band
behaviour, totals for the sketch defaults (Paddy, 3 acres, canal, 0.72, 850 mm, 90 %) land
inside the crop's realistic range, range ordering low <= expected <= high.
