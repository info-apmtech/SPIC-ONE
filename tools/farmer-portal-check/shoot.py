"""
Farmer portal screenshots (docs/farmer-portal.md, "Design system").

Photographs farmer portal screens from the LOCAL web host (signed in as a local test farmer via the
Development-only devCode) at 375x812 (phone) and 1280x800 (desktop), with the installed Microsoft
Edge. Also shoots staff pages for before/after "nothing changed" checks.

    python tools/farmer-portal-check/shoot.py --out .superpowers/sdd/<plan>/shots/task-1b [--set farmer|staff|all]

Needs: the local API (`dotnet run --project SpicAPI --urls http://localhost:5120`, Development,
local spicone_dev) and the local web host started with `--ApiBaseUrl=http://localhost:5120/`.
Refuses any base URL that is not localhost. The farmer number must be a LOCAL test farmer whose
profile is complete (default 6777777703); a fresh random 6xxxxxxxxx number is used for the
onboarding screens. Staff shots sign in with the local QA user/password from tools/help-demo/shots.json.

Each OTP request counts against the number's hourly limit (5 per hour): this script signs in once
per run and reuses the browser storage for both sizes.
"""
import argparse
import json
import os
import random
import re
import sys
from urllib.parse import urlparse

from playwright.sync_api import sync_playwright

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SIZES = {"phone": (375, 812, True), "tablet": (768, 1024, False), "desktop": (1280, 800, False)}


def refuse_non_local(base):
    host = urlparse(base).hostname or ""
    if host not in ("localhost", "127.0.0.1", "::1"):
        sys.exit(f"Refusing {base}: screenshots come from the LOCAL web host only.")


def settle(page, ms=900):
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(ms)


def shot(page, out, name, full=True):
    """Viewport picture, plus a -full picture when the page is taller than the viewport."""
    path = os.path.join(out, f"{name}.png")
    page.screenshot(path=path)
    print("  saved", path)
    if not full:
        return
    height = page.evaluate(
        "() => Math.max(document.scrollingElement.scrollHeight, ...[...document.querySelectorAll('.main-content, .content, main')].map(e => e.scrollHeight))")
    vp = page.viewport_size
    if height > vp["height"] + 40:
        page.set_viewport_size({"width": vp["width"], "height": min(height, 9000)})
        page.wait_for_timeout(400)
        fpath = os.path.join(out, f"{name}-full.png")
        page.screenshot(path=fpath, full_page=True)
        print("  saved", fpath)
        page.set_viewport_size(vp)
        page.wait_for_timeout(200)


def farmer_sign_in(page, base, phone):
    page.goto(base + "/farmer/login")
    settle(page)
    page.fill("#fpl-phone", phone)
    page.click("button[type=submit]")
    page.wait_for_selector("[data-dev-code], .fpl-dev strong", timeout=15000)
    el = page.query_selector("[data-dev-code]")
    code = el.get_attribute("data-dev-code") if el else page.inner_text(".fpl-dev strong")
    code = re.sub(r"\D", "", code)
    first = page.query_selector(".fp-otp input") or page.query_selector("#fpl-code")
    first.click()
    page.keyboard.type(code, delay=60)
    page.wait_for_url(lambda u: "/farmer/login" not in u, timeout=20000)
    settle(page, 1200)


def staff_sign_in(page, base, user, password):
    page.goto(base + "/login")
    settle(page)
    page.fill("input[placeholder='User Name']", user)
    page.fill("input[type=password]", password)
    page.click("button[type=submit]")
    page.wait_for_url(lambda u: "/login" not in u.lower() and urlparse(u).path != "/", timeout=20000)
    settle(page, 1500)


def new_context(browser, size, storage=None):
    w, h, mobile = SIZES[size]
    return browser.new_context(viewport={"width": w, "height": h}, is_mobile=mobile, has_touch=mobile,
                               device_scale_factor=1, locale="en-IN", reduced_motion="reduce",
                               storage_state=storage, timezone_id="Asia/Kolkata")


def shoot_farmer(browser, base, out, phone, sizes):
    # Anonymous login screens.
    for size in sizes:
        ctx = new_context(browser, size)
        page = ctx.new_page()
        page.goto(base + "/farmer/login")
        settle(page)
        shot(page, out, f"login-phone-step-{size}", full=False)
        page.fill("#fpl-phone", "12345")
        page.click("button[type=submit]")
        page.wait_for_timeout(500)
        shot(page, out, f"login-error-{size}", full=False)
        ctx.close()

    # Signed-in farmer: sign in once (phone size shows the code step), reuse the storage.
    ctx = new_context(browser, sizes[0])
    page = ctx.new_page()
    page.goto(base + "/farmer/login")
    settle(page)
    page.fill("#fpl-phone", phone)
    page.click("button[type=submit]")
    page.wait_for_selector("[data-dev-code], .fpl-dev strong", timeout=15000)
    page.wait_for_timeout(500)
    el = page.query_selector("[data-dev-code]")
    code = re.sub(r"\D", "", el.get_attribute("data-dev-code") if el else page.inner_text(".fpl-dev strong"))
    first = page.query_selector(".fp-otp input") or page.query_selector("#fpl-code")
    first.click()
    page.keyboard.type(code[:3], delay=60)
    page.wait_for_timeout(300)
    shot(page, out, f"login-code-step-{sizes[0]}", full=False)
    page.keyboard.type(code[3:], delay=60)
    page.wait_for_url(lambda u: "/farmer/login" not in u, timeout=20000)
    settle(page, 1200)
    storage = ctx.storage_state()
    ctx.close()

    for size in sizes:
        ctx = new_context(browser, size, storage)
        page = ctx.new_page()
        for name, url in (("home", "/farmer"), ("profile", "/farmer/profile")):
            page.goto(base + url)
            settle(page, 1500)
            shot(page, out, f"{name}-{size}")
        ctx.close()

    # Desktop code step: a fresh random number (the test farmer is inside its 30 s resend cooldown).
    # Only an OTP row is created; no account until a code is verified.
    if "desktop" in sizes:
        ctx = new_context(browser, "desktop")
        page = ctx.new_page()
        page.goto(base + "/farmer/login")
        settle(page)
        page.fill("#fpl-phone", random_phone())
        page.click("button[type=submit]")
        page.wait_for_timeout(1500)
        shot(page, out, "login-code-step-desktop", full=False)
        ctx.close()


def random_phone():
    return "6" + "".join(random.choice("0123456789") for _ in range(9))


def shoot_onboarding(browser, base, out, sizes):
    for size in sizes:
        phone = random_phone()
        ctx = new_context(browser, size)
        page = ctx.new_page()
        farmer_sign_in(page, base, phone)
        if "/farmer/onboarding" not in page.url:
            page.goto(base + "/farmer/onboarding")
            settle(page, 1200)
        shot(page, out, f"onboarding-1-{size}")
        page.click("[data-fp-next]")
        page.wait_for_timeout(500)
        shot(page, out, f"onboarding-1-errors-{size}", full=False)
        page.fill("#fpf-name", "Test Farmer Selvi")
        page.click("[data-fp-next]")
        page.wait_for_timeout(600)
        for crop in ("Paddy", "Banana"):
            page.click(f".fp-crop-chip:has-text('{crop}')")
            page.wait_for_timeout(150)
        page.fill("#fpf-land", "2.5")
        page.wait_for_timeout(300)
        shot(page, out, f"onboarding-2-{size}")
        page.click("[data-fp-next]")
        page.wait_for_timeout(600)
        shot(page, out, f"onboarding-3-{size}")
        # Finish the flow: login -> onboarding -> home.
        page.fill("#fpf-pin", "628501")
        page.select_option("#fpf-state", label="Tamil Nadu")
        page.wait_for_function("() => document.querySelector('#fpf-district') && document.querySelector('#fpf-district').options.length > 1", timeout=15000)
        page.select_option("#fpf-district", label="Thoothukudi")
        page.fill("#fpf-villagename", "Kovilpatti")
        page.click("button[type=submit]")
        page.wait_for_url(lambda u: urlparse(u).path == "/farmer", timeout=20000)
        settle(page, 1500)
        shot(page, out, f"onboarding-done-home-{size}", full=False)
        ctx.close()


def shoot_gallery(browser, base, out, user, password, sizes):
    for size in sizes:
        ctx = new_context(browser, size)
        page = ctx.new_page()
        staff_sign_in(page, base, user, password)
        page.goto(base + "/dev/farmer-portal")
        settle(page, 1500)
        shot(page, out, f"gallery-{size}")
        ctx.close()


def shoot_staff(browser, base, out, user, password, prefix):
    ctx = new_context(browser, "desktop")
    page = ctx.new_page()
    page.goto(base + "/login")
    settle(page, 1200)
    shot(page, out, f"{prefix}staff-login-desktop", full=False)
    staff_sign_in(page, base, user, password)
    for name, url in (("dashboard", "/Dashboard"), ("dealer-directory", "/DealerDirectory")):
        page.goto(base + url)
        settle(page, 2500)
        shot(page, out, f"{prefix}staff-{name}-desktop", full=False)
    ctx.close()
    ctx = new_context(browser, "phone")
    page = ctx.new_page()
    page.goto(base + "/login")
    settle(page, 1200)
    shot(page, out, f"{prefix}staff-login-phone", full=False)
    ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--set", default="all", choices=["farmer", "onboarding", "gallery", "staff", "all"])
    ap.add_argument("--prefix", default="", help="file name prefix, e.g. before- / after-")
    ap.add_argument("--sizes", default="phone,desktop")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")

    with open(os.path.join(ROOT, "tools", "help-demo", "shots.json"), encoding="utf-8") as f:
        qa = json.load(f)
    user, password = "qa.admin", qa["password"]

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        if args.set in ("farmer", "all"):
            shoot_farmer(browser, base, out, args.phone, sizes)
        if args.set in ("onboarding", "all"):
            shoot_onboarding(browser, base, out, sizes)
        if args.set in ("gallery", "all"):
            shoot_gallery(browser, base, out, user, password, sizes)
        if args.set in ("staff", "all"):
            shoot_staff(browser, base, out, user, password, args.prefix)
        browser.close()


if __name__ == "__main__":
    main()
