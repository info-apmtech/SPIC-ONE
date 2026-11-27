# Farmer Portal: design

Date: 2026-11-27. Branch `farmer-portal` (from azure-deploy 5c53e256). Owner asked on 2026-11-27 night and
handed over the how ("use sub agent and implement all one by one, as accurate as possible"). Every
decision below marked **Call** was made by Claude, not agreed with the owner, and is listed for
review in the morning notes.

## Intent

Any farmer who downloads SPIC ONE (Play Store / App Store / web) can sign in with only a mobile
number, fill a short profile once, and then get, in one farmer home:

1. what is trending in their area, and the SPIC community near them
2. SPIC dealers near them (locator)
3. their SPIC field team (JMDO, MDO, RM): who they are and a way to connect
4. fertilizer recommendation from crop, soil type, area and soil test values (N, P, K, pH)
5. a leaf-photo check that spots nutrient deficiency early (leaf colour / chlorosis)
6. a chatbot (SPIC AI) for farming questions
7. a harvest yield forecast for harvest and market planning

Built one feature at a time, reusing what SPIC ONE already has (Farmer role, SAS soil reports,
lab nutrient ratings and fertilizer schedules, Community, Digital Library + SPIC AI, dealer
GPS, location masters).

## Goals

- Phone-number OTP sign-in that works for strangers (open sign-up), safe against SMS abuse.
- Farmer-first UI: phone layout first, big touch targets, plain words, works on web too.
- Advice must be agronomically sound and consistent with what the SAS soil report already says.
- Every AI answer is honest about what it is (estimate / AI suggestion, consult your MDO).

## Non-goals (this round)

- Translating the portal UI chrome (English UI; AI answers follow the farmer's chosen language).
- Real satellite NDVI (needs a Sentinel Hub / Earth Engine account): the farmer sets canopy vigour.
- Market prices, e-commerce, payments.
- Admin screens for editing the agronomy tables (seeded data; admin edit later).
- Prod migration or deploy (owner runs prod; see docs/deployment-guide.md and memory rules).

## Anti-goals

- No live data created during tests: only the local `spicone_dev` database.
- No staff personal data handed to anonymous users by default.
- No breaking the existing username/password login or any staff page.
- No silent "fake" AI: when the AI key is missing, say the answer is a rule-based fallback.

## Constraints found in the code

- `AppRole.Farmer` exists; farmer pages resolve from Designation + OpenAccessPages
  (`SPIC.Core/Entities/PageAuthorization.cs`). Self-signed-up farmers have no designation.
- Login today: `api/Authentication/login` (username + password), JWT + rotating refresh token.
- No SMS sender exists in the API.
- SAS: `SasFarmer` (Mobile unique, `UserId` link), My Samples / My Reports farmer pages, lab
  parameter ranges (`LabParameter` Low/Normal/High), `LabCropRecommendation` + dose factors.
- SPIC AI: `IAssistantProvider` (Anthropic Messages API, keyword fallback) used by
  `LibraryAssistantController`.
- Dealers: `DealerRegistration` has Latitude/Longitude, StateId, Region, DistrictId, status.
- Location master: Zone > State > Region > Headquarter, State > District. Headquarter is NOT
  linked to District. Employees carry Region/Headquarter ids (`EmployeeRegistration`).
- Leaflet is already in the web assets; SkiaSharp is available through PDFtoImage.

## Approaches considered (for the whole portal)

1. **Inside the existing SPIC ONE app (chosen).** New `/farmer/*` pages in the shared MAUI/Blazor
   project, new API controllers, same JWT. Reuses shell, components, SAS reports, AI provider,
   one store listing. Cost: farmer and staff share one app binary and menu logic.
2. Separate farmer app. Clean branding, but a second store listing, duplicated auth and
   components, and the SAS farmer pages already live in this app. Rejected.
3. WhatsApp / SMS bot. Reaches feature phones but cannot do locator maps, photos UI, or the
   slider tools well. Possible later front end to the same APIs.

## Global design calls

- **Call: access model.** Farmer portal routes are gated by role (`AppRole.Farmer`), not by
  designation, so open sign-up works without seeding designation data. Admin / SuperAdmin can
  open them for support. Staff never see the farmer menu.
- **Call: OTP.** 6-digit code, SHA-256 hashed with a server pepper, 5 minute expiry, 5 wrong
  tries locks that code, 30 s resend cooldown, 5 sends per number per hour, 20 per IP per hour,
  Indian mobiles only (10 digits starting 6-9, `+91` / `0` stripped).
- **Call: SMS provider.** `ISmsSender` with `Log` (development: code written to the log and,
  only in Development environment, returned as `devCode`) and `Msg91` (OTP template API).
  `Sms:Provider` defaults to `Log`; in Production with `Log`, the OTP request returns 503
  "SMS is not set up yet" instead of pretending.
- **Call: account.** First verified OTP creates an Identity user, `Role = Farmer`,
  `UserName = "f" + 10-digit mobile`, `PhoneNumberConfirmed = true`, random unusable password.
  A mobile that already belongs to a non-farmer user is refused (staff use the normal login).
- **Call: SAS link.** After OTP verification, an unlinked `SasFarmer` row with the same mobile
  is linked to the new user (OTP proves the phone), so soil reports show up.
- **Call: profile.** Name required; email optional (many farmers have none); state, district,
  village, pincode required; GPS optional; crops, land (acres), irrigation, language optional.
- **Call: staff contacts.** The farmer sees name, role, headquarter of their JMDO/MDO/RM and a
  "Request a call back" button that creates a request the staff member sees on a new staff page.
  Staff phone numbers are shown only when `FarmerPortal:ShowStaffPhoneNumbers = true`
  (default false). Owner to confirm.
- **Call: area mapping.** Farmer district -> active dealers in that district -> their Region and
  Headquarter -> employees with those ids (JMDO/MDO by headquarter, RM by region). Fallback:
  state-level RM list. Nearest dealer (GPS) used when the district has no dealer.
- **Call: AI cost control.** Per-farmer daily caps: 30 chatbot questions, 10 leaf checks.
- **Call: honesty labels.** Fertilizer and yield screens say "estimate / general guidance";
  leaf check says "AI suggestion, confirm with a soil test or your MDO".
- **Call: weather on trends.** Open-Meteo (free, no key) with coordinates rounded to 0.1 deg
  (about 11 km), cached 1 hour. No personal data leaves the server.

## Feature designs (detail in the plan)

See docs/superpowers/plans/2026-11-27-farmer-portal.md, Tasks 1-7. Each task is one feature and
one commit series; tables are added by one EF migration per task that needs them, dry-run on the
local database.

## Left to the builder

Exact page layout within the existing native shell components (PageHeader, tokens.css,
BottomSheet, StatCard, EmptyState), naming inside the patterns the codebase already uses, and
which existing helper to reuse where the plan says "reuse".
