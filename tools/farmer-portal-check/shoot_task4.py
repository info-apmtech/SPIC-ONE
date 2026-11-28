"""
Farmer portal Task 4 screenshots: fertilizer advice inputs (sliders in Low / Medium / High), the result for
cotton on black cotton soil with the owner's sketch values (N 100, P 5, K 50), an acidic-soil case, the
"Use my soil report" prefill, the home tile and the history (list and one saved run).

    python tools/farmer-portal-check/shoot_task4.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/task-4

LOCAL web host only (refuses other hosts). Signs in once as the LOCAL test farmer 6777777703 (one of the
number's 5 codes per hour), who must be linked to the demo soil report of seed_task4_local.sql. Each run
saves three fertilizer runs per size for that test farmer (cleanup_task4_local.sql removes them).
"""
import argparse
import os
import sys

from playwright.sync_api import sync_playwright

sys.path.insert(0, os.path.dirname(__file__))
import shoot  # noqa: E402  (sign-in helpers and the shot() writer)


def set_value(page, slider_id, value):
    box = page.locator(f".fp-zone:has(#{slider_id}) .fp-zone__entry input")
    box.fill(str(value))
    box.press("Tab")
    page.wait_for_timeout(250)


def soil(page, n, p, k, ph):
    set_value(page, "fz-n", n)
    set_value(page, "fz-p", p)
    set_value(page, "fz-k", k)
    set_value(page, "fz-ph", ph)


def crop(page, name, variant, soil_type, acres):
    page.select_option("#fz-crop", name)
    page.wait_for_timeout(300)
    if variant:
        page.locator(".fpfz-variants button", has_text=variant).first.click()
        page.wait_for_timeout(200)
    page.select_option("#fz-soil", soil_type)
    page.fill("#fz-acres", str(acres))
    page.press("#fz-acres", "Tab")
    page.wait_for_timeout(300)


def to_top(page):
    """Back to the top of the page (the desktop shell scrolls .main-content, the phone the window) without a focus ring."""
    page.evaluate("""() => {
        document.activeElement && document.activeElement.blur();
        window.scrollTo(0, 0);
        document.querySelectorAll('.main-content, main, .content, article').forEach(e => e.scrollTo && e.scrollTo(0, 0));
    }""")
    page.wait_for_timeout(400)


def submit(page):
    page.click(".fpfz-actionbar .fp-btn--primary")
    page.wait_for_selector(".fpfr", timeout=20000)
    shoot.settle(page, 1500)


def scroll_to(page, selector):
    # scrollIntoView, then back 76 px so the element clears the app header (window on phone, .main-content on desktop).
    page.evaluate("""(sel) => { const e = document.querySelector(sel); if (!e) return;
        e.scrollIntoView({block: 'start'}); window.scrollBy(0, -76);
        document.querySelectorAll('.main-content, main, .content, article').forEach(m => m.scrollBy && m.scrollBy(0, -76)); }""", selector)
    page.wait_for_timeout(500)


def open_page(page, base):
    page.goto(base + "/farmer/fertilizer")
    page.wait_for_selector(".fpfz-inputs, .fp-error", timeout=25000)
    shoot.settle(page, 800)


def screens(browser, base, out, storage, size):
    ctx = shoot.new_context(browser, size, storage)
    page = ctx.new_page()

    # Home with the new first tile.
    page.goto(base + "/farmer")
    page.wait_for_selector(".fp-tile-grid", timeout=25000)
    shoot.settle(page, 1200)
    scroll_to(page, ".fp-tile-grid")
    shoot.shot(page, out, f"home-tile-{size}", full=False)

    # 1. Inputs: N Low, P Medium, K High, pH neutral.
    open_page(page, base)
    soil(page, 100, 30, 320, 7.0)
    crop(page, "Cotton", "Irrigated hybrids", "Black Cotton Soil", 2)
    to_top(page)
    shoot.shot(page, out, f"fertilizer-inputs-{size}")

    # 1b. P entered from a card printed in P2O5 (the unit toggle on): 40 kg P2O5 = 17.5 kg P, Medium.
    open_page(page, base)
    page.locator(".fpfz-basis button", has_text="kg P2O5/ha").first.click()
    page.wait_for_timeout(300)
    soil(page, 300, 40, 150, 6.8)
    crop(page, "Paddy", "Medium duration", "Alluvial Soil", 2)
    scroll_to(page, ".fp-zone:has(#fz-p)")
    shoot.shot(page, out, f"fertilizer-inputs-p2o5-{size}", full=False)

    # 2. The sketch: cotton on black cotton soil, N 100, P 5, K 50.
    open_page(page, base)
    soil(page, 100, 5, 50, 7.0)
    crop(page, "Cotton", "Irrigated hybrids", "Black Cotton Soil", 1)
    submit(page)
    scroll_to(page, ".fpfr")
    shoot.shot(page, out, f"fertilizer-result-cotton-{size}")

    # 3. Acidic laterite soil, paddy.
    open_page(page, base)
    soil(page, 220, 15, 100, 5.2)
    crop(page, "Paddy", "Medium duration", "Laterite Soil", 2.5)
    submit(page)
    page.evaluate("""() => { const c = [...document.querySelectorAll('.fpfr .fp-card')].find(e => e.textContent.includes('Strongly acidic soil'));
                             if (c) c.id = 'shot-acid'; }""")
    scroll_to(page, "#shot-acid")
    shoot.shot(page, out, f"fertilizer-acidic-{size}", full=False)

    # 4. Prefilled from the SPIC soil report.
    open_page(page, base)
    page.click("text=Use my soil report")
    page.wait_for_selector(".fpfz-report.fp-alert--success", timeout=20000)
    shoot.settle(page, 600)
    crop(page, "Cotton", "Irrigated varieties", "Black Cotton Soil", 2)
    to_top(page)
    scroll_to(page, ".fpfz-report")
    shoot.shot(page, out, f"fertilizer-prefilled-{size}")
    submit(page)
    scroll_to(page, ".fpfr")
    shoot.shot(page, out, f"fertilizer-prefilled-result-{size}", full=False)

    # 5. History and one saved run.
    page.goto(base + "/farmer/fertilizer/history")
    page.wait_for_selector(".fpfh-list, .fp-empty", timeout=25000)
    shoot.settle(page, 800)
    shoot.shot(page, out, f"fertilizer-history-{size}")
    page.click(".fpfh-item >> nth=0")
    page.wait_for_selector(".fpfr", timeout=20000)
    shoot.settle(page, 800)
    shoot.shot(page, out, f"fertilizer-history-run-{size}", full=False)
    ctx.close()


def partial(browser, base, out, storage, size):
    """A report without potassium: K stays "Not set" and advice is refused until the farmer enters it."""
    ctx = shoot.new_context(browser, size, storage)
    page = ctx.new_page()
    open_page(page, base)
    page.click("text=Use my soil report")
    page.wait_for_selector(".fpfz-report.fp-alert--success", timeout=20000)
    shoot.settle(page, 600)
    crop(page, "Paddy", "Medium duration", "Alluvial Soil", 1)
    page.click(".fpfz-actionbar .fp-btn--primary")
    page.wait_for_selector(".fp-alert--error", timeout=10000)
    shoot.settle(page, 400)
    text = page.inner_text(".fp-alert--error")
    if "no usable potassium" not in text or page.query_selector(".fpfr") is not None:
        sys.exit(f"partial report: advice was not blocked ({text!r})")
    scroll_to(page, ".fp-zone:has(#fz-k)")
    shoot.shot(page, out, f"fertilizer-partial-blocked-{size}", full=False)
    # Entering K unblocks it.
    set_value(page, "fz-k", 140)
    submit(page)
    print(f"  partial report ({size}): blocked until K was entered, then advice shown")
    ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--phone", default="6777777703", help="local test farmer linked to the demo soil report")
    ap.add_argument("--sizes", default="phone,desktop")
    ap.add_argument("--partial-phone", default="", help="local test farmer linked to the report without potassium (e.g. 6614843176)")
    ap.add_argument("--only-partial", action="store_true")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    shoot.refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        ctx = shoot.new_context(browser, sizes[0])
        page = ctx.new_page()
        shoot.farmer_sign_in(page, base, args.phone)
        storage = ctx.storage_state()
        ctx.close()
        if not args.only_partial:
            for size in sizes:
                screens(browser, base, out, storage, size)
        if args.partial_phone:
            ctx = shoot.new_context(browser, sizes[0])
            page = ctx.new_page()
            shoot.farmer_sign_in(page, base, args.partial_phone)
            pstorage = ctx.storage_state()
            ctx.close()
            for size in sizes:
                partial(browser, base, out, pstorage, size)
        browser.close()


if __name__ == "__main__":
    main()
