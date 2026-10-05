"""Scroll-through screenshots of every route on the attached Android phone (portrait), for visual review.

The Android WebView does not support Page.captureScreenshot beyond the viewport, so each page is
scrolled in viewport-height steps and `adb exec-out screencap` is taken at every step. A reviewer
reads the numbered shots of a route in order to see the whole page, including what sits under the
sticky footer and the bottom tab bar. The app must already be signed in.

  adb forward tcp:9333 localabstract:webview_devtools_remote_<pid>
  python tools/ui-sweep/fullpage_device.py --port 9333 --out tools/ui-sweep/out/fullpage-phone
  python tools/ui-sweep/fullpage_device.py --filter "Sample|Consignment|Community|DigitalLibrary"
"""
from __future__ import annotations

import argparse
import json
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

# Finds the element that actually scrolls (html, body, or one of the shell wrappers) and
# scrolls it by one viewport at a time; returns the new offset and the total height.
SCROLL_JS = r"""
(step) => {
  const cands = [document.scrollingElement, document.body, document.documentElement,
                 document.querySelector('.app-shell'), document.querySelector('.content-wrap'),
                 document.querySelector('.main-content')].filter(Boolean);
  let el = cands.find(e => e.scrollHeight > e.clientHeight + 10) || document.scrollingElement;
  const total = el.scrollHeight, view = el.clientHeight || innerHeight;
  if (step === -1) { el.scrollTop = 0; window.scrollTo(0, 0); }
  else { el.scrollTop = el.scrollTop + step; window.scrollBy(0, step); }
  return { top: Math.round(el.scrollTop || window.scrollY), total, view, who: el.className || el.tagName };
}
"""


def slug(route: str) -> str:
    return re.sub(r"[^A-Za-z0-9]+", "_", route.strip("/")) or "root"


def screencap(adb: str, path: str) -> bool:
    with open(path, "wb") as f:
        r = subprocess.run([adb, "exec-out", "screencap", "-p"], stdout=f, stderr=subprocess.DEVNULL, timeout=60)
    return r.returncode == 0 and os.path.getsize(path) > 1000


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, default=9333)
    ap.add_argument("--adb", default=DEFAULT_ADB)
    ap.add_argument("--wait", type=float, default=3.5)
    ap.add_argument("--out", default=os.path.join(HERE, "out", "fullpage-phone"))
    ap.add_argument("--filter", metavar="REGEX")
    ap.add_argument("--routes-file")
    ap.add_argument("--max-shots", type=int, default=8, help="max screens per page")
    args = ap.parse_args()

    if args.routes_file:
        routes = [l.strip() for l in open(args.routes_file, encoding="utf-8") if l.strip() and not l.startswith("#")]
    else:
        routes, _ = routes_mod.derive_routes(route_filter=args.filter)

    os.makedirs(args.out, exist_ok=True)
    target = core.pick_page_target(args.port)
    cdp = core.CDP(target["webSocketDebuggerUrl"], timeout=30)
    origin = cdp.evaluate("location.origin")
    rows = []
    for i, route in enumerate(routes, 1):
        cdp.evaluate(f"location.href = {json.dumps(origin + route)}")
        time.sleep(args.wait)
        try:
            actual = cdp.evaluate("location.pathname + location.search")
            st = cdp.evaluate(f"({SCROLL_JS})(-1)")
        except Exception as ex:  # noqa: BLE001
            rows.append({"route": route, "error": str(ex)})
            print(f"{i:03d} {route:40s} ERROR {ex}")
            continue
        shots = []
        step = max(int(st["view"]) - 80, 200)   # overlap 80px so nothing is lost between shots
        k = 0
        while True:
            k += 1
            path = os.path.join(args.out, f"{i:03d}_{slug(route)}_{k}.png")
            time.sleep(0.4)
            if screencap(args.adb, path):
                shots.append(os.path.basename(path))
            if k >= args.max_shots:
                break
            before = st["top"]
            st = cdp.evaluate(f"({SCROLL_JS})({step})")
            time.sleep(0.5)
            if st["top"] <= before or st["top"] + st["view"] >= st["total"] - 4:
                # reached the end (take the final position once more if it moved)
                if st["top"] > before:
                    k += 1
                    path = os.path.join(args.out, f"{i:03d}_{slug(route)}_{k}.png")
                    time.sleep(0.4)
                    if screencap(args.adb, path):
                        shots.append(os.path.basename(path))
                break
        rows.append({"route": route, "actual": actual, "total": st["total"], "view": st["view"], "scroller": st["who"], "shots": shots})
        print(f"{i:03d} {route:40s} -> {actual:34s} h={st['total']} shots={len(shots)}")
    cdp.close()
    with open(os.path.join(args.out, "index.json"), "w", encoding="utf-8") as f:
        json.dump(rows, f, indent=1)
    print(f"{len(rows)} routes captured to {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
