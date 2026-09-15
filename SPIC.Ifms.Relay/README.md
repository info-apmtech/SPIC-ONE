# SPIC IFMS Relay

A small, dedicated Android app for the phone that holds the IFMS SIM. It does
three things and nothing else:

1. **Forwards the IFMS one-time-password SMS** to SpicAPI within seconds of it
   arriving, so the 04:05 automated login needs nobody awake.
2. **Rings when the automation cannot read a CAPTCHA**, shows the image, and
   lets the person type the answer.
3. **Keeps a heartbeat** so the server can tell a live phone from a dead one.

It replaces the relay that used to live inside the main SPIC ONE app. Moving it
out means the main app needs no SMS permission, and this app has no sign-in,
no WebView and nothing to load: it opens straight onto the CAPTCHA.

Plain .NET MAUI (XAML + C#), `net10.0-android` only, no reference to any other
project in the solution. Package id `com.apmiot.spic.ifmsrelay`.

## Endpoints

All relative to the API base (default `https://spicapi.apmiot.com/`). Every
call after pairing carries `X-Device-Token`; every such call also refreshes
`LastSeenAt` on the server, so polling doubles as the heartbeat.

| Call | Auth | Purpose |
|---|---|---|
| `POST api/IfmsAutomation/devices/register` | `X-Device-Key` (pairing key) | Body `{DeviceId, DeviceName, AppVersion, Platform}` → `{Success, Token, ReplacedExisting}`. 401 = wrong key. |
| `POST api/IfmsAutomation/sms` | token | Body `{DeviceId, Sender, Body, ReceivedAt}`. |
| `GET api/IfmsAutomation/challenge/pending` | token | `null` or `{Id, RunId, ChallengeType, ImageBase64, Prompt, Round, FailedGuesses, CreatedAt, ExpiresAt, SecondsRemaining}`. Polled every 60 s by the watcher. |
| `POST api/IfmsAutomation/challenge/{id}/answer` | token | Body `{Answer}`. Non-2xx body is `{Success, Message}` (for example "expired"); the message is shown as-is. |
| `POST api/IfmsAutomation/devices/heartbeat` | token | Explicit "alive"; rarely needed because polls already count. |
| `GET api/IfmsAutomation/status` | token | `{PendingChallenge, LatestRun, NeedsAttention, Headline}`. The Home page and "Test connection" use this. |

The pairing key is typed once, sent once to `register`, and never stored.

## How to pair

On the phone holding the IFMS SIM:

1. Install the APK (sideload; see below) and open **SPIC IFMS Relay**.
2. Go to the **Pair** tab. The API address is prefilled; enter the **device
   key** (the server's `IfmsAutomation:DeviceKey`) and tap **Pair**.
3. Android asks for **SMS** permission, then **notifications**. Allow both.
4. The page says "Paired. Registered as Xiaomi-2312FRAFDI-xxxxxx". The watcher
   starts and a quiet "SPIC IFMS Relay - Watching for the IFMS OTP" notification
   appears. That notification is permanent while paired; it is what keeps the
   app alive.
5. Go to the **Home** tab and tap **Allow background running**; choose Allow in
   Android's dialog. Then follow the HyperOS steps below.
6. Tap **Test connection**. It should say "Connected." followed by the server's
   headline for the last run.

"Turn off" on the Pair tab clears the token, switches the relay off and stops
the watcher. Re-pairing the same phone rotates its token on the server rather
than creating a second device row.

### What leaves the phone

"Forward only IFMS messages" is **on by default**. With it on, an SMS is
forwarded only if its body contains "ifms" (any case) or its sender matches one
of the **accepted senders** (default `7305430555`; `+91`, spaces and
punctuation are ignored when comparing). Everything else stays on the phone and
is never sent anywhere; the activity list shows only "SMS from <sender> skipped
by the IFMS filter". Switch it off only for a SIM that receives nothing but
IFMS traffic.

Message bodies are never logged or displayed, only the sender, the time and
whether the message was forwarded.

## How to answer a CAPTCHA

1. The phone vibrates and shows **"IFMS needs the CAPTCHA"** (high-priority
   notification). Tap it; the app opens on the Home tab with the CAPTCHA card
   at the top. If the app is already open, the card appears by itself within
   15 s.
2. The card shows the image, what the automatic solver read before giving up,
   and an "expires in mm:ss" countdown.
3. Type what you see (keyboard opens in capitals, no autocorrect) and tap
   **Send** or the keyboard's Send key.
4. "Sent. The download is continuing." means done. If it says the CAPTCHA
   expired, wait: the automation sends a fresh image and the phone rings again.

## Home tab

- **Headline** from `/status`, refreshed on open and every 15 s while visible.
- **Status**: relay on/off, SMS permission, notifications, battery
  optimisation, watcher running, device id, last contact, last SMS relayed.
- **Refresh**, **Test connection**, **Allow background running** (Android's
  `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` dialog), **Open battery settings**
  (the app's own settings page, the HyperOS fallback).
- **Recent activity**: the last 30 events (forwarded, skipped, poll failures,
  CAPTCHA shown, answer sent, server unreachable/reachable again).

## Building the APK

```
cd D:\GIT_C\GIT\SPIC-ONE-ifms
dotnet build   SPIC.Ifms.Relay/SPIC.Ifms.Relay.csproj -f net10.0-android -c Release --nologo -v q
dotnet publish SPIC.Ifms.Relay/SPIC.Ifms.Relay.csproj -f net10.0-android -c Release -p:AndroidPackageFormats=apk --nologo -v q
```

Output: `SPIC.Ifms.Relay/bin/Release/net10.0-android/publish/com.apmiot.spic.ifmsrelay-Signed.apk`
(signed with the default debug keystore, which is fine for sideloading; a
replacement build must be signed with the same key or Android will refuse to
update over the installed copy).

Install with `adb install -r <apk>` or copy the file to the phone and open it.
Google Play will not accept an app with `RECEIVE_SMS`/`READ_SMS` unless
messaging is its core function, so this app is for sideloading only.

## HyperOS / Xiaomi notes (2312FRAFDI, Android 15)

HyperOS kills background apps that are not on its allow-list, foreground
service or not. After pairing:

- Settings → Apps → Manage apps → **SPIC IFMS Relay** → **Battery saver**:
  **No restrictions**.
- Same screen → **Autostart**: **on**.
- Same screen → **Other permissions** → allow "Display pop-up windows while
  running in the background" if offered (lets the CAPTCHA screen come up).
- Open Recents, long-press the app card and **lock** it (padlock) so "clear
  all" does not kill it.
- Home tab → **Allow background running** → Allow (the standard Android Doze
  exemption).

The app restarts its watcher whenever it is opened and after a reboot, so if a
run is missed, opening the app once is the recovery.

### Why the foreground service is typed `specialUse`, not `dataSync`

Android 15 (the phone's version) applies two rules to `dataSync` services:
they may not be started from `BOOT_COMPLETED`, and they are stopped after six
hours of use in any 24-hour window. A watcher started when the phone was last
opened in the evening would hit the six-hour limit at about the time the 04:05
run needs it. `specialUse` has neither restriction. Google Play asks for a
justification for that type; a sideloaded app needs none.

## What the old main-app relay left behind

- The old relay registered as device row **Id 2** on the server. That row
  stays; nothing depends on it. It can be revoked from the SPIC ONE portal
  (`POST api/IfmsAutomation/devices/2/revoke`) once this app is paired.
- This app generates a **new device id** (`Manufacturer-Model-6hex`) and
  registers a new device row on first pairing. Both can coexist; the server
  accepts any registered token.
- The relay code (receiver, watch service, settings, pairing page) is removed
  from the main app in the same change, so a main app built from this commit
  onwards forwards nothing and asks for no SMS permission. Until the phone
  gets that newer main-app build, "Turn off" the relay on its IFMS Relay
  screen so two apps do not race to forward the same SMS.
- The server contract did not change; the endpoint table above is the whole
  of it.

## Files

```
SPIC.Ifms.Relay.csproj              net10.0-android, MAUI, source-generated XAML
MauiProgram.cs / App.xaml(.cs)      fonts, merged styles, Shell window
AppShell.xaml(.cs)                  two tabs: Home, Pair
Pages/HomePage.xaml(.cs)            status, CAPTCHA card, buttons, activity log
Pages/PairPage.xaml(.cs)            pairing form, filter switch, accepted senders
Services/RelaySettings.cs           Preferences-backed settings (no pairing key)
Services/RelayClient.cs             the six HTTP calls + TestConnection
Services/RelayDtos.cs               wire shapes + source-generated JSON context
Services/RelayLog.cs                30-entry persisted activity ring (no SMS bodies)
Services/RelayEvents.cs             static events between service/activity and page
Platforms/Android/IfmsSmsReceiver.cs        SMS_RECEIVED receiver with IFMS filter
Platforms/Android/RelayForegroundService.cs 60 s poll + heartbeat + CAPTCHA alert
Platforms/Android/BootReceiver.cs           restart after boot
Platforms/Android/MainActivity.cs           singleTop; show=captcha extra
Platforms/Android/BatteryOptimisation.cs    Doze exemption + app settings intents
Platforms/Android/SmsPermission.cs          RECEIVE_SMS + READ_SMS
Platforms/Android/AndroidManifest.xml       permissions
Resources/AppIcon, Splash, Fonts, Styles
```
