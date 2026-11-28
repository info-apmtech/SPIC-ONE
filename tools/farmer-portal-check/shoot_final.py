"""
Farmer portal final fix wave screenshots: the privacy notice, the account-deletion page, the login consent line, the
profile's "Your data" card and the "Delete my account" sheet, at 375x812 and 1280x800, from the LOCAL web host.

    python tools/farmer-portal-check/shoot_final.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/final

Same set-up as shoot.py (local API + local web host with --ApiBaseUrl=http://localhost:5120/). Signs in as the local
test farmer (default 6777777703) once; the delete sheet is only opened (and the "I understand" box ticked), never
confirmed, so no data changes.
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
    args = ap.parse_args()
    base = args.base.rstrip("/")
    refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = ["phone", "desktop"]

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)

        # Public pages, signed out.
        for size in sizes:
            ctx = new_context(browser, size)
            page = ctx.new_page()
            for name, url in (("privacy", "/farmer/privacy"), ("delete-account", "/farmer/delete-account"), ("login-consent", "/farmer/login")):
                page.goto(base + url)
                settle(page, 1200)
                shot(page, out, f"{name}-{size}", full=name != "login-consent")
            ctx.close()

        # Signed in: profile "Your data" card and the delete sheet.
        ctx = new_context(browser, "phone")
        page = ctx.new_page()
        farmer_sign_in(page, base, args.phone)
        storage = ctx.storage_state()
        ctx.close()
        for size in sizes:
            ctx = new_context(browser, size, storage)
            page = ctx.new_page()
            page.goto(base + "/farmer/profile")
            settle(page, 1800)
            card = page.locator(".fpp-data")
            card.scroll_into_view_if_needed()
            page.wait_for_timeout(400)
            shot(page, out, f"profile-your-data-{size}", full=False)
            page.click(".fpp-delete")
            page.wait_for_selector(".fpdel-confirm", timeout=10000)
            page.wait_for_timeout(600)
            shot(page, out, f"delete-sheet-{size}", full=False)
            page.check(".fpdel-confirm input")
            page.wait_for_timeout(300)
            sheet_body = page.locator(".fpdel-confirm")
            sheet_body.scroll_into_view_if_needed()
            page.wait_for_timeout(300)
            shot(page, out, f"delete-sheet-confirmed-{size}", full=False)
            ctx.close()
        browser.close()


if __name__ == "__main__":
    main()
