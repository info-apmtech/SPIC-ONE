"""Browser UI sweep for the SPIC ONE Blazor Server web host at several viewport widths.

    set SPICONE_TEST_USER=...  &  set SPICONE_TEST_PASS=...      (PowerShell: $env:SPICONE_TEST_USER='...')
    python tools/ui-sweep/sweep_web.py                                    # 375, 768, 1280, 1920 against http://localhost:5027
    python tools/ui-sweep/sweep_web.py --widths 375 1280 --out out/web-before
    python tools/ui-sweep/sweep_web.py --base-url https://spicone.in --widths 1920 --filter "Dashboard|Stock"

For each width a fresh headless Chrome/Edge (found in the usual install paths, or --browser <exe>)
is launched with --remote-debugging-port and driven over the DevTools protocol. The script
logs in through the real login form (username/password inputs inside `.login-card`, submit
`button.btn-login`), then visits every route with Blazor.navigateTo, evaluates the metrics
script, captures a screenshot and records JS exceptions, console errors and failed network
requests. Without credentials only the public routes (/login, /privacy) are checked.

Output: <out>/results.json, <out>/report.md (one column per width), <out>/shots/<WxH>/*.png
Exit codes: 0 no flags, 1 at least one flag, 2 setup failure (no browser, server unreachable,
login failed).
"""
from __future__ import annotations

import argparse
import base64
import datetime as dt
import json
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import routes as routes_mod  # noqa: E402
import sweepcore as core  # noqa: E402

DEFAULT_WIDTHS = {375: 812, 768: 1024, 1280: 800, 1920: 1080}
PUBLIC_ROUTES = ("/login", "/privacy")
BROWSER_PATHS = {
    "chrome": [
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        os.path.join(os.environ.get("LOCALAPPDATA", ""), r"Google\Chrome\Application\chrome.exe"),
    ],
    "edge": [
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    ],
}

LOGIN_READY_JS = "!!document.querySelector('.login-card input[type=text]') || !!document.querySelector('.app-shell')"
LOGIN_FILL_JS = """(() => {
  const card = document.querySelector('.login-card');
  if (!card) return 'no-card';
  const inputs = card.querySelectorAll('input.form-control-custom');
  const user = card.querySelector('input[type=text].form-control-custom') || inputs[0];
  const pass = card.querySelector('input[type=password]') || inputs[1];
  if (!user || !pass) return 'no-inputs';
  for (const [el, v] of [[user, %s], [pass, %s]]) {
    el.focus();
    el.value = v;
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    el.blur();
  }
  return 'filled';
})()"""
LOGIN_SUBMIT_JS = """(() => {
  const btn = document.querySelector('.login-card button.btn-login');
  if (!btn) return 'no-button';
  btn.click();
  return 'clicked';
})()"""
LOGIN_ERROR_JS = """(() => {
  const el = document.querySelector('.login-card div[style*="red"], .login-card .validation-summary-errors, .login-card .validation-message');
  return el ? el.innerText.trim() : '';
})()"""


def fail(msg: str, code: int = 2) -> int:
    print(f"ERROR: {msg}", file=sys.stderr)
    return code


def find_browser(choice: str) -> tuple[str, str]:
    if choice not in ("auto", "chrome", "edge"):
        if os.path.isfile(choice):
            return choice, os.path.splitext(os.path.basename(choice))[0]
        raise FileNotFoundError(choice)
    order = ["chrome", "edge"] if choice == "auto" else [choice]
    for name in order:
        for p in BROWSER_PATHS[name]:
            if p and os.path.isfile(p):
                return p, name
    raise FileNotFoundError("no Chrome or Edge found in the usual install paths; pass --browser <path to exe>")


def free_port(preferred: int) -> int:
    for port in range(preferred, preferred + 50):
        with socket.socket() as s:
            try:
                s.bind(("127.0.0.1", port))
                return port
            except OSError:
                continue
    raise RuntimeError("no free port for remote debugging")


def server_reachable(url: str, timeout: float = 5) -> tuple[bool, str]:
    try:
        with urllib.request.urlopen(url, timeout=timeout) as r:
            return True, f"HTTP {r.status}"
    except urllib.error.HTTPError as e:
        return True, f"HTTP {e.code}"
    except Exception as exc:
        return False, str(exc)


class Browser:
    def __init__(self, exe: str, width: int, height: int, port: int, headless: bool):
        self.exe, self.width, self.height, self.port = exe, width, height, port
        self.profile = tempfile.mkdtemp(prefix=f"spic-sweep-{width}-")
        args = [
            exe,
            f"--remote-debugging-port={port}",
            f"--window-size={width},{height}",
            f"--user-data-dir={self.profile}",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-extensions",
            "--disable-background-networking",
            "--disable-sync",
            "--hide-scrollbars",
            "--disable-gpu",
            "about:blank",
        ]
        if headless:
            args.insert(1, "--headless=new")
        self.proc = subprocess.Popen(args, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        deadline = time.time() + 30
        while time.time() < deadline:
            try:
                core.list_targets(port)
                return
            except Exception:
                if self.proc.poll() is not None:
                    raise RuntimeError(f"browser exited early with code {self.proc.returncode}")
                time.sleep(0.3)
        raise RuntimeError(f"browser did not open DevTools port {port} within 30s")

    def close(self) -> None:
        try:
            self.proc.terminate()
            self.proc.wait(timeout=10)
        except Exception:
            try:
                self.proc.kill()
            except Exception:
                pass
        for _ in range(10):
            try:
                shutil.rmtree(self.profile)
                break
            except Exception:
                time.sleep(0.5)


def open_app(cdp: core.CDP, base_url: str, timeout: float) -> bool:
    """Loads /login and waits for the Blazor circuit to render the form (or the shell)."""
    cdp.send("Page.navigate", {"url": base_url.rstrip("/") + "/login"})
    return cdp.wait_for(LOGIN_READY_JS, timeout)


def login(cdp: core.CDP, user: str, password: str, timeout: float) -> tuple[bool, str]:
    """Fills and submits the already rendered login form; returns (ok, message)."""
    if cdp.evaluate("!!document.querySelector('.app-shell')"):
        return True, "already authenticated (session restored)"
    state = cdp.evaluate(LOGIN_FILL_JS % (json.dumps(user), json.dumps(password)))
    if state != "filled":
        return False, f"could not fill the login form: {state}"
    cdp.drain(0.8)  # let Blazor receive the change events
    state = cdp.evaluate(LOGIN_SUBMIT_JS)
    if state != "clicked":
        return False, f"could not submit the login form: {state}"
    deadline = time.time() + timeout
    while time.time() < deadline:
        cdp.drain(0.5)
        path = core.normalize_path(cdp.evaluate("location.pathname"))
        if path not in ("/login", "/"):
            return True, f"landed on {path}"
        err = cdp.evaluate(LOGIN_ERROR_JS)
        if err:
            return False, f"login form error: {err}"
    return False, f"still on /login after {timeout}s"


def sweep_width(args, exe: str, width: int, height: int, routes: list[str], creds: tuple[str, str] | None) -> tuple[list[dict], dict]:
    label = f"{width}x{height}"
    port = free_port(args.port)
    browser = Browser(exe, width, height, port, headless=not args.no_headless)
    events = core.EventLog()
    rows: list[dict] = []
    info: dict = {"label": label, "port": port}
    shots_dir = os.path.join(args.out, "shots", label)
    os.makedirs(shots_dir, exist_ok=True)
    try:
        target = core.pick_page_target(port)
        cdp = core.CDP(target["webSocketDebuggerUrl"], on_event=events.handle)
        try:
            cdp.send("Page.enable")
            cdp.send("Runtime.enable")
            cdp.send("Page.enable")
            cdp.send("Log.enable")
            cdp.send("Network.enable")
            if not open_app(cdp, args.base_url, args.login_timeout):
                raise RuntimeError(f"[{label}] the app did not render within {args.login_timeout}s at {args.base_url} "
                                   "(is the Blazor circuit connecting? check the server console)")
            # The viewport override goes on AFTER the first navigation: set on about:blank,
            # Page.navigate to another origin never completes in headless Chrome. Headless
            # windows also have a minimum width (~500 px), so --window-size alone cannot give 375.
            cdp.send("Emulation.setDeviceMetricsOverride", {
                "width": width, "height": height, "deviceScaleFactor": 1, "mobile": width < 768,
            })
            cdp.drain(0.8)
            actual = cdp.evaluate("innerWidth")
            if actual != width:
                print(f"[{label}] WARNING: innerWidth is {actual}, expected {width}", file=sys.stderr)
            authed = False
            if creds:
                ok, msg = login(cdp, creds[0], creds[1], args.login_timeout)
                info["login"] = msg
                print(f"[{label}] login: {msg}")
                if not ok:
                    raise RuntimeError(f"login failed at {label}: {msg}")
                authed = True
            info["authenticated"] = authed
            for i, route in enumerate(routes):
                row = core.sweep_route(cdp, route, args.wait, events, base_url=args.base_url, authed=authed)
                row["label"] = label
                row["width"], row["height"] = width, height
                if not args.no_screenshots:
                    params = {"format": "png"}
                    grew = False
                    if args.full_page:
                        # The app scrolls inside .app-shell / .content-wrap, not the document, so the
                        # document height is just the viewport. Measure the tallest scroller and grow
                        # the emulated viewport to it: the inner scroller then shows everything and
                        # fixed bars sit at the true bottom.
                        try:
                            h = int(cdp.evaluate(
                                "Math.max(...[document.scrollingElement, document.body, document.documentElement,"
                                " document.querySelector('.app-shell'), document.querySelector('.content-wrap'),"
                                " document.querySelector('.main-content')].filter(Boolean).map(e => e.scrollHeight))"))
                        except Exception:
                            h = int(row["metrics"].get("scrollH") or height)
                        h = max(height, min(h, 16000))
                        if h > height:
                            cdp.send("Emulation.setDeviceMetricsOverride", {
                                "width": width, "height": h, "deviceScaleFactor": 1, "mobile": width < 768})
                            cdp.drain(0.6)
                            grew = True
                        params.update({"captureBeyondViewport": True,
                                       "clip": {"x": 0, "y": 0, "width": width, "height": h, "scale": 1}})
                    try:
                        data = cdp.send("Page.captureScreenshot", params)["data"]
                        if grew:
                            cdp.send("Emulation.setDeviceMetricsOverride", {
                                "width": width, "height": height, "deviceScaleFactor": 1, "mobile": width < 768})
                            cdp.drain(0.3)
                        shot = os.path.join("shots", label, f"{i:03d}_{core.slug(route)}.png")
                        with open(os.path.join(args.out, shot), "wb") as f:
                            f.write(base64.b64decode(data))
                        row["screenshot"] = shot
                    except Exception as exc:
                        row["screenshotError"] = str(exc)
                rows.append(row)
                core.print_row(i, row)
        finally:
            cdp.close()
    finally:
        browser.close()
    return rows, info


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base-url", default=os.environ.get("SPICONE_BASE_URL", "http://localhost:5027"))
    ap.add_argument("--widths", nargs="+", type=int, default=list(DEFAULT_WIDTHS),
                    help="viewport widths; heights come from the presets 375x812 768x1024 1280x800 1920x1080 or WxH via --size")
    ap.add_argument("--size", nargs="+", metavar="WxH", default=[], help="explicit viewport sizes, e.g. --size 1366x768")
    ap.add_argument("--browser", default="auto", help="auto | chrome | edge | path to the browser exe")
    ap.add_argument("--port", type=int, default=9223, help="first remote-debugging port to try")
    ap.add_argument("--wait", type=float, default=3.0, help="seconds to wait after each navigation")
    ap.add_argument("--login-timeout", type=float, default=40.0)
    ap.add_argument("--out", default=os.path.join(HERE, "out", "web"), help="output folder (default tools/ui-sweep/out/web)")
    ap.add_argument("--filter", metavar="REGEX", help="only routes matching this regex")
    ap.add_argument("--routes-file", help="text file with one route per line (default: derive from source)")
    ap.add_argument("--include-params", metavar="JSON", help="sample ids for parameterised routes (see routes.py)")
    ap.add_argument("--user", default=os.environ.get("SPICONE_TEST_USER"), help=argparse.SUPPRESS)
    ap.add_argument("--full-page", action="store_true", help="capture the whole scroll height instead of the viewport")
    ap.add_argument("--no-screenshots", action="store_true")
    ap.add_argument("--no-headless", action="store_true", help="show the browser window (debugging)")
    args = ap.parse_args(argv)

    sizes: list[tuple[int, int]] = []
    for w in args.widths:
        sizes.append((w, DEFAULT_WIDTHS.get(w, round(w * 0.625))))
    for s in args.size:
        m = re.fullmatch(r"(\d+)x(\d+)", s)
        if not m:
            return fail(f"bad --size {s}; use WxH")
        sizes.append((int(m.group(1)), int(m.group(2))))
    if args.size and args.widths == list(DEFAULT_WIDTHS):
        sizes = sizes[len(DEFAULT_WIDTHS):]  # --size alone replaces the presets

    user = args.user
    password = os.environ.get("SPICONE_TEST_PASS")
    creds = (user, password) if user and password else None

    if args.routes_file:
        with open(args.routes_file, encoding="utf-8") as f:
            routes = [l.strip() for l in f if l.strip() and not l.startswith("#")]
        if args.filter:
            rx = re.compile(args.filter, re.IGNORECASE)
            routes = [r for r in routes if rx.search(r)]
    else:
        routes, _ = routes_mod.derive_routes(include_params=args.include_params, route_filter=args.filter)
    if not creds:
        print("SPICONE_TEST_USER / SPICONE_TEST_PASS not set: skipping login, checking public routes only "
              f"({', '.join(PUBLIC_ROUTES)})", file=sys.stderr)
        routes = [r for r in routes if r.lower() in PUBLIC_ROUTES] or list(PUBLIC_ROUTES)
    if not routes:
        return fail("no routes selected")

    try:
        exe, browser_name = find_browser(args.browser)
    except FileNotFoundError as exc:
        return fail(str(exc))
    ok, status = server_reachable(args.base_url)
    if not ok:
        return fail(f"{args.base_url} is not reachable ({status}); start the web app first: "
                    "dotnet run --project SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web --launch-profile http")

    args.out = os.path.abspath(args.out)
    os.makedirs(args.out, exist_ok=True)
    print(f"{browser_name} at {exe}; {args.base_url} ({status}); {len(routes)} routes; sizes {', '.join(f'{w}x{h}' for w, h in sizes)}")

    all_rows: list[dict] = []
    infos: list[dict] = []
    labels: list[str] = []
    for w, h in sizes:
        try:
            rows, info = sweep_width(args, exe, w, h, routes, creds)
        except Exception as exc:
            return fail(str(exc))
        all_rows.extend(rows)
        infos.append(info)
        labels.append(info["label"])

    meta = {
        "kind": "web",
        "generated": dt.datetime.now().isoformat(timespec="seconds"),
        "baseUrl": args.base_url,
        "browser": f"{browser_name} ({exe})",
        "authenticated": bool(creds),
        "waitSeconds": args.wait,
        "commit": core.git_commit_short(routes_mod.REPO_ROOT),
        "viewports": infos,
    }
    core.write_results(args.out, meta, all_rows)
    report = core.write_report(args.out, meta, all_rows, labels)
    flagged = sum(1 for r in all_rows if r["flags"])
    print(f"\n{flagged}/{len(all_rows)} checks flagged. Report: {report}")
    return 1 if flagged else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        raise SystemExit(130)
