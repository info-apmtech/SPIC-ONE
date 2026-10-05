# SPIC IFMS Automation

Downloads the iFMS reports from `dbtfert.nic.in` every morning and imports them
into SPIC ONE, so nobody has to do it by hand.

It runs the same import code as the **Excel Upload** page — `ExcelBulkUploadService`
— so an automated import and a manual one cannot drift apart.

## What happens at 04:05

1. Wait for the portal to answer (it opens around 04:00, not punctually). Polls
   for up to an hour.
2. **For each company in turn** — SPIC, then Greenstar — sign in at
   `https://dbtfert.nic.in/mFMS/loginNew.action` in its own browser session:
   - Reuse yesterday's cookies if the portal still accepts them — no CAPTCHA, no OTP.
   - Otherwise username, password and CAPTCHA. **Five automatic attempts**, each
     one reloading the page for a fresh CAPTCHA.
   - Then the OTP, which this portal asks for on **every** fresh login. The paired
     Android phone forwards the SMS and the login continues in a couple of seconds.
   - If all five CAPTCHA attempts fail, the image is pushed to the phone and the
     run waits for a person. Answering late is fine: if the page has expired by
     then, a fresh CAPTCHA is sent straight away.
3. For each of that company's reports: walk the menu, apply the filters, export,
   download.
4. Import each file. **One report failing never stops the others, and one company
   failing never stops the other.**
5. Email / dashboard / phone / WhatsApp: what worked, what did not, and what to
   do by hand — with the company named on every line.

The two logins run one after another, never at once. That is deliberate: both
OTPs may land on the same handset from the same sender, and the only thing that
tells the codes apart is which login asked for one most recently.

## Before it can run

### 1. The two portal logins

Credentials are **not** in `appsettings.json`. Both companies live in the
`IfmsPortalAccounts` table with the password encrypted, because the portal
expires passwords every 80 days and they have to be changeable without a
redeploy.

Set them from the command line:

```bash
dotnet run -- set-credentials greenstar 1000249825 "<password>" Greenstar
```

`1000249825` is the **Greenstar** login — the portal welcome bar reads
`1000249825 (CDataV ::GFL)`.

```bash
dotnet run -- list-credentials
```

Or from **IFMS Logins** in the portal, which also shows how many days each
password has left and warns inside the last ten.

The `AccountKey` — `spic`, `greenstar` — is what each report job uses to say
which login pulls it.

#### How the encryption works, and why it matters

Passwords are encrypted with ASP.NET Data Protection before they reach the
database, and **the keys are stored in the database too**, in
`DataProtectionKeys`. That is on purpose: SpicAPI and this service run on
different machines and both need to read the same passwords. A shared folder
cannot span two hosts; the shared Postgres already does, and an ordinary
database backup then covers the keys.

Both processes call `SetApplicationName("SPIC.Ifms")`. That string is part of the
key derivation — change it on one side and neither can read what the other
wrote.

### 2. The two shared keys

These authenticate the phone and the automation to SpicAPI. Both default to
empty, which rejects everything.

Generate two random strings:

```bash
openssl rand -base64 32
```

On Windows, or if `openssl` is not to hand:

```bash
pwsh -c "[Convert]::ToBase64String((1..32|%{Get-Random -Max 256}))"
```

Then put them in three places:

| Key | Where it goes | Value |
|---|---|---|
| Device key | SpicAPI `IfmsAutomation:DeviceKey` | the first random string |
| | Phone, **IFMS OTP Relay** screen | the same first string |
| Automation key | SpicAPI `IfmsAutomation:AutomationKey` | the second random string |
| | Here, `Alerts:Push:ApiKey` | the same second string |

In production set them as environment variables rather than in the file:

```bash
IfmsAutomation__DeviceKey=… IfmsAutomation__AutomationKey=… dotnet SpicAPI.dll
```

The device key is what lets the phone relay an SMS with nobody signed in — the
user's JWT lives in the WebView's session storage and is gone by 4am.

### 3. The database

```bash
dotnet ef database update --project Spic.Infrastructure --startup-project SpicAPI
```

Two migrations, adding eight tables in total: `IfmsAutomationRuns`,
`IfmsAutomationReportRuns`, `IfmsOtpMessages`, `IfmsPortalSessions`,
`IfmsChallengeRequests`, `IfmsPortalAccounts`, `IfmsPasswordChanges` and
`DataProtectionKeys`. Nothing existing is touched — checked column by column.

### 4. Chromium

```bash
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

On a bare Linux VPS `--with-deps` also installs the system libraries Chromium
needs. This is a one-off, about 150 MB.

### 5. Tesseract language data

```bash
curl -L -o tessdata/eng.traineddata \
  https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata
```

Without it the OCR logs an error and every CAPTCHA goes to the phone.

### 6. The OCR — already measured

Measured on 25 live CAPTCHAs on 2026-08-30:

| | |
|---|---|
| Read exactly right | 11 |
| Rejected as wrong length | 9 |
| Read wrong | 5 |
| **Per-attempt accuracy** | **44%** |
| **After five attempts** | **~95%** |

So the phone gets asked roughly one morning in eighteen. The rest of the time
nobody touches it.

Two things got it there, and neither was threshold tuning:

- **Colour, not brightness.** The characters are saturated orange and the
  background is a grey gradient. A brightness threshold cannot separate them;
  `max(RGB)-min(RGB)` separates them cleanly. That is `IsolateColouredText`.
- **Undoing the staggered baselines.** The characters sit at deliberately
  different heights and every Tesseract line mode assumes one baseline, so fed
  the whole strip it silently drops the ones that do not fit. The solver cuts the
  glyphs apart, re-seats them on a common baseline, and reads that.

Whole-strip OCR without those scored 1 in 13. With them, 11 in 25.

Remaining errors are font confusions — 8 read as B, 6 as S, O as 0. The retry
loop absorbs them.

To re-measure after any change:

```bash
dotnet run -- test-captcha 25                 # fetch a fresh sample
dotnet run -- test-captcha replay <folder>    # re-read a saved sample
```

Both write an `index.html` showing each original beside what the OCR saw and
what it read. Use `replay` while tuning — it compares like with like instead of
a fresh draw, and does not keep hitting the portal.

Worth knowing: `CharacterWhitelist` is uppercase and digits only. Adding
lowercase back measurably hurts, because it gives every glyph a case-confusable
twin.

### 7. Record the report menu paths

This is the one part that could not be written in advance — it is behind the
login, and every report has its own filters.

```bash
pwsh bin/Debug/net10.0/playwright.ps1 codegen https://dbtfert.nic.in/mFMS/loginNew.action
```

Sign in by hand in the window that opens, click through to a report, apply its
filters, export. The recorder prints a selector for every action. Copy them into
`ReportJobs:Jobs[].Steps` in `appsettings.json`, replace the typed dates with
`{{reportDate}}`, and set `Enabled: true`.

There are **14 jobs**, not 7: each report exists once per company, because the
parameters selected inside the report page differ between SPIC and Greenstar.
They share a `CategoryId` and differ by `AccountKey`.

Start with one report for one company. Once that works end to end, the rest are
the same shape.

#### Step actions

| Action | Does |
|---|---|
| `goto` | navigate to `Value` |
| `click` | click `Selector` |
| `fill` | type `Value` into `Selector` |
| `select` / `selectText` | choose a dropdown option by value / by label |
| `check` / `uncheck` | tick a checkbox |
| `waitFor` / `waitHidden` | wait for `Selector` to appear / disappear |
| `wait` | sleep `TimeoutMs` |
| `press` | send a key |
| `frame` / `mainFrame` | move in and out of an iframe |
| `eval` | run JavaScript (escape hatch) |

Set `Optional: true` on a step that may not be there — a disclaimer popup, a
cookie banner.

#### Tokens

`{{reportDate}}`, `{{reportDate:yyyy-MM-dd}}`, `{{fromDate}}`, `{{toDate}}`,
`{{today}}`, `{{yesterday}}`, `{{monthStart}}`, `{{monthEnd}}`,
`{{financialYear}}`, `{{userName}}`.

Dates default to `dd/MM/yyyy`.

### 8. Verify the logged-in selector

`Ifms:Selectors:LoggedIn` is currently a guess (`a[href*='logout']`). Everything
depends on it: if it never matches, every login looks like a failure even when it
worked. Confirm it while you have the recorder open.

### 9. The OTP field ids

The OTP screen only appears after a correct username, password and CAPTCHA, so
its markup could not be inspected in advance. Left unset, the automation finds
the field itself — first anything named like an OTP box, then the single empty
input on the page — and **logs what it found**.

Read that line from the first real run and put the real values in
`Ifms:Selectors:OtpInput` and `OtpSubmit`. The guessing then never runs again.
If it ever finds more than one candidate it refuses to guess, saves a screenshot,
and says so.

## Running it

Development, watching the browser work:

```bash
dotnet run
```

`appsettings.Development.json` turns the schedule off, runs the browser visibly
and slows it down, so you can trigger runs from the dashboard's **Run now**
button and watch.

### As a Linux service

Deploying to **103.14.121.144** (`cam.server`), which also hosts the Postgres
this writes to. Chosen over the other candidate box because that one had swap
fully consumed and under 1 GB free, and belongs to a different product; this one
idles at 9% memory on a current kernel.

Everything needed is in `deploy/`. From a checkout on the server:

```bash
sudo ./deploy/setup.sh
```

That checks for the .NET runtime and `tzdata`, creates `/opt/spic-ifms`, writes
a `0600` secrets file and installs the systemd unit. Safe to re-run.

Then edit `/opt/spic-ifms/secrets.env`, and:

```bash
sudo ./deploy/publish.sh
```

Which builds, installs Chromium, fetches the OCR data if missing, and restarts
the service. Re-run it for every update — it deliberately does not use
`rsync --delete`, so `downloads/`, `diagnostics/` and `secrets.env` survive.

Finally the two portal logins, and enable it:

```bash
cd /opt/spic-ifms && dotnet SPIC.Ifms.Automation.dll set-credentials greenstar 1000249825 "<password>" Greenstar
```

```bash
sudo systemctl enable --now spic-ifms && journalctl -u spic-ifms -f
```

#### Notes on the unit

- `MemoryMax=2G` and `CPUQuota=150%`. This box also runs the JT1078 video
  gateways and Postgres; a stuck Chromium must not be able to starve them.
- `StartLimitBurst=3`. The service idles until 04:05, so a crash loop would
  otherwise be invisible — this turns it into a failed unit you can see.
- `LimitNOFILE=65535`, because Chromium wants far more descriptors than the
  default 1024.
- The connection string in `secrets.env` uses `localhost`, since the database is
  on the same host. It overrides `appsettings.json`.

#### Line endings

`deploy/.gitattributes` pins `*.sh` and `*.service` to LF. Checked out with CRLF
on a Linux box, a script fails with `bad interpreter: /bin/bash^M` — which is
not an obvious error the first time you meet it.

#### The first deployment is useful even with no reports configured

All 14 jobs ship disabled. Deploy anyway: at 04:05 the service will wake, probe
the portal, sign in as both companies, and stop. That proves the CAPTCHA solver,
the OTP relay and the logged-in selector against the real host — and the log
tells you the OTP field ids to put in config. Enable reports one at a time
afterwards.

## The Android side

The phone holding the IFMS SIM does two jobs: forward the OTP, and ring when a
CAPTCHA needs a human.

1. Install the SPIC app on that phone.
2. Open **IFMS OTP Relay**, enter the API address and device key, press **Pair**.
3. Allow SMS and notifications when Android asks.

It stays switched off until paired, so an ordinary user's handset never forwards
anything. Turning it off on that screen stops it immediately.

A build carrying `RECEIVE_SMS`/`READ_SMS` cannot go on the public Play Store —
Google restricts those to messaging apps. Sideload it or use a managed
enterprise channel.

## Watching it

**IFMS Auto Import** in the portal shows the last 30 runs, per-report row counts,
and a **Run now** button. When a CAPTCHA is waiting it appears at the top of that
page with a text box — same on the phone.

When something breaks, look in `diagnostics/<date>/` — a full-page screenshot and
the raw HTML from the moment it failed. Debugging a headless failure without
those is guesswork.

Downloaded files are kept in `downloads/<report-date>/` for 120 days. When a
number looks wrong months later, the file the portal actually served settles it.

## Things worth knowing

- **A partial run is not retried.** Six good reports and one failure will not
  re-download the six. The alert names the one to do by hand.
- **An `.xlsx` that is not a zip is rejected before parsing.** These portals
  answer an export click with an HTML error page more often than you would like,
  and it arrives with an `.xlsx` name.
- **A report with zero rows is treated as a failure** unless the job sets
  `AllowEmpty: true`.
- **`ReuseStoredSession` is the thing that keeps CAPTCHAs rare.** If the portal
  turns out to expire sessions nightly it costs nothing; if it does not, most
  mornings need no CAPTCHA and no OTP at all — worth twice over now that there
  are two logins.
- **Passwords expire every 80 days.** The run warns in the log inside the last
  ten days and shouts once expired. The **IFMS Logins** page shows the countdown.
  Changing the password on the portal and forgetting to update it here is the
  most likely cause of a sudden run of failures.
- **Losing the `DataProtectionKeys` rows makes every stored password
  unreadable.** They are in the database, so a normal backup covers them — but do
  not truncate that table.
