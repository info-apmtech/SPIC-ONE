"""
Farmer portal Task 5 screenshots: Ask SPIC AI (/farmer/assistant).

    python tools/farmer-portal-check/shoot_task5.py --out .superpowers/sdd/2026-11-27-farmer-portal/shots/task-5 --set basic
    python tools/farmer-portal-check/shoot_task5.py --out ... --set stub

LOCAL web host only (refuses other hosts), signed in as LOCAL test farmers through the devCode.

  --set basic   the API WITHOUT an Anthropic key (what runs locally): the empty chat with starter chips, a
                conversation with library sources (basic answers), My chats (the phone list and the desktop
                column), the home tile, and the daily cap message for --cap-phone (a farmer whose allowance
                smoke_task5.py used up today).
  --set stub    the API pointed at tools/farmer-portal-check/fake_anthropic.py --delay 6 (never the real
                Anthropic API): the typing indicator while the stub waits, then the stub's full answer.

Each run asks a few questions as the test farmers (cleanup_task5_local.sql removes their chats).
"""
import argparse
import os
import sys

from playwright.sync_api import sync_playwright

sys.path.insert(0, os.path.dirname(__file__))
import shoot  # noqa: E402


def open_chat(page, base, path="/farmer/assistant"):
    page.goto(base + path)
    page.wait_for_selector(".fpai-layout, .fp-error", timeout=25000)
    shoot.settle(page, 900)


def ask(page, text, wait_answer=True):
    page.fill("#fpai-input", text)
    page.click(".fpai-composer__send")
    if wait_answer:
        page.wait_for_function("() => !document.querySelector('.fpai-typing')", timeout=30000)
        shoot.settle(page, 900)


def blur(page):
    page.evaluate("() => document.activeElement && document.activeElement.blur()")
    page.wait_for_timeout(200)


def basic(browser, base, out, sizes, phone, cap_phone):
    for size in sizes:
        ctx = shoot.new_context(browser, size)
        page = ctx.new_page()
        if size == sizes[0]:
            shoot.farmer_sign_in(page, base, phone)
            storage = ctx.storage_state()
        else:
            ctx.close()
            ctx = shoot.new_context(browser, size, storage)
            page = ctx.new_page()

        # Home tile
        page.goto(base + "/farmer")
        page.wait_for_selector(".fp-tile-grid", timeout=25000)
        shoot.settle(page, 1200)
        page.evaluate("() => { const e = document.querySelector('.fp-tile-grid'); e && e.scrollIntoView({block: 'start'}); window.scrollBy(0, -76);"
                      " document.querySelectorAll('.main-content').forEach(m => m.scrollBy(0, -76)); }")
        page.wait_for_timeout(400)
        shoot.shot(page, out, f"home-tile-{size}", full=False)

        # 1. Empty chat with starter chips
        open_chat(page, base)
        blur(page)
        shoot.shot(page, out, f"assistant-empty-{size}")

        # 2. A conversation with sources: a starter chip, then a typed follow-up
        page.click(".fpai-starter >> nth=0")
        page.wait_for_function("() => !document.querySelector('.fpai-typing') && document.querySelectorAll('.fpai-msg--ai').length >= 1", timeout=30000)
        shoot.settle(page, 800)
        ask(page, "How much DAP should I apply for paddy?")
        blur(page)
        shoot.shot(page, out, f"assistant-conversation-{size}")

        # 3. My chats
        if size == "phone":
            open_chat(page, base, "/farmer/assistant/chats")
            blur(page)
            shoot.shot(page, out, f"assistant-chats-{size}")
        else:
            page.click(".fpai-conv__link >> nth=0")
            page.wait_for_selector(".fpai-msg--ai", timeout=20000)
            shoot.settle(page, 800)
            blur(page)
            shoot.shot(page, out, f"assistant-chats-{size}", full=False)
        ctx.close()

    # 4. The daily cap (a farmer who used today's allowance)
    for size in sizes:
        ctx = shoot.new_context(browser, size)
        page = ctx.new_page()
        if size == sizes[0]:
            shoot.farmer_sign_in(page, base, cap_phone)
            cap_storage = ctx.storage_state()
        else:
            ctx.close()
            ctx = shoot.new_context(browser, size, cap_storage)
            page = ctx.new_page()
        open_chat(page, base)
        page.wait_for_selector(".fpai-cap", timeout=20000)
        blur(page)
        shoot.shot(page, out, f"assistant-cap-{size}", full=False)
        ctx.close()


def stub(browser, base, out, sizes, phone):
    for size in sizes:
        ctx = shoot.new_context(browser, size)
        page = ctx.new_page()
        if size == sizes[0]:
            shoot.farmer_sign_in(page, base, phone)
            storage = ctx.storage_state()
        else:
            ctx.close()
            ctx = shoot.new_context(browser, size, storage)
            page = ctx.new_page()
        open_chat(page, base)
        ask(page, "Why are my paddy leaves turning yellow?", wait_answer=False)
        page.wait_for_selector(".fpai-typing", timeout=10000)
        page.wait_for_timeout(1200)
        blur(page)
        shoot.shot(page, out, f"assistant-typing-{size}", full=False)
        page.wait_for_function("() => !document.querySelector('.fpai-typing')", timeout=30000)
        shoot.settle(page, 900)
        blur(page)
        shoot.shot(page, out, f"assistant-full-answer-stub-{size}")
        ctx.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--out", required=True)
    ap.add_argument("--set", choices=("basic", "stub"), required=True)
    ap.add_argument("--phone", default="6777777703", help="English local test farmer (basic set)")
    ap.add_argument("--cap-phone", default="6163665844", help="local test farmer with no questions left today")
    ap.add_argument("--stub-phone", default="6999174165", help="local test farmer for the stub set")
    ap.add_argument("--sizes", default="phone,desktop")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    shoot.refuse_non_local(base)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)
    sizes = args.sizes.split(",")
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        if args.set == "basic":
            basic(browser, base, out, sizes, args.phone, args.cap_phone)
        else:
            stub(browser, base, out, sizes, args.stub_phone)
        browser.close()


if __name__ == "__main__":
    main()
