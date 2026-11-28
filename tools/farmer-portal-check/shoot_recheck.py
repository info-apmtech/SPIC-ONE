"""
Farmer portal recheck fixes: screenshots of the changed screens from the LOCAL web host (phone 375x812, desktop 1280x800,
and the tablet drawer at 768x1024).

    python tools/farmer-portal-check/shoot_recheck.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/recheck

- privacy notice and the public delete-account page (B7 wording), the "Delete my account" sheet (B7 lists);
- the farmer sign-in page reached by Log out (U5);
- the tablet rail's Menu drawer with the farmer tools (U6), and the same drawer on the phone;
- the leaf check (the web keeps the file-input camera; the Android button is MAUI-only, U2) and the SPIC AI page (U3).
Signs in as the local test farmer once; the delete sheet is only opened, never confirmed. Changes no data.
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from playwright.sync_api import sync_playwright  # noqa: E402
from shoot import farmer_sign_in, new_context, refuse_non_local, settle, shot  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--only", default="", help="comma list of shot names (the sign-in still runs)")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = ["phone", "desktop"]

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        for size in ([] if args.only else sizes):
            ctx = new_context(browser, size)
            page = ctx.new_page()
            for name, url in (("privacy", "/farmer/privacy"), ("delete-account", "/farmer/delete-account")):
                page.goto(base + url)
                settle(page, 1200)
                shot(page, out, f"{name}-{size}")
            ctx.close()

        ctx = new_context(browser, "phone")
        page = ctx.new_page()
        farmer_sign_in(page, base, args.phone)
        storage = ctx.storage_state()
        ctx.close()

        for size in ([] if args.only else sizes):
            ctx = new_context(browser, size, storage)
            page = ctx.new_page()
            page.goto(base + "/farmer/profile")
            settle(page, 1800)
            page.click(".fpp-delete")
            page.wait_for_selector(".fpdel-confirm", timeout=10000)
            page.wait_for_timeout(600)
            shot(page, out, f"delete-sheet-{size}", full=False)
            page.goto(base + "/farmer/leaf-check")
            settle(page, 1500)
            shot(page, out, f"leaf-check-{size}", full=False)
            page.goto(base + "/farmer/assistant")
            settle(page, 1500)
            shot(page, out, f"assistant-{size}", full=False)
            ctx.close()

        # U6: the tablet rail's Menu opens the drawer with the farmer tools; the phone's full menu is the same drawer.
        for size in (() if args.only else ("tablet", "phone")):
            ctx = new_context(browser, size, storage)
            page = ctx.new_page()
            page.goto(base + "/farmer/leaf-check")
            settle(page, 1500)
            page.evaluate("() => { const el = document.getElementById('mobileSidebar'); "
                          "if (el && window.bootstrap) bootstrap.Offcanvas.getOrCreateInstance(el).show(); }")
            page.wait_for_timeout(900)
            shot(page, out, f"menu-drawer-{size}", full=False)
            ctx.close()

        # U5: Log out from the farmer shell lands on the farmer sign-in page (phone: More sheet; desktop: account menu).
        for size in sizes:
            if args.only and f"after-logout-{size}" not in args.only:
                continue
            ctx = new_context(browser, size, storage)
            page = ctx.new_page()
            page.goto(base + "/farmer")
            settle(page, 1500)
            if size == "desktop":
                page.click("button.user-menu-toggle")
                page.wait_for_timeout(500)
                page.click("button.reference-logout")
            else:
                page.click("button:has-text('More')")
                page.wait_for_timeout(600)
                page.locator("button:has-text('Log out')").last.click()
            page.wait_for_timeout(2500)
            print("  after log out:", page.url)
            shot(page, out, f"after-logout-{size}", full=False)
            ctx.close()
        browser.close()


if __name__ == "__main__":
    main()
