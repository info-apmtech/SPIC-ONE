# Farmer Portal: visual design brief

Owner's words (2026-11-27): "need a very good design, not just like that, it's a corporate project,
so more good attractive design". Every farmer screen is held to this brief. Reviewers check
screenshots against it.

## Feel

Premium agri-corporate: the confidence of a modern banking app, the warmth of fields. Calm,
spacious, clear numbers, one strong action per screen. Never a plain Bootstrap form on a white
page. A farmer with a basic Android phone in sunlight must read it easily.

## Scope rule

All styles live in `SPIC.MauiBlazorApp.Shared/wwwroot/css/farmer-portal.css` (loaded in both hosts
like tokens.css) and component `.razor.css` files, and every selector is scoped under the root
class `.fp` (each farmer page's outer element is `<div class="fp fp-page ...">`). Nothing may
change how existing staff pages look.

## Tokens (CSS custom properties on `.fp`)

| Token | Value | Use |
|---|---|---|
| `--fp-green-900` | `#063d22` | hero gradient end, headings on light |
| `--fp-green-700` | `#0b6b3a` | SPIC primary (matches `--primary`) |
| `--fp-green-500` | `#14935a` | hero gradient start, active states |
| `--fp-green-50` | `#e8f5ee` | tinted backgrounds, icon circles |
| `--fp-gold-500` | `#e8a317` | harvest accent, highlights, "new" |
| `--fp-gold-50` | `#fdf4e1` | gold tint |
| `--fp-soil-600` | `#8a5a2b` | soil / earth accents |
| `--fp-sky-600` | `#1f7ae0` | water / weather |
| `--fp-ink-900` | `#0f1f17` | body text |
| `--fp-ink-600` | `#4b5b52` | secondary text |
| `--fp-line` | `#e3ece6` | card borders, dividers |
| `--fp-bg` | `#f5f8f6` | page background |
| `--fp-low` | `#d64545` | Low / deficient |
| `--fp-medium` | `#1e9e5a` | Medium / optimal |
| `--fp-high` | `#6c4ed9` | High / excess |
| `--fp-warn` | `#e08a00` | caution |
| `--fp-radius` | `16px` (cards), `12px` (inputs), `999px` (chips) | |
| `--fp-shadow` | `0 6px 24px rgba(6, 61, 34, 0.08)` | cards |
| `--fp-shadow-lift` | `0 10px 32px rgba(6, 61, 34, 0.14)` | hover / pressed tiles |

Type: the app font stack; headings 700, page title 24/30 phone (28/34 desktop), section 18/24,
body 15/22, caption 13/18; numbers use `font-variant-numeric: tabular-nums`; big result numbers
40-48 px 800 weight. Icons: Bootstrap Icons (`bi-*`) already in the app.

## Building blocks (shared Razor components under `Components/FarmerPortal/`, prefix `Fp`)

- **FpHero**: page top band, green gradient (`--fp-green-500` to `--fp-green-900`, 135 deg) with a
  subtle inline-SVG field-rows / leaf pattern at 8-10 % opacity, title, subtitle, optional back
  button and slot for a chip row or weather strip. Rounded bottom corners 24 px on phone.
- **FpCard**: white, `--fp-radius`, `--fp-shadow`, 1 px `--fp-line` border, 16-20 px padding,
  optional header with tinted icon circle.
- **FpTile**: home feature tile: 44 px tinted icon circle (each feature its own hue), title,
  one-line description, chevron; 2 columns phone, 3 tablet, 4 desktop; press/hover lift.
- **FpChip / FpStatus**: pill chips; nutrient status chip coloured Low / Medium / High.
- **FpZoneSlider**: range input with a coloured zone track (low red / medium green / high violet
  bands drawn from the real thresholds), value bubble, min/max labels and live status chip, and
  a number box for exact entry. Thumb 28 px.
- **FpStat / FpBigNumber**: big result number with unit and caption; range bar (low-expected-high);
  confidence ring or bar.
- **FpSteps**: step indicator for multi-step flows (login, onboarding, fertilizer).
- **FpEmpty / FpError**: friendly illustration (simple inline SVG: sprout, field, phone), title,
  one line, action button.
- **FpSkeleton**: shimmer placeholders matching the card layout (use existing Skeleton if it fits).
- **FpDisclaimer**: small info strip with `bi-info-circle`, used on every estimate / AI result.
- Primary button: full width on phone, 52 px tall, `--fp-green-700`, 12 px radius, 600 weight;
  secondary: outlined green; danger never used for farmers.

## Screen patterns

- **Login (`/farmer/login`)**: desktop/tablet split screen: left brand panel (green gradient,
  SPIC ONE wordmark, illustration, three value points: soil advice, dealers near you, expert
  help), right a centred card. Phone: compact brand hero on top, card overlapping it. Mobile
  field with fixed "+91" prefix, large digits. OTP: six separate boxes, auto-advance, paste fills
  all, backspace moves back, `autocomplete="one-time-code"` and `inputmode="numeric"`; resend
  countdown; clear inline errors in plain words.
- **Onboarding**: FpSteps (About you, Your farm, Your village), one group per step on phone, crop
  chips with icons, "Use my location" button with a status line, progress saved per step only on
  final submit.
- **Home**: FpHero with "Good morning, <name>" (time-of-day greeting in IST), village/district
  chip, weather strip (Task 3); a highlight card (e.g. last soil report or "Get fertilizer
  advice"); then FpTile grid; then "Your SPIC team" preview row.
- **Tools (fertilizer, yield, leaf check)**: input card(s) first, sticky primary action at the
  bottom on phone, result appears below with a smooth scroll; result has a summary band (big
  number / status), then details cards, then actions (save/share/ask my MDO), then FpDisclaimer.
- **Lists (dealers, team, requests, history)**: cards with avatar/initials or icon, key facts,
  action buttons as icon+label pills (Call, WhatsApp, Directions, Request call back).
- **Chat**: bubbles (farmer right green tint, SPIC AI left white with small SPIC leaf avatar),
  starter suggestion chips, sources as small link chips, typing indicator, input bar fixed at
  bottom with send button.

## Motion and quality bar

- 150-220 ms ease-out transitions; cards fade-up 8 px on first render; respect
  `prefers-reduced-motion`.
- Every screen has loading (skeleton), empty, and error states designed, not default text.
- WCAG AA contrast, 44 px tap targets, visible focus ring, labels on every input.
- No horizontal overflow at 360 px; looks intentional at 375, 768, 1280 and 1920.

## Verification

Each task captures screenshots of its screens at 375x812 and 1280x800 from the local web host
(signed in as a local test farmer via devCode against the local API) and saves them under
`.superpowers/sdd/2026-11-27-farmer-portal/shots/task-<N>/`. Playwright with the installed Edge
works (see how `tools/help-demo` / help-tools shoot runs it).
