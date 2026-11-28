"""
Farmer portal Task 7 screenshots: the harvest yield forecast inputs (prefilled from the profile, the owner's sketch
values), the result for the sketch (Paddy, 3 acres, canal / borewell, NDVI 0.72, 850 mm, 90 %), a low-input result
(rainfed, sparse by look, 550 mm, 60 %), a form error, the history (list and one saved forecast), the empty history
and the home tile; with --set error, the page while the API is down.

    python tools/farmer-portal-check/shoot_task7.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/task-7
    python tools/farmer-portal-check/shoot_task7.py --out ... --set empty --phone 6163665844   (a farmer with no forecasts)
    python tools/farmer-portal-check/shoot_task7.py --out ... --set farmer --storage-out <file>   then stop the API and
    python tools/farmer-portal-check/shoot_task7.py --out ... --set error --storage-in <file>

LOCAL web host only (refuses other hosts). Signs in once per run as a LOCAL test farmer (one of the number's 5 codes per
hour). Each farmer run saves two forecasts per size for that test farmer (cleanup_task7_local.sql removes them).
"""
import argparse
import os
import sys

from playwright.sync_api import sync_playwright

sys.path.insert(0, os.path.dirname(__file__))
import shoot  # noqa: E402  (sign-in helpers and the shot() writer)
from shoot_task4 import scroll_to, to_top  # noqa: E402


def set_value(page, slider_id, value):
    box = page.locator(f".fp-zone:has(#{slider_id}) .fp-zone__entry input")
    box.fill(str(value))
    box.press("Tab")
    page.wait_for_timeout(250)


def open_page(page, base):
    page.goto(base + "/farmer/yield")
    page.wait_for_selector(".fpy-inputs, .fp-empty", timeout=25000)
    shoot.settle(page, 800)


def crop(page, name):
    page.locator(".fpy-crops button", has_text=name).first.click()
    page.wait_for_timeout(250)


def method(page, label):
    page.locator(".fpy-method", has_text=label).first.click()
    page.wait_for_timeout(250)


def acres(page, value):
    page.fill("#fpy-acres", str(value))
    page.press("#fpy-acres", "Tab")
    page.wait_for_timeout(250)


def submit(page):
    page.click(".fpy-actionbar .fp-btn--primary")
    page.wait_for_selector(".fpyr", timeout=20000)
    shoot.settle(page, 1200)


def farmer_screens(browser, base, out, storage, size):
    ctx = shoot.new_context(browser, size, storage)
    page = ctx.new_page()

    # Home with the new tile in view.
    page.goto(base + "/farmer")
    page.wait_for_selector(".fp-tile-grid", timeout=25000)
    shoot.settle(page, 1200)
    scroll_to(page, ".fp-tile-grid")
    if size == "phone":
        page.evaluate("""() => { const t = document.querySelector('.fp-tile-grid a[href="/farmer/yield"]');
            if (t) { t.scrollIntoView({block: 'center'}); } }""")
        page.wait_for_timeout(400)
    shoot.shot(page, out, f"home-tile-{size}", full=False)

    # 1. Inputs: the sketch (prefilled from the profile, area set to the sketch's 3 acres).
    open_page(page, base)
    crop(page, "Paddy")
    method(page, "Canal / Borewell")
    acres(page, 3)
    to_top(page)
    shoot.shot(page, out, f"yield-inputs-{size}")

    # 2. The sketch result.
    submit(page)
    scroll_to(page, ".fpyr")
    shoot.shot(page, out, f"yield-result-{size}")

    # 3. Low inputs: rainfed, sparse by look, 550 mm, 60 % NPK.
    open_page(page, base)
    crop(page, "Paddy")
    method(page, "Rainfed")
    acres(page, 3)
    page.click(".fpy-look--sparse")
    page.wait_for_timeout(300)
    set_value(page, "fpy-rain", 550)
    set_value(page, "fpy-npk", 60)
    scroll_to(page, ".fpy-card--season")
    shoot.shot(page, out, f"yield-inputs-low-{size}", full=False)
    submit(page)
    scroll_to(page, ".fpyr")
    shoot.shot(page, out, f"yield-result-low-{size}")

    # 4. Sugarcane: rainfed is not offered.
    open_page(page, base)
    crop(page, "Sugarcane")
    scroll_to(page, ".fpy-methods")
    shoot.shot(page, out, f"yield-sugarcane-methods-{size}", full=False)

    # 5. A form error: no area.
    open_page(page, base)
    page.fill("#fpy-acres", "")
    page.press("#fpy-acres", "Tab")
    page.click(".fpy-actionbar .fp-btn--primary")
    page.wait_for_selector(".fpy-formerror", timeout=10000)
    shoot.settle(page, 400)
    scroll_to(page, "#fpy-acres")
    shoot.shot(page, out, f"yield-form-error-{size}", full=False)

    # 6. History and one saved forecast.
    page.goto(base + "/farmer/yield/history")
    page.wait_for_selector(".fpyh-list, .fp-empty", timeout=25000)
    shoot.settle(page, 800)
    shoot.shot(page, out, f"yield-history-{size}")
    page.click(".fpyh-item >> nth=0")
    page.wait_for_selector(".fpyr", timeout=20000)
    shoot.settle(page, 800)
    shoot.shot(page, out, f"yield-history-one-{size}", full=False)
    ctx.close()


def empty_screens(browser, base, out, storage, size):
    ctx = shoot.new_context(browser, size, storage)
    page = ctx.new_page()
    page.goto(base + "/farmer/yield/history")
    page.wait_for_selector(".fpyh-list, .fp-empty", timeout=25000)
    shoot.settle(page, 800)
    if page.query_selector(".fpyh-list") is not None:
        sys.exit("empty set: this farmer has forecasts; run cleanup_task7_local.sql or pick another local test farmer")
    shoot.shot(page, out, f"yield-history-empty-{size}", full=False)
    ctx.close()


def error_screens(browser, base, out, storage, size):
    ctx = shoot.new_context(browser, size, storage)
    page = ctx.new_page()
    page.goto(base + "/farmer/yield")
    page.wait_for_selector("text=We could not open the harvest forecast", timeout=120000)  # FpError; connection retries take a while
    shoot.settle(page, 800)
    shoot.shot(page, out, f"yield-error-{size}", full=False)
    ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--phone", default="6777777703", help="local test farmer")
    ap.add_argument("--sizes", default="phone,desktop")
    ap.add_argument("--set", default="farmer", choices=["farmer", "empty", "error"])
    ap.add_argument("--storage-out", default="", help="save the signed-in browser storage here (for --set error)")
    ap.add_argument("--storage-in", default="", help="use this saved storage instead of signing in")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    shoot.refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        if args.storage_in:
            storage = args.storage_in
        else:
            ctx = shoot.new_context(browser, sizes[0])
            page = ctx.new_page()
            shoot.farmer_sign_in(page, base, args.phone)
            storage = ctx.storage_state(path=args.storage_out or None)
            ctx.close()
        screens = {"farmer": farmer_screens, "empty": empty_screens, "error": error_screens}[args.set]
        for size in sizes:
            screens(browser, base, out, storage, size)
        browser.close()


if __name__ == "__main__":
    main()
