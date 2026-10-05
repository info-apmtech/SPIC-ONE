# SPIC ONE: publishing the app to Google Play, the App Store and Windows

The MAUI head (`SPIC.MauiBlazorApp/SPIC.MauiBlazorApp`) is one project that builds
for Android, iOS, Mac Catalyst and Windows. This runbook covers what each store needs,
the exact commands, and how updates reach users afterwards. The Web host is deployed
separately (see `azure-deployment.md`) and needs none of this.

Identity used everywhere (set in the csproj):

| Setting | Value |
|---|---|
| Display name | SPIC ONE |
| Package / bundle id | `com.apmiot.spicone` |
| Version shown to users | `ApplicationDisplayVersion` (`1.0.0`) |
| Build number | `ApplicationVersion` (`1`, must increase on every store upload) |
| Brand colour (icon, splash) | `#0b6b3a` |

Bump both version properties in the csproj for every release, on every platform at
once, and tag the commit. The same number then appears in Play, App Store Connect and
the Windows package.

---

## 0. Before the first submission (all platforms)

| Item | Status | What to do |
|---|---|---|
| App identity, icon, splash, window title | Done | Icon foreground is the 120 px `logo.png`. Replace `Resources/AppIcon/appiconfg.png` with a vector or 1024 px version before the listing; stores show the icon at up to 1024 px. |
| Login survives app restart | Done | `SecureSessionStore` (Keychain / Android Keystore / DPAPI). Tokens still expire after 60 min because the API has no refresh token. |
| Assets bundled locally | Done | Bootstrap, icons, jQuery, Select2, Leaflet, Chart.js are served from `wwwroot/lib`; no CDN at runtime. |
| API address | Done | Build property `SpicApiBaseUrl`, default `https://api.spicone.in/`. Override per environment: `dotnet publish ... -p:SpicApiBaseUrl=https://previewspicapi.apmiot.com/`. |
| Google Play 16 KB page-size rule | Done | QuestPDF removed from the Shared project; the data-table PDF export is rendered by `POST api/Export/table-pdf`. The Android build no longer warns `XA0141`. |
| Mobile parity: downloads, file viewing, external links, geolocation, print | Done | `maui-interop.js` + `DeviceInterop.cs` route the browser-only helpers to native APIs. File upload works out of the box in the .NET 10 BlazorWebView. |
| Privacy policy URL | Done (review wording) | The app now serves its own policy at `/privacy` (`Pages/PrivacyPolicy.razor`), linked from the login footer on web and app. Register **https://spicone.in/privacy** in both stores. Have SPIC review the text, the retention periods and the contact address (`sdwa@spic.co.in` is the only address found in the app; change it if support has a different mailbox). |
| Reviewer test account | **Open** | Apple always, Google sometimes: a working login with representative data. |
| Camera / QR scanning | Not built | The scanner pages are visual mock-ups; no camera permission is requested. Add only when the feature is implemented. |

---

## 1. Android: Google Play

You already have a Play Console account.

### 1.1 One-time: upload keystore

Play App Signing holds the real signing key; you keep an *upload* key. Generate it once
and store it outside the repository (password manager plus an offline copy). Losing it
means a support request to Google to reset it.

```bash
keytool -genkeypair -v -keystore spicone-upload.keystore -alias spicone -keyalg RSA -keysize 2048 -validity 10000
```

The csproj signs automatically when these environment variables are present, so
developer builds never need the keystore:

```powershell
$env:SPICONE_ANDROID_KEYSTORE = "D:\secure\spicone-upload.keystore"
$env:SPICONE_ANDROID_KEY_ALIAS = "spicone"
$env:SPICONE_ANDROID_KEY_PASS = "<key password>"
$env:SPICONE_ANDROID_STORE_PASS = "<store password>"
```

### 1.2 One-time: Play Console app record

1. **Create app**: name *SPIC ONE*, default language, App, Free.
2. **App content** (left menu, Policy): privacy policy URL; **Data safety** form (collects name,
   phone, email, precise location, files and documents, photos; encrypted in transit; not shared);
   **Ads**: none; **Target audience**: 18+; **News app**: no; **Government app**: no;
   **Financial features**: none; **Content rating** questionnaire (Utility).
3. **Store listing**: short description (80 chars), full description, 512 px icon, 1024x500
   feature graphic, at least 2 phone screenshots (16:9 or 9:16, 320 to 3840 px), tablet
   screenshots if you keep tablets enabled.
4. **App access**: "All or some functionality is restricted", add the reviewer login.
5. **Countries**: India (plus any others).

### 1.3 Every release: build and upload

```bash
dotnet publish SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.csproj -f net10.0-android -c Release
```

Output: `SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/bin/Release/net10.0-android/publish/com.apmiot.spicone-Signed.aab`.

1. Play Console, **Testing, Internal testing**, *Create new release*, upload the `.aab`.
   Internal testing has no review and reaches up to 100 testers within minutes: use it first.
2. Add release notes, roll out, share the opt-in link with testers.
3. Promote the same release to **Closed testing**, then **Production**. Production review takes
   from a few hours to about 3 days for a new app.
4. Each new upload needs a higher `ApplicationVersion`.

Personal developer accounts created after November 2023 must run a closed test with at
least 12 testers for 14 days before production is unlocked; organisation accounts are exempt.

---

## 2. iOS: App Store

You have a Mac and an Apple Developer account. iOS builds only happen on the Mac (or a
Mac paired to Visual Studio on Windows); Windows alone cannot produce an `.ipa`.

### 2.1 One-time: Mac setup

1. Install the current Xcode from the Mac App Store, open it once, accept the licence,
   install the iOS platform when prompted.
2. Install the .NET 10 SDK, then `sudo dotnet workload install maui`.
3. Xcode, Settings, Accounts: sign in with the Apple ID that owns the developer account.
4. Clone the repository on the Mac. Confirm a build works before touching signing:

```bash
dotnet build SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.csproj -f net10.0-ios -c Debug
```

### 2.2 One-time: certificates, identifier, profile

In [developer.apple.com](https://developer.apple.com/account) under *Certificates, Identifiers & Profiles*:

1. **Identifiers**: register an App ID, explicit, `com.apmiot.spicone`. No extra
   capabilities are needed today (Keychain access for SecureStorage is implicit).
2. **Certificates**: create an *Apple Distribution* certificate. Easiest path: in Xcode,
   Settings, Accounts, *Manage Certificates*, plus, Apple Distribution. It lands in your
   login keychain.
3. **Profiles**: create an *App Store* distribution profile for the App ID, named
   `SPIC ONE App Store`, download and double-click it.

### 2.3 One-time: App Store Connect record

1. **My Apps**, plus, *New App*: iOS, name *SPIC ONE*, bundle id `com.apmiot.spicone`,
   SKU `spicone-ios`.
2. **App Information**: category Business, privacy policy URL.
3. **App Privacy**: declare Contact Info, Location, User Content (photos, documents),
   Identifiers; "used for app functionality", "linked to the user", not for tracking.
4. **Pricing**: Free. **Availability**: India.
5. **Version page**: description, keywords, support URL, screenshots. Because
   `Info.plist` lists device family 1 and 2, you need iPhone 6.7" and iPad 13"
   screenshots. If you do not want to support iPad, remove `<integer>2</integer>`
   from `UIDeviceFamily` and only iPhone shots are required.
6. **App Review Information**: the reviewer login and a note explaining the app is for
   SPIC dealers and staff (this is what stops a "limited audience" rejection).
7. Add `ITSAppUsesNonExemptEncryption` = `false` to `Platforms/iOS/Info.plist` so every
   build skips the export-compliance question.

### 2.4 Every release: build, upload, TestFlight

Bump the versions in the csproj, then on the Mac:

```bash
dotnet publish SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.csproj -f net10.0-ios -c Release -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Distribution: <Team Name> (<TEAMID>)" -p:CodesignProvision="SPIC ONE App Store"
```

Output: `bin/Release/net10.0-ios/ios-arm64/publish/SPIC.MauiBlazorApp.ipa`.

1. Open **Transporter** (free, Mac App Store), sign in, drag the `.ipa`, *Deliver*.
2. In App Store Connect the build appears under **TestFlight** after processing
   (10 to 30 minutes). Internal testers (your team, up to 100) can install at once via
   the TestFlight app. External testers need a short beta review first.
3. On the version page choose the build, *Add for Review*, *Submit*. First reviews take
   1 to 3 days; updates are usually under 24 hours.

Review risks specific to this app: guideline 4.2 (minimum functionality) is where web
wrappers fail, so the Part 3 parity work matters; guideline 2.1 needs the demo login;
guideline 5.1.1(v) account deletion does not apply because there is no self sign-up.

---

## 3. Windows

Two ways to ship, both start from the same project.

### 3.1 Recommended: MSIX with App Installer (built-in auto-update)

The project is already set to `WindowsPackageType=MSIX`. An MSIX must be signed with a
code-signing certificate whose subject matches `Publisher` in
`Platforms/Windows/Package.appxmanifest`. Users then install from a link and Windows
checks that link for new versions on every launch.

One-time:

1. **Certificate**. Options, cheapest first: *Azure Trusted Signing* (an Azure resource,
   about USD 10 per month, signs with a Microsoft-trusted certificate, fits the existing
   Azure subscription); an OV code-signing certificate from Sectigo, DigiCert or GlobalSign
   (about USD 200 to 400 per year, delivered on a hardware token). A self-signed
   certificate works only on PCs where you install it manually, fine for internal testing.
2. Set `Publisher="CN=<exactly the certificate subject>"` and `PublisherDisplayName` in
   `Package.appxmanifest`. The display name, logos and version are filled from the csproj
   at build time.
3. Create a public folder for the packages: an Azure Storage account with *Static website*
   enabled (or any HTTPS host). Example base URL:
   `https://spiconeapp.z29.web.core.windows.net/windows/`.

Every release, from this PC:

```powershell
dotnet publish SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:GenerateAppInstallerFile=true -p:AppInstallerUri=https://spiconeapp.z29.web.core.windows.net/windows/ -p:AppInstallerCheckForUpdateFrequency=OnApplicationRun -p:AppInstallerUpdateFrequency=0 -p:AppxPackageSigningEnabled=true -p:PackageCertificateThumbprint=<thumbprint>
```

Output folder: `bin/Release/net10.0-windows10.0.19041.0/win-x64/AppPackages/SPIC.MauiBlazorApp_1.0.0.0_Test/`
containing the `.msix`, `SPIC.MauiBlazorApp.appinstaller`, and an `index.html` install page.
Upload the whole folder to the static website. Users open the `.appinstaller` link once;
after that Windows updates the app automatically when a newer version is uploaded.

With Azure Trusted Signing, replace the thumbprint property with the `SignTool`
step from the Trusted Signing docs after publish.

### 3.2 Alternative: plain `.exe` folder plus installer

If an MSIX is not wanted (for example for PCs with App Installer disabled by policy):

```powershell
dotnet publish SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true
```

Wrap the output folder with Inno Setup for a classic `setup.exe`. Auto-update then has
to be added in code; Velopack is the usual .NET choice. This is more work and more to
maintain than the MSIX route, so use it only if MSIX is ruled out.

### 3.3 Microsoft Store (possible, and the cheapest signed route)

The same MSIX can go to the Microsoft Store. The Store signs the package with its own
certificate, so **no code-signing certificate is needed**, and it delivers updates
automatically like Google Play does. Reviews add roughly a day per release.

One-time:

1. Register at [partner.center.microsoft.com](https://partner.microsoft.com/dashboard)
   as a company (one-time USD 19 for individuals, USD 99 for companies; company
   verification takes a few days).
2. *Apps and games*, *New product*, *MSIX or PWA app*, reserve the name **SPIC ONE**.
3. On the product's *Product identity* page copy the three values (Package/Identity/Name,
   Publisher, PublisherDisplayName) into `Platforms/Windows/Package.appxmanifest`, and
   set `<ApplicationTitle>` to match the reserved name. The Store's Publisher id replaces
   the placeholder `CN=User Name`.
4. Fill *Properties* (category Business), *Age ratings* (IARC questionnaire),
   *Store listing* (description, at least one 1366x768 or larger screenshot, 300x300 logo),
   privacy policy URL, and *Availability* (markets: India, or all; visibility can be
   "Private audience" for an internal roll-out, which is the Windows equivalent of Play's
   internal track).

Every release:

```powershell
dotnet publish SPIC.MauiBlazorApp/SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false -p:UapAppxPackageBuildMode=StoreUpload
```

Upload the `.msixupload` from `bin/Release/net10.0-windows10.0.19041.0/win-x64/AppPackages/`
on the submission's *Packages* page, then *Submit to the Store*. Users install from the
Store app or from the `ms-windows-store://pdp/?productid=...` link, and updates arrive
automatically.

Choose between 3.1 and 3.3 by audience: staff-only PCs where IT can install from a link
suit App Installer (3.1); a public or dealer audience suits the Store (3.3). Both can run
in parallel from the same build.

---

## 4. Auto-update on every platform

| Platform | Mechanism | Anything to build? |
|---|---|---|
| Android | Google Play updates apps automatically (user setting, on by default). Play's *In-App Updates* API can force an immediate update. | Optional: version-check dialog below. |
| iOS | App Store automatic updates (on by default). No API to force; an in-app prompt linking to the App Store page is the norm. | Optional: version-check dialog below. |
| Windows MSIX | App Installer checks the `.appinstaller` URL on launch and installs silently. | Nothing beyond section 3.1. |
| Windows exe | None built in. | Velopack or a custom updater. |
| Web | Always current. | Nothing. |

**Recommended cross-platform addition (small, one endpoint plus one dialog):**

1. API: `GET /api/app/version` returning `{ "minimumVersion": "1.0.0", "latestVersion": "1.2.0", "android": "<play url>", "ios": "<app store url>", "windows": "<appinstaller url>" }`, values from configuration so they change without a deploy of the app.
2. App: on start (and when coming back to the foreground) compare `AppInfo.Current.Version`
   to the response. Below `minimumVersion`: blocking screen with an *Update* button that
   opens the store link. Between minimum and latest: dismissible banner.
3. This gives the same "please update" behaviour on every platform and lets you retire
   old builds after a breaking API change.

---

## 5. Release checklist

1. Bump `ApplicationDisplayVersion` and `ApplicationVersion` in the csproj; commit and tag.
2. Build and upload Android (`.aab`) from this PC, iOS (`.ipa`) from the Mac, Windows
   (`.msix` folder) from this PC, using the commands above.
3. Android: Internal testing first, then promote. iOS: TestFlight internal first, then
   submit. Windows: upload the AppPackages folder; App Installer does the rest.
4. Deploy the API before the apps when a release needs new endpoints; the app cannot be
   rolled back on users' devices, the API can.
