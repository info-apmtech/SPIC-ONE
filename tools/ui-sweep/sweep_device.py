"""Android UI sweep for the SPIC ONE MAUI app through the WebView's Chrome DevTools socket.

    python tools/ui-sweep/sweep_device.py
    python tools/ui-sweep/sweep_device.py --package com.apmiot.spicone --wait 4 --out out/device --filter "Dashboard|Stock"

What it does
  1. adb forward tcp:<port> localabstract:webview_devtools_remote_<pid of package>
  2. connects to the first page target, enables Runtime/Log events
  3. for every route: Blazor.navigateTo(route), waits, evaluates the metrics script,
     captures a screenshot with `adb exec-out screencap -p`
  4. writes results.json, report.md and shots/*.png to the output folder

Preconditions: the app is running in the foreground and already LOGGED IN (the sweep
does not type credentials on the device); USB debugging is on. The WebView debug socket
is only present when the app was built with WebView debugging enabled (Debug builds).

Exit codes: 0 no flags, 1 at least one flag, 2 setup failure (no device, app not running,
no DevTools page).
"""
from __future__ import annotations

import argparse
import datetime as dt
import os
import re
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import routes as routes_mod  # noqa: E402
import sweepcore as core  # noqa: E402

DEFAULT_ADB = r"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"


def find_adb(explicit: str | None) -> str:
    # An explicit path (flag or ADB env var) must exist: never fall back silently to another adb.
    for given in (explicit, os.environ.get("ADB")):
        if given:
            if os.path.isfile(given):
                return given
            raise FileNotFoundError(given)
    candidates = [DEFAULT_ADB]
    sdk = os.environ.get("ANDROID_HOME") or os.environ.get("ANDROID_SDK_ROOT")
    if sdk:
        candidates.append(os.path.join(sdk, "platform-tools", "adb.exe"))
    candidates.append(os.path.join(os.environ.get("LOCALAPPDATA", ""), "Android", "Sdk", "platform-tools", "adb.exe"))
    for c in candidates:
        if c and os.path.isfile(c):
            return c
    return "adb"  # rely on PATH


def adb(adb_exe: str, serial: str | None, *args: str, timeout: float = 20, binary: bool = False):
    cmd = [adb_exe] + (["-s", serial] if serial else []) + list(args)
    return subprocess.run(cmd, capture_output=True, timeout=timeout, text=not binary)


def fail(msg: str, code: int = 2) -> int:
    print(f"ERROR: {msg}", file=sys.stderr)
    return code


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--package", default="com.apmiot.spicone", help="Android application id")
    ap.add_argument("--serial", help="adb device serial (when more than one device is attached)")
    ap.add_argument("--adb", help="path to adb.exe (default: the Android SDK platform-tools copy)")
    ap.add_argument("--port", type=int, default=9222, help="local TCP port forwarded to the WebView socket")
    ap.add_argument("--wait", type=float, default=3.5, help="seconds to wait after each navigation")
    ap.add_argument("--out", default=os.path.join(HERE, "out", "device"), help="output folder (default tools/ui-sweep/out/device)")
    ap.add_argument("--filter", metavar="REGEX", help="only routes matching this regex")
    ap.add_argument("--routes-file", help="text file with one route per line (default: derive from source)")
    ap.add_argument("--include-params", metavar="JSON", help="sample ids for parameterised routes (see routes.py)")
    ap.add_argument("--no-screenshots", action="store_true")
    ap.add_argument("--keep-forward", action="store_true", help="leave the adb forward in place afterwards")
    args = ap.parse_args(argv)

    # ---- routes -----------------------------------------------------------
    if args.routes_file:
        with open(args.routes_file, encoding="utf-8") as f:
            routes = [l.strip() for l in f if l.strip() and not l.startswith("#")]
        if args.filter:
            rx = re.compile(args.filter, re.IGNORECASE)
            routes = [r for r in routes if rx.search(r)]
    else:
        routes, _ = routes_mod.derive_routes(include_params=args.include_params, route_filter=args.filter)
    if not routes:
        return fail("no routes selected")

    # ---- adb / device -----------------------------------------------------
    try:
        adb_exe = find_adb(args.adb)
    except FileNotFoundError as exc:
        return fail(f"adb not found: {exc}")
    try:
        r = adb(adb_exe, None, "devices")
    except FileNotFoundError:
        return fail(f"adb not found ({adb_exe}); pass --adb or set ANDROID_HOME")
    except subprocess.TimeoutExpired:
        return fail("adb did not respond (is the adb server hung? try `adb kill-server`)")
    devices = [l.split("\t")[0] for l in r.stdout.splitlines()[1:] if "\tdevice" in l]
    if not devices:
        return fail("no Android device in `adb devices` (enable USB debugging and accept the prompt on the phone)")
    serial = args.serial
    if serial and serial not in devices:
        return fail(f"device {serial} is not attached; attached: {', '.join(devices)}")
    if not serial:
        if len(devices) > 1:
            return fail(f"more than one device attached; pass --serial one of: {', '.join(devices)}")
        serial = devices[0]

    r = adb(adb_exe, serial, "shell", "pidof", args.package)
    pid = r.stdout.strip().split()[0] if r.returncode == 0 and r.stdout.strip() else ""
    if not pid:
        return fail(f"{args.package} is not running on {serial}; open the app and log in first")

    socket_name = f"webview_devtools_remote_{pid}"
    r = adb(adb_exe, serial, "shell", "cat", "/proc/net/unix")
    if r.returncode == 0 and socket_name not in r.stdout:
        return fail(f"WebView debug socket {socket_name} not present; the app must be a Debug build "
                    "(WebView.setWebContentsDebuggingEnabled) and in the foreground")

    r = adb(adb_exe, serial, "forward", f"tcp:{args.port}", f"localabstract:{socket_name}")
    if r.returncode != 0:
        return fail(f"adb forward failed: {r.stderr.strip() or r.stdout.strip()}")

    try:
        target = core.pick_page_target(args.port)
    except Exception as exc:
        return fail(f"no DevTools page on port {args.port}: {exc}")

    # ---- sweep -------------------------------------------------------------
    events = core.EventLog()
    cdp = core.CDP(target["webSocketDebuggerUrl"], on_event=events.handle)
    out_dir = os.path.abspath(args.out)
    shots_dir = os.path.join(out_dir, "shots")
    os.makedirs(shots_dir, exist_ok=True)
    rows: list[dict] = []
    try:
        cdp.send("Runtime.enable")
        cdp.send("Page.enable")
        try:
            cdp.send("Log.enable")
        except Exception:
            pass
        start_path = cdp.evaluate("location.pathname")
        model = adb(adb_exe, serial, "shell", "getprop", "ro.product.model").stdout.strip()
        if core.normalize_path(start_path) in ("/", "/login"):
            print("WARNING: the app is on the login page; protected routes will show REDIRECT. Log in on the phone first.",
                  file=sys.stderr)
        authed = core.normalize_path(start_path) not in ("/", "/login")
        print(f"device {serial} ({model}) pid {pid} page '{target.get('title')}' at {start_path}; {len(routes)} routes")

        for i, route in enumerate(routes):
            row = core.sweep_route(cdp, route, args.wait, events, authed=authed)
            row["label"] = "device"
            if not args.no_screenshots:
                shot = os.path.join("shots", f"{i:03d}_{core.slug(route)}.png")
                try:
                    png = adb(adb_exe, serial, "exec-out", "screencap", "-p", timeout=30, binary=True)
                    if png.returncode == 0 and png.stdout:
                        with open(os.path.join(out_dir, shot), "wb") as f:
                            f.write(png.stdout)
                        row["screenshot"] = shot
                except Exception as exc:
                    row["screenshotError"] = str(exc)
            rows.append(row)
            core.print_row(i, row)
    finally:
        cdp.close()
        if not args.keep_forward:
            adb(adb_exe, serial, "forward", "--remove", f"tcp:{args.port}")

    meta = {
        "kind": "device",
        "generated": dt.datetime.now().isoformat(timespec="seconds"),
        "package": args.package,
        "device": f"{serial} {model}".strip(),
        "authenticated": authed,
        "waitSeconds": args.wait,
        "commit": core.git_commit_short(routes_mod.REPO_ROOT),
        "viewport": {"width": rows[0]["metrics"].get("iw") if rows else None,
                     "height": rows[0]["metrics"].get("ih") if rows else None},
    }
    core.write_results(out_dir, meta, rows)
    report = core.write_report(out_dir, meta, rows, ["device"])
    flagged = sum(1 for r in rows if r["flags"])
    print(f"\n{flagged}/{len(rows)} routes flagged. Report: {report}")
    return 1 if flagged else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        raise SystemExit(130)
