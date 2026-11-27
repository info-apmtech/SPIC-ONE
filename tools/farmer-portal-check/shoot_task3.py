"""
Farmer portal Task 3 screenshots: the home weather strip (with and without GPS on the profile), Trends in
my area (with and without GPS), Community near me, the Community "Near Me" tab and a discussion opened
by a farmer, and the profile opened from "Use my location".

    python tools/farmer-portal-check/shoot_task3.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/task-3

LOCAL web host only (refuses other hosts). Signs in once per run as each of two LOCAL test farmers
through the devCode (each sign-in uses one of the number's 5 codes per hour): one WITHOUT a farm
location (default 6163665844) and one WITH (default 6614843176, Kovilpatti). Run seed_task2_local.sql
and seed_task3_local.sql first (demo dealers, staff, community posts). Changes no data.
"""
import argparse
import os
import sys

from playwright.sync_api import sync_playwright

sys.path.insert(0, os.path.dirname(__file__))
import shoot  # noqa: E402  (sign-in helpers and the shot() writer)


def sign_in(browser, base, phone, size):
    ctx = shoot.new_context(browser, size)
    page = ctx.new_page()
    shoot.farmer_sign_in(page, base, phone)
    storage = ctx.storage_state()
    ctx.close()
    return storage


def visit(page, base, url, ready, ms=1000):
    page.goto(base + url)
    page.wait_for_selector(ready, timeout=25000)
    shoot.settle(page, ms)


def screens(browser, base, out, storage, sizes, tag, extra):
    for size in sizes:
        ctx = shoot.new_context(browser, size, storage)
        page = ctx.new_page()

        visit(page, base, "/farmer", ".fpw:not(.fpw--loading), .fph-team-grid")
        page.wait_for_selector(".fpw:not(.fpw--loading)", timeout=20000)
        shoot.settle(page, 800)
        shoot.shot(page, out, f"home-{tag}-{size}")

        visit(page, base, "/farmer/trends", ".fptr-grid, .fp-error")
        shoot.shot(page, out, f"trends-{tag}-{size}")

        if extra:
            visit(page, base, "/farmer/community", ".fpcm-list, .fp-empty, .fp-error")
            shoot.shot(page, out, f"community-{size}")

            visit(page, base, "/Community/discussions?tab=near", ".cmd-tab.active", 1500)
            shoot.shot(page, out, f"community-near-tab-{size}", full=False)

            page.goto(base + "/farmer/community")
            page.wait_for_selector(".fpdc", timeout=20000)
            page.click(".fpdc >> nth=0")
            page.wait_for_url(lambda u: "/Community/discussion/" in u, timeout=20000)
            shoot.settle(page, 1500)
            shoot.shot(page, out, f"community-detail-{size}", full=False)
        else:
            # The "Use my location" button of the weather prompt opens the profile at "Farm location".
            page.goto(base + "/farmer")
            page.wait_for_selector(".fpw-prompt__btn", timeout=20000)
            page.click(".fpw-prompt__btn")
            page.wait_for_selector("#fpf-location", timeout=20000)
            shoot.settle(page, 1500)
            shoot.shot(page, out, f"profile-location-{size}", full=False)
        ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--phone", default="6163665844", help="local test farmer WITHOUT a farm location")
    ap.add_argument("--gps-phone", default="6614843176", help="local test farmer WITH a farm location")
    ap.add_argument("--sizes", default="phone,desktop")
    ap.add_argument("--only", choices=["gps", "nogps", "all"], default="all")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    shoot.refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        if args.only in ("gps", "all"):
            screens(browser, base, out, sign_in(browser, base, args.gps_phone, sizes[0]), sizes, "gps", extra=True)
        if args.only in ("nogps", "all"):
            screens(browser, base, out, sign_in(browser, base, args.phone, sizes[0]), sizes, "nogps", extra=False)
        browser.close()


if __name__ == "__main__":
    main()
