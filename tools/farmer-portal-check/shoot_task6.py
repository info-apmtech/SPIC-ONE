"""
Farmer portal Task 6 screenshots: the leaf check (/farmer/leaf-check) and My past leaf checks.

    python tools/farmer-portal-check/make_leaves.py --out <scratch>/leaves
    python tools/farmer-portal-check/shoot_task6.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/task-6 --set stub --leaves <scratch>/leaves
    python tools/farmer-portal-check/shoot_task6.py --out ... --set basic --leaves <scratch>/leaves

LOCAL web host only (refuses other hosts), signed in as a LOCAL test farmer through the devCode. The photos are the
drawn test leaves from make_leaves.py (nothing downloaded).

  --set stub    the API pointed at fake_anthropic.py --delay 5 (never the real Anthropic API): the capture screen with
                tips, the preview, the analysing state, results for a deficiency (interveinal, groundnut), a healthy leaf
                and a photo that is not a leaf (the stub's "notleaf" mode), the history list and the home tile.
  --set basic   the API WITHOUT an Anthropic key: the "basic colour check" result (yellow maize leaf).

Each size makes real checks (stub: 3, basic: 1) for the farmer: cleanup_task6_local.sql resets the allowance.
"""
import argparse
import json
import os
import sys
import urllib.request

from playwright.sync_api import sync_playwright

sys.path.insert(0, os.path.dirname(__file__))
import shoot  # noqa: E402


def context(browser, size, storage=None):
    w, h, mobile = shoot.SIZES[size]
    # motion on: the analysing state is a scanning animation
    return browser.new_context(viewport={"width": w, "height": h}, is_mobile=mobile, has_touch=mobile, device_scale_factor=1,
                               locale="en-IN", reduced_motion="no-preference", storage_state=storage, timezone_id="Asia/Kolkata")


def blur(page):
    page.evaluate("() => document.activeElement && document.activeElement.blur()")
    page.mouse.move(2, 2)
    page.wait_for_timeout(200)


def top(page):
    page.evaluate("() => { window.scrollTo(0, 0); document.querySelectorAll('.main-content, .content, main').forEach(e => e.scrollTo(0, 0)); }")
    page.wait_for_timeout(300)


def open_check(page, base):
    page.goto(base + "/farmer/leaf-check")
    page.wait_for_selector(".fplc-layout, .fplr, .fp-error, .fplc-cap", timeout=25000)
    shoot.settle(page, 900)


def choose(page, path, crop, age=None):
    page.set_input_files("#fplc-gallery", path)
    page.wait_for_selector(".fplc-preview img", timeout=20000)
    page.click(f".fplc-crops button.fp-chip:has-text('{crop}')")
    if age:
        page.click(f"button.fp-chip:has-text('{age}')")
    shoot.settle(page, 600)


def submit(page):
    page.click(".fplc-actionbar .fp-btn--primary")


def wait_result(page):
    page.wait_for_selector(".fplr", timeout=90000)
    shoot.settle(page, 1200)
    blur(page)
    top(page)


def stub_mode(stub, mode):
    req = urllib.request.Request(stub.rstrip("/") + "/__mode", data=json.dumps({"vision": mode}).encode(), method="POST")
    urllib.request.urlopen(req, timeout=5).read()


def stub_set(browser, base, out, sizes, phone, leaves, stub):
    stub_mode(stub, "auto")
    storage = None
    for size in sizes:
        ctx = context(browser, size, storage)
        page = ctx.new_page()
        if storage is None:
            shoot.farmer_sign_in(page, base, phone)
            storage = ctx.storage_state()

        # home tile
        page.goto(base + "/farmer")
        page.wait_for_selector(".fp-tile-grid", timeout=25000)
        shoot.settle(page, 1200)
        page.evaluate("() => { const e = document.querySelector('.fp-tile-grid'); e && e.scrollIntoView({block: 'start'}); window.scrollBy(0, -76);"
                      " document.querySelectorAll('.main-content').forEach(m => m.scrollBy(0, -76)); }")
        page.wait_for_timeout(400)
        shoot.shot(page, out, f"home-tile-{size}", full=False)

        # 1. capture screen with tips
        open_check(page, base)
        blur(page)
        shoot.shot(page, out, f"leaf-capture-{size}")

        # 2. preview (crop and leaves chosen)
        choose(page, os.path.join(leaves, "interveinal.jpg"), "Groundnut", "Young (top) leaves")
        blur(page)
        top(page)
        shoot.shot(page, out, f"leaf-preview-{size}")

        # 3. analysing (the stub waits 5 s)
        submit(page)
        page.wait_for_selector(".fplc-preview.is-scanning", timeout=10000)
        page.wait_for_timeout(1300)
        top(page)
        shoot.shot(page, out, f"leaf-analysing-{size}", full=False)

        # 4. a deficiency
        wait_result(page)
        shoot.shot(page, out, f"leaf-result-deficiency-{size}")

        # 5. a healthy leaf
        page.click(".fplr-actions button:has-text('Check another leaf')")
        page.wait_for_selector(".fplc-drop", timeout=10000)
        choose(page, os.path.join(leaves, "healthy.jpg"), "Paddy")
        submit(page)
        wait_result(page)
        shoot.shot(page, out, f"leaf-result-healthy-{size}")

        # 6. not a leaf (the stub answers notALeaf)
        stub_mode(stub, "notleaf")
        page.click(".fplr-actions button:has-text('Check another leaf')")
        page.wait_for_selector(".fplc-drop", timeout=10000)
        choose(page, os.path.join(leaves, "not_leaf.jpg"), "Cotton")
        submit(page)
        wait_result(page)
        stub_mode(stub, "auto")
        shoot.shot(page, out, f"leaf-result-notleaf-{size}")

        # 7. history (thumbnails load after the list)
        page.goto(base + "/farmer/leaf-check/history")
        page.wait_for_selector(".fplh-list, .fp-empty", timeout=25000)
        page.wait_for_function("() => document.querySelectorAll('.fplh-item__thumb img').length >= Math.min(3, document.querySelectorAll('.fplh-item').length)", timeout=30000)
        shoot.settle(page, 900)
        shoot.shot(page, out, f"leaf-history-{size}")
        ctx.close()


def basic_set(browser, base, out, sizes, phone, leaves):
    storage = None
    for size in sizes:
        ctx = context(browser, size, storage)
        page = ctx.new_page()
        if storage is None:
            shoot.farmer_sign_in(page, base, phone)
            storage = ctx.storage_state()
        open_check(page, base)
        blur(page)
        shoot.shot(page, out, f"leaf-capture-basic-{size}", full=False)
        choose(page, os.path.join(leaves, "n_deficient.jpg"), "Maize", "Older (lower) leaves")
        submit(page)
        wait_result(page)
        shoot.shot(page, out, f"leaf-result-basic-{size}")
        ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--set", choices=("stub", "basic"), required=True)
    ap.add_argument("--leaves", required=True)
    ap.add_argument("--phone", default="6271619756", help="English local test farmer with a complete profile")
    ap.add_argument("--stub", default="http://127.0.0.1:5999")
    ap.add_argument("--sizes", default="phone,desktop")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    shoot.refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        if args.set == "stub":
            stub_set(browser, base, out, sizes, args.phone, args.leaves, args.stub)
        else:
            basic_set(browser, base, out, sizes, args.phone, args.leaves)
        browser.close()


if __name__ == "__main__":
    main()
