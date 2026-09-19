# Feedback & form-chrome components

Folder: `SPIC.MauiBlazorApp.Shared/Components/Feedback/` (namespace `SPIC.MauiBlazorApp.Shared.Components.Feedback`).
Services: `Services/ToastService.cs`, `Services/ConnectivityState.cs`. Interop: `wwwroot/js/feedback-interop.js`.

All components are opt-in: nothing changes on existing pages until they are placed in a layout or a page.
Breakpoints: phone `<= 767px`, tablet `768-1199px`, desktop `>= 1200px`. Styles are scoped (`*.razor.css`), so they
are bundled into `SPIC.MauiBlazorApp.Web.styles.css` / `SPIC.MauiBlazorApp.styles.css`, which both host pages already link.

Dev demo (no login needed): **`/dev/feedback`** (`_FeedbackDemo.razor`, uses `EmptyLayout`). It loads the interop
script itself and creates its own service instances, so it runs before any of the wiring below is done.

---

## 1. Integration (coordinator)

### 1a. DI registration

`SPIC.MauiBlazorApp.Web/Program.cs` (next to the other `AddScoped` lines):

```csharp
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConnectivityState>();
```

`SPIC.MauiBlazorApp/MauiProgram.cs` (same block as `LoginState`/`LoadingService`):

```csharp
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ConnectivityState>();
```

Both already have `using SPIC.MauiBlazorApp.Shared.Services;`.

### 1b. Script tag (both host pages, after `app-interop.js`)

`SPIC.MauiBlazorApp.Web/Components/App.razor` and `SPIC.MauiBlazorApp/wwwroot/index.html`:

```html
<script src="_content/SPIC.MauiBlazorApp.Shared/js/feedback-interop.js"></script>
```

In `index.html` put it **before** `maui-interop.js` (it does not touch downloads/geolocation, so order relative to
`maui-interop.js` does not matter functionally, but keeping "shared first, MAUI overrides last" is the house rule).

Without the script, the components still work but lose: back-button/Escape/swipe closing and scroll-lock for
`BottomSheet`, exact spacer sizing for `ActionBar`, and offline detection (banner stays hidden, app assumed online).

### 1c. `_Imports.razor` (Shared)

```razor
@using SPIC.MauiBlazorApp.Shared.Components.Feedback
```

### 1d. MainLayout placement

Inside the `@if (_isReady)` block, directly after `<LoadingOverlay />` (they are all `position: fixed`, so the exact
spot inside `.app-shell` does not affect layout):

```razor
<div class="app-shell">

    <LoadingOverlay />
    <ToastHost />
    <ConfirmDialog />
    <OfflineBanner />
    <SessionExpiryDialog />

    <aside class="sidebar">
```

`ToastHost` and `ConfirmDialog` are also useful in `EmptyLayout` (Login page error toasts). If you add them there,
do **not** also add `SessionExpiryDialog`/`OfflineBanner` to EmptyLayout unless wanted on the login screen.

Optional: when the shell gets a phone tab bar, set its height once on a wrapper so `ActionBar` and `ToastHost` sit
above it:

```css
.app-shell { --shell-bottom-offset: 56px; }   /* phone only, e.g. inside @media (max-width: 767px) */
```

### 1e. Replacing browser `confirm()` on existing pages

```razor
@inject ToastService Toasts
...
if (await Toasts.ConfirmAsync("Delete this scheme?", "This cannot be undone.", "Delete", danger: true))
{
    await DeleteAsync();
    Toasts.Success("Scheme deleted.");
}
```

### 1f. Converting an existing *Drawer.razor* to `BottomSheet`

Existing drawers render `@if (IsOpen) { <div class="xx-overlay" @onclick="Close"><div class="xx-drawer">...` and
expose `IsOpen` / `IsOpenChanged`. Wrap the drawer *contents* (header text, body, footer) instead of the overlay:

```razor
<BottomSheet @bind-IsOpen="IsOpen" Title="Dealer Details" Subtitle="Complete information about the dealer" Size="BottomSheet.SheetSize.Full">
    <ChildContent>
        @* the former .dd-body content, unchanged *@
    </ChildContent>
    <Footer>
        @* the former footer buttons, unchanged *@
    </Footer>
</BottomSheet>
```

The drawer keeps its 420px right-side look on tablet/desktop (`Size.Full` = 720px) and becomes a real bottom sheet on
phones. Delete the page's own `.xx-overlay`/`.xx-drawer` CSS when done.

---

## 2. Public API

### `BottomSheet`

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `IsOpen` / `IsOpenChanged` | `bool` | | two-way bindable |
| `Title`, `Subtitle` | `string?` | | header |
| `ChildContent` | `RenderFragment?` | | scrollable body |
| `Footer` | `RenderFragment?` | | sticky footer; buttons stretch on phone |
| `HeaderActions` | `RenderFragment?` | | extra controls left of the close button |
| `Size` | `SheetSize` `Auto \| Half \| Full` | `Auto` | phone: content / 50% / full height; drawer: 420px / 420px / 720px |
| `CloseOnBackdrop` | `bool` | `true` | |
| `ShowCloseButton` | `bool` | `true` | |
| `CssClass` | `string?` | | extra class on the root |
| `OnClosed` | `EventCallback<CloseReason>` | | `Button, Backdrop, Escape, History, Swipe, Programmatic` |

Methods: `OpenAsync()`, `CloseAsync()`, `CloseAsync(CloseReason)`. `[JSInvokable] RequestClose(string)` is called by
the interop script (Escape / popstate / swipe).

Behaviour: pushes `history.state = { spicSheet: id }` on open so **browser back / Android hardware back** closes the
sheet instead of leaving the page; the entry is popped again on programmatic close. Body scroll is locked while open
(ref-counted). Focus moves into the panel and is restored on close. CSS hooks: `--fb-drawer-width` (420px),
`--fb-drawer-width-full` (720px), `--fb-z-sheet` (10000).

MAUI note: hardware back reaches `history.back()` only if the host lets the WebView handle it. If the Android
`MainActivity`/`MainPage` overrides `OnBackButtonPressed`, it should call `webView.GoBack()` (or evaluate
`history.back()`) when `window.spicFeedback.sheet.openCount() > 0`.

### `ActionBar`

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | | primary buttons |
| `Secondary` | `RenderFragment?` | | Cancel/Back etc., rendered first |
| `Sticky` | `bool` | `true` | phone: fixed to bottom; `false` keeps it inline everywhere |
| `Align` | `BarAlign` `End \| Start \| Between` | `End` | tablet/desktop alignment |
| `CssClass`, `AriaLabel` | `string` | | |

Phone: `position: fixed; bottom: var(--shell-bottom-offset, 0px)`, safe-area padding, top shadow, buttons 44px and
stretched. A spacer element precedes the bar; `feedback-interop.js` keeps it exactly as tall as the bar
(fallback 64px + safe area) and publishes `--fb-actionbar-height` on `<html>` so `ToastHost` stacks above it.
Tablet/desktop: static right-aligned row, no spacer.

### `ToastService` (scoped)

```csharp
void Success(string message, string? title = null, int durationMs = 3500);
void Error  (string message, string? title = null, int durationMs = 3500);
void Info   (string message, string? title = null, int durationMs = 3500);
void Warning(string message, string? title = null, int durationMs = 3500);
Guid Show(ToastLevel level, string message, string? title = null, int durationMs = 3500); // durationMs <= 0 = sticky
void Dismiss(Guid id);  void Clear();
IReadOnlyList<ToastMessage> Toasts;  int QueuedCount;  int MaxVisible = 4;   // extra toasts queue
event Action? OnChange;

Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", bool danger = false, string cancelText = "Cancel");
ConfirmRequest? PendingConfirm;  void ResolveConfirm(bool result);           // used by ConfirmDialog
```

`OnChange` may fire from any thread; components use `InvokeAsync`. The service holds no timers (auto-dismiss lives in
`ToastHost`), so it needs no disposal.

### `ToastHost`

Parameters: `CssClass`. Renders `aria-live="polite"` region; each toast is `role="status"` (`alert` for Error/Warning).
Desktop/tablet: top-right stack (380px). Phone: bottom-centre above `--fb-actionbar-height` + `--shell-bottom-offset`
+ safe area. Hover pauses auto-dismiss; leaving resumes with a 1.5s grace. z-index `--fb-z-toast` (10020).

### `ConfirmDialog`

No parameters. Shows `ToastService.PendingConfirm`; Cancel/backdrop/Escape resolve `false`. Focus lands on **Cancel**
so a stray Enter cannot confirm a destructive action. Danger mode = red icon + red confirm button. Phone: bottom card;
desktop: centred. Multiple `ConfirmAsync` calls are shown one after another.

### `ConnectivityState` (scoped, `IAsyncDisposable`)

```csharp
bool IsOnline;  DateTime? OfflineSinceUtc;  bool IsListening;
event Action? OnChange;  event Action? OnBackOnline;
Task InitializeAsync();          // idempotent; OfflineBanner calls it after first render
[JSInvokable] void OnConnectivityChanged(bool isOnline);
```

Uses `navigator.onLine` + `online`/`offline` window events through `spicFeedback.connectivity`. Works in browsers and
the MAUI BlazorWebView. Note `navigator.onLine` reports *network interface* state: a captive portal or dead Wi-Fi can
still read as online, so keep API error handling as the source of truth for failed requests.

### `OfflineBanner`

| Parameter | Default |
|---|---|
| `Message` | `"You are offline. Showing the last loaded data."` |
| `BackOnlineMessage` | `"Back online"` |
| `ShowBackOnlineToast` | `true` |
| `CssClass` | |

Slim fixed strip at the very top (`--fb-z-offline` 10030, respects `safe-area-inset-top`).

### `SessionExpiryDialog`

| Parameter | Default | Notes |
|---|---|---|
| `WarnBefore` | `2 min` | when the countdown appears |
| `LoginPath` | `"/login"` | |
| `Title`, `Message` | | copy |
| `OnContinue` | | **hook for the future refresh-token call** |
| `OnExpired` | | runs once before the redirect |

Method: `Reschedule()` (re-read `LoginState.Expiration` after setting it without an `OnChange`).

Implementation: one `System.Threading.Timer`, re-armed after each evaluation (10s polling outside the window, 1s ticks
inside it, so a `LoginState.Expiration` change is picked up within 10s even without `OnChange`). Subscribes to
`LoginState.OnChange`. No `async void`; timer callbacks marshal via `InvokeAsync` and swallow disposed-circuit errors.

* **Continue**: only hides the dialog for the current expiry. **There is no refresh-token endpoint yet**; the API team
  must add one (e.g. `POST api/authentication/refresh`) and the coordinator should call it from `OnContinue`, updating
  `LoginState.Token`, `LoginState.Expiration` and `SessionKeys.Expiration` in the session store. Until then the user
  is still signed out at expiry.
* **Sign out now**: `LoginState.Logout()`, `Expiration = default`, clears the Authorization header and the four
  `SessionKeys` entries, navigates to `LoginPath`.
* **On expiry**: navigates to `LoginPath`. `MainLayout`'s existing 5s expiry timer performs the storage cleanup; if
  that timer is ever removed, move its `AutoLogout` body into `OnExpired`.

---

## 3. z-index map (CSS variables, all overridable)

| Layer | Variable | Default |
|---|---|---|
| ActionBar (phone) | `--fb-z-actionbar` | 1030 |
| BottomSheet | `--fb-z-sheet` | 10000 |
| ConfirmDialog | `--fb-z-confirm` | 10010 |
| SessionExpiryDialog | `--fb-z-session` | 10015 |
| ToastHost | `--fb-z-toast` | 10020 |
| OfflineBanner | `--fb-z-offline` | 10030 |

Existing page drawers use 9998/9999 and `SendBackPopup` 99999; the sheet sits above the drawers so it can be adopted
incrementally, and toasts/banner are above both.

---

## 4. Testing checklist

1. `dotnet run --project SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web --launch-profile http` and open
   `http://localhost:5027/dev/feedback`.
2. Desktop width: toasts top-right, sheet = right drawer, action bar inline right-aligned, confirm centred.
3. DevTools device toolbar at 390px: sheet slides up with handle, drag it down to close, browser Back closes it
   (page stays), Escape closes it; action bar fixed at bottom, last form field still reachable; toasts above the bar;
   tick "Simulate tab bar" and both move up 56px.
4. DevTools → Network → Offline: banner appears; back Online: banner hides, "Back online" toast.
5. "Dialog in 10 s": countdown appears at 2:00, Continue hides it, "Expire in 20 s" redirects when it hits 0.
