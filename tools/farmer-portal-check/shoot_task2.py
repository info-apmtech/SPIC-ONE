"""
Farmer portal Task 2 screenshots: home with the SPIC team block, dealers (district, nearby, map,
empty search), my SPIC team, the call-back sheet, my requests, and the staff /farmer-requests page.

    python tools/farmer-portal-check/shoot_task2.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/task-2

LOCAL web host only (refuses other hosts), signed in as the local test farmer through the devCode
(one OTP per run) and as the local QA staff users (run seed_task2_local.sql first: it adds the demo
dealers / staff and activates qa.jmdo / qa.admin). The farmer's device location is faked to
Kovilpatti (9.17, 77.87) for "Use my location". The one request this script sends carries the
message prefix "[demo]" (cleanup_task2_local.sql deletes it).
"""
import argparse
import json
import os
import sys

from playwright.sync_api import sync_playwright

sys.path.insert(0, os.path.dirname(__file__))
import shoot  # noqa: E402  (reuse the sign-in helpers and the shot() writer)

KOVILPATTI = {"latitude": 9.1717, "longitude": 77.8691}


def context(browser, size, storage=None):
    w, h, mobile = shoot.SIZES[size]
    return browser.new_context(viewport={"width": w, "height": h}, is_mobile=mobile, has_touch=mobile,
                               device_scale_factor=1, locale="en-IN", reduced_motion="reduce",
                               storage_state=storage, timezone_id="Asia/Kolkata",
                               geolocation=KOVILPATTI, permissions=["geolocation"])


def farmer_screens(browser, base, out, phone, sizes):
    ctx = context(browser, sizes[0])
    page = ctx.new_page()
    shoot.farmer_sign_in(page, base, phone)
    storage = ctx.storage_state()
    ctx.close()

    for size in sizes:
        ctx = context(browser, size, storage)
        page = ctx.new_page()

        page.goto(base + "/farmer")
        page.wait_for_selector(".fph-team-grid, .fph-team .fp-empty", timeout=20000)
        shoot.settle(page, 1200)
        shoot.shot(page, out, f"home-{size}")

        # Dealers: the test farmer has no saved farm location, so the district list comes first.
        page.goto(base + "/farmer/dealers")
        page.wait_for_selector(".fpd-list, .fp-empty", timeout=20000)
        shoot.settle(page, 900)
        shoot.shot(page, out, f"dealers-district-{size}")

        page.click(".fpd-where__btn")
        page.wait_for_selector(".fpdc__distance", timeout=20000)
        shoot.settle(page, 900)
        shoot.shot(page, out, f"dealers-nearby-{size}")

        page.click(".fpd-view .fpd-seg__btn:has-text('Map')")
        page.wait_for_selector(".fpd-map .leaflet-marker-icon", timeout=20000)
        page.wait_for_timeout(2500)  # tiles
        page.click(".fpd-map .fpd-pin--dealer >> nth=0")
        page.wait_for_timeout(900)
        shoot.shot(page, out, f"dealers-map-{size}", full=False)

        page.click(".fpd-view .fpd-seg__btn:has-text('List')")
        page.fill("#fpd-q", "Coimbatore")
        page.click(".fpd-search__go")
        page.wait_for_selector(".fp-empty", timeout=20000)
        shoot.settle(page, 600)
        shoot.shot(page, out, f"dealers-search-empty-{size}", full=False)

        page.goto(base + "/farmer/team")
        page.wait_for_selector(".fpt-grid, .fp-empty", timeout=20000)
        shoot.settle(page, 900)
        shoot.shot(page, out, f"team-{size}")

        # The call-back sheet: pick a topic, type a message, choose a time.
        page.click(".fptm__ask >> nth=0")
        page.wait_for_selector(".fb-sheet__panel", timeout=10000)
        page.wait_for_timeout(500)
        page.click(".fb-sheet button.fp-chip:has-text('Crop problem')")
        page.fill("#fpcb-message", "[demo] Leaves of my paddy are turning yellow near the tips.")
        page.click(".fpcb-time:has-text('Morning')")
        page.wait_for_timeout(400)
        shoot.shot(page, out, f"request-sheet-{size}", full=False)

        if size == sizes[0]:
            page.click(".fpcb-send")
            page.wait_for_selector(".fpcb-done", timeout=15000)
            page.wait_for_timeout(500)
            shoot.shot(page, out, f"request-sent-{size}", full=False)
            page.click(".fpcb-footer .fp-btn--primary")
            page.wait_for_timeout(600)
        else:
            page.keyboard.press("Escape")
            page.wait_for_timeout(400)

        page.goto(base + "/farmer/requests")
        page.wait_for_selector(".fpr-list, .fp-empty", timeout=20000)
        shoot.settle(page, 900)
        shoot.shot(page, out, f"my-requests-{size}")
        ctx.close()


def staff_screens(browser, base, out, password, sizes):
    for user, prefix in (("qa.jmdo", "staff-requests"), ("qa.admin", "staff-requests-admin")):
        for size in sizes:
            ctx = context(browser, size)
            page = ctx.new_page()
            shoot.staff_sign_in(page, base, user, password)
            page.goto(base + "/farmer-requests")
            page.wait_for_selector(".frq-list, .spic-empty", timeout=20000)
            shoot.settle(page, 1200)
            shoot.shot(page, out, f"{prefix}-{size}")
            if user == "qa.jmdo" and page.query_selector(".frq-btn"):
                page.click(".frq-btn >> nth=0")
                page.wait_for_selector(".fb-sheet__panel", timeout=10000)
                page.fill("#frq-note", "Called the farmer, will visit the field on Monday.")
                page.wait_for_timeout(500)
                shoot.shot(page, out, f"staff-update-sheet-{size}", full=False)
            ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--sizes", default="phone,desktop")
    ap.add_argument("--set", default="all", choices=["farmer", "staff", "all"])
    args = ap.parse_args()
    base = args.base.rstrip("/")
    shoot.refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")
    with open(os.path.join(shoot.ROOT, "tools", "help-demo", "shots.json"), encoding="utf-8") as f:
        password = json.load(f)["password"]

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        if args.set in ("farmer", "all"):
            farmer_screens(browser, base, out, args.phone, sizes)
        if args.set in ("staff", "all"):
            staff_screens(browser, base, out, password, sizes)
        browser.close()


if __name__ == "__main__":
    main()
