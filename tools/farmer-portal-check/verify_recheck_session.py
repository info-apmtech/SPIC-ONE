"""
Recheck fixes U1 / U5 (farmer refresh token and farmer sign-in page), on the LOCAL web host and LOCAL API only.

    python tools/farmer-portal-check/verify_recheck_session.py [--base http://localhost:5027] [--phone 6777777703]

Run the local API with a short JWT so the refresh path is reached in minutes:

    $env:Jwt__ExpiryMinutes = "4"   (plus the usual local DB / Development / Sms__Provider=Log settings)

Checks:
  1. U1 silent refresh: a farmer who keeps the app open gets a new JWT before expiry (no warning, no sign-in form).
  2. U1 cold start: with the stored JWT marked expired, a fresh tab on "/" is restored through the refresh token.
  3. U5 sign-out: the More sheet / menu Log out lands on /farmer/login; "/" afterwards opens /farmer/login too, and a
     signed-out /farmer/fertilizer link opens /farmer/login.
  4. U1 failure: with the farmer's refresh tokens revoked (local DB), the session ends at expiry on /farmer/login.
Writes nothing but the farmer's own refresh tokens (deleted in step 4 on the local DB).

Recheck residual (--residual): run the web host through tools/farmer-portal-check/flaky_proxy.py
(--ApiBaseUrl=http://127.0.0.1:5121/) so the refresh call can be made to fail on purpose:
  N2. "Keep me signed in" unticked: after a refresh, localStorage holds no token or refresh token, and a new tab does not
      restore the session; ticked: localStorage holds the refreshed pair.
  N3. The refresh answering 503, or the connection dropping, on a cold start: the sign-in form is shown and the stored
      tokens are kept; when the API answers again, a new tab is restored. In the app at expiry with the refresh failing:
      no sign-out, the tokens are kept, and the session refreshes once the API answers again.
"""
import argparse
import os
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
from playwright.sync_api import sync_playwright  # noqa: E402
from shoot import farmer_sign_in, new_context, refuse_non_local, settle  # noqa: E402

PSQL = r"D:\Program Files\PostgreSQL\17\bin\psql.exe"
failures = []


def check(name, ok, detail=""):
    print(("  PASS  " if ok else "  FAIL  ") + name + ("" if ok else "  -- " + str(detail)[:300]))
    if not ok:
        failures.append(name)


def local_sql(sql):
    env = dict(os.environ, PGPASSWORD="1234")
    out = subprocess.run([PSQL, "-h", "localhost", "-U", "postgres", "-d", "spicone_dev", "-Atc", sql],
                         capture_output=True, text=True, env=env, check=True)
    return out.stdout.strip()


def stored(page, key):
    return page.evaluate("k => sessionStorage.getItem(k) || localStorage.getItem(k)", key)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--only-failure", action="store_true", help="run step 4 only")
    ap.add_argument("--residual", action="store_true", help="run the recheck residual N2 / N3 steps only (needs the proxy)")
    ap.add_argument("--proxy", default="http://127.0.0.1:5121", help="flaky_proxy.py control address")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    refuse_non_local(base)
    refuse_non_local(args.proxy)

    if args.residual:
        with sync_playwright() as p:
            browser = p.chromium.launch()
            residual_steps(browser, base, args)
            browser.close()
        print()
        print("All checks passed." if not failures else f"{len(failures)} check(s) FAILED: {failures}")
        sys.exit(1 if failures else 0)

    with sync_playwright() as p:
        browser = p.chromium.launch()
        ctx = new_context(browser, "phone")
        page = ctx.new_page()
        if not args.only_failure:
            steps_1_to_3(browser, ctx, page, base, args)

        # 4. Refresh refused: revoke the farmer's refresh tokens, the session ends at expiry on /farmer/login.
        # Local test number only: skip the 30 s resend cooldown left by the first sign-in.
        local_sql(f"delete from \"FarmerOtpChallenges\" where \"Phone\" = '{args.phone}' and \"ConsumedAtUtc\" is not null")
        farmer_sign_in(page, base, args.phone)
        uid = local_sql(f"select \"Id\" from \"AppUsers\" where \"UserName\" = 'f{args.phone}'")
        local_sql(f"delete from \"RefreshTokens\" where \"UserId\" = '{uid}'")
        ended = False
        for _ in range(60):  # up to 5 minutes
            page.wait_for_timeout(5000)
            if "/farmer/login" in page.url:
                ended = True
                break
        check("U1: with the refresh token refused, the session ends on /farmer/login (not the staff form)", ended, page.url)
        page.wait_for_timeout(3000)  # the sign-in page's restore makes its one refresh attempt, then forgets the token
        check("U1: and the refused refresh token is cleared from the browser", not stored(page, "spic_refresh_token"),
              "still stored")
        check("U1: the farmer sees the phone form, not the staff form", page.query_selector("#fpl-phone") is not None, page.url)
        browser.close()

    print()
    print("All checks passed." if not failures else f"{len(failures)} check(s) FAILED: {failures}")
    sys.exit(1 if failures else 0)


def local(page, key):
    return page.evaluate("k => localStorage.getItem(k)", key)


def session(page, key):
    return page.evaluate("k => sessionStorage.getItem(k)", key)


def proxy_mode(args, **mode):
    import urllib.request
    query = "&".join(f"{k}={v}" for k, v in mode.items())
    with urllib.request.urlopen(args.proxy.rstrip("/") + "/__proxy/mode?" + query, timeout=10) as r:
        return r.read().decode()


def sign_in_farmer(page, base, phone, keep):
    """farmer_sign_in with the "Keep me signed in" box set as asked."""
    import re
    local_sql(f"delete from \"FarmerOtpChallenges\" where \"Phone\" = '{phone}' and \"ConsumedAtUtc\" is not null")
    page.goto(base + "/farmer/login")
    settle(page)
    page.fill("#fpl-phone", phone)
    box = page.query_selector(".fpl-remember input")
    if box is not None and box.is_checked() != keep:
        box.click()
    page.click("button[type=submit]")
    page.wait_for_selector("[data-dev-code], .fpl-dev strong", timeout=15000)
    el = page.query_selector("[data-dev-code]")
    code = re.sub(r"\D", "", el.get_attribute("data-dev-code") if el else page.inner_text(".fpl-dev strong"))
    first = page.query_selector(".fp-otp input") or page.query_selector("#fpl-code")
    first.click()
    page.keyboard.type(code, delay=60)
    page.wait_for_url(lambda u: "/farmer/login" not in u, timeout=20000)
    settle(page, 1200)


def wait_for_new_token(page, first, minutes=4):
    for _ in range(minutes * 12):
        page.wait_for_timeout(5000)
        now = session(page, "jwt_token")
        if now and now != first:
            return now
    return None


def residual_steps(browser, base, args):
    proxy_mode(args, refresh="ok", all="ok")

    # N2, unticked: the refreshed session stays in this tab only.
    ctx = new_context(browser, "phone")
    page = ctx.new_page()
    sign_in_farmer(page, base, args.phone, keep=False)
    check("N2: signed in with 'Keep me signed in' unticked", "/farmer" in page.url and local(page, "jwt_token") is None, page.url)
    renewed = wait_for_new_token(page, session(page, "jwt_token"))
    check("N2 (unticked): the session was refreshed in the tab", renewed is not None)
    check("N2 (unticked): after the refresh localStorage has no auth token and no refresh token",
          local(page, "jwt_token") is None and local(page, "spic_refresh_token") is None,
          (local(page, "jwt_token") is not None, local(page, "spic_refresh_token") is not None))
    other = ctx.new_page()  # a new tab: empty sessionStorage, the same localStorage (as after closing the browser)
    other.goto(base + "/")
    settle(other, 2500)
    check("N2 (unticked): a new tab does not restore the session (sign-in form)", "/farmer/login" in other.url, other.url)
    ctx.close()

    # N2, ticked: the refreshed pair is kept in localStorage.
    ctx = new_context(browser, "phone")
    page = ctx.new_page()
    sign_in_farmer(page, base, args.phone, keep=True)
    renewed = wait_for_new_token(page, session(page, "jwt_token"))
    check("N2 (ticked): the session was refreshed", renewed is not None)
    check("N2 (ticked): localStorage holds the refreshed token and a refresh token",
          renewed is not None and local(page, "jwt_token") == renewed and bool(local(page, "spic_refresh_token")))

    # N3, cold start while the refresh fails (503, then a dropped connection): form shown, tokens kept.
    page.evaluate("() => localStorage.setItem('jwt_expiration', '2020-01-01T00:00:00.0000000Z')")
    kept_refresh = local(page, "spic_refresh_token")
    for mode in ("503", "drop"):
        proxy_mode(args, refresh=mode)
        cold = ctx.new_page()
        cold.goto(base + "/")
        settle(cold, 4000)
        check(f"N3 (cold start, refresh {mode}): the sign-in form is shown", "/farmer/login" in cold.url, cold.url)
        check(f"N3 (cold start, refresh {mode}): the stored refresh token and JWT are kept",
              local(cold, "spic_refresh_token") == kept_refresh and bool(local(cold, "jwt_token")))
        cold.close()
    proxy_mode(args, refresh="ok")
    cold = ctx.new_page()
    cold.goto(base + "/")
    settle(cold, 4000)
    check("N3 (cold start): once the API answers again, a new tab is restored through the kept refresh token",
          "/farmer" in cold.url and "/farmer/login" not in cold.url, cold.url)
    # That restore rotated the refresh token, so the first tab's copy is now (correctly) refused: carry on in the
    # restored tab, which holds the current pair.
    page.close()
    page = cold

    # N3, in the app at expiry while the refresh fails: no sign-out, tokens kept; refreshed when the API is back.
    page.goto(base + "/farmer")
    settle(page, 2000)
    proxy_mode(args, refresh="503")
    exp_text = session(page, "jwt_expiration")
    tok = session(page, "jwt_token")
    refresh = session(page, "spic_refresh_token")
    print("  in-app expiry at", exp_text, "- waiting past it with the refresh failing")
    from datetime import datetime, timezone
    exp = datetime.fromisoformat(exp_text.replace("Z", "+00:00").split(".")[0] + "+00:00") if "." in exp_text else datetime.fromisoformat(exp_text.replace("Z", "+00:00"))
    while datetime.now(timezone.utc) < exp:
        page.wait_for_timeout(5000)
    page.wait_for_timeout(40000)  # the expiry tick's last try and one 30 s retry, both failing
    check("N3 (in app): past expiry with the refresh failing, the farmer is not signed out", "/farmer/login" not in page.url and "/login" not in page.url, page.url)
    check("N3 (in app): the JWT and refresh token are kept", session(page, "jwt_token") == tok and session(page, "spic_refresh_token") == refresh)
    proxy_mode(args, refresh="ok")
    back = None
    for _ in range(16):
        page.wait_for_timeout(5000)
        if session(page, "jwt_token") != tok:
            back = session(page, "jwt_token")
            break
    check("N3 (in app): when the API answers again the session refreshes by itself", back is not None and "/farmer/login" not in page.url, page.url)
    ctx.close()
    proxy_mode(args, refresh="ok", all="ok")


def steps_1_to_3(browser, ctx, page, base, args):
    if True:
        farmer_sign_in(page, base, args.phone)
        check("signed in as a farmer", "/farmer" in page.url, page.url)
        first = stored(page, "jwt_token")
        exp_text = stored(page, "jwt_expiration")
        print("  token expires", exp_text)

        # 1. Silent refresh: the layout refreshes 3 minutes ahead (at most once a minute).
        renewed = None
        for _ in range(48):  # up to 4 minutes
            page.wait_for_timeout(5000)
            now = stored(page, "jwt_token")
            if now and now != first:
                renewed = now
                break
        check("U1: the open app got a new JWT before expiry (silent refresh)", renewed is not None)
        check("U1: no sign-in form, still on the farmer page", "/login" not in page.url, page.url)
        check("U1: no session warning dialog", page.query_selector(".fb-session__dialog") is None)

        # 2. Cold start with the stored JWT marked expired: a new tab on "/" (localStorage copy only).
        page.evaluate("() => { localStorage.setItem('jwt_expiration', '2020-01-01T00:00:00.0000000Z'); }")
        cold = ctx.new_page()
        cold.goto(base + "/")
        settle(cold, 2500)
        check("U1: a cold start with an expired farmer JWT is restored through the refresh token (no form)",
              "/farmer" in cold.url and "/login" not in cold.url, cold.url)
        restored_exp = cold.evaluate("() => localStorage.getItem('jwt_expiration')")
        check("U1: the restored session has a new expiry", restored_exp and not restored_exp.startswith("2020"), restored_exp)
        cold.close()

        # 3. U5: sign out from the farmer shell lands on the farmer sign-in.
        page.goto(base + "/farmer")
        settle(page, 1500)
        logged_out = page.evaluate("""() => {
            const b = [...document.querySelectorAll('button, a')].find(x => /log ?out|sign out/i.test(x.textContent || ''));
            if (b) { b.click(); return true; } return false; }""")
        if not logged_out:
            more = page.query_selector("[aria-label='More'], .bottom-nav__more, button:has-text('More')")
            if more:
                more.click()
                page.wait_for_timeout(600)
                btn = page.query_selector("button:has-text('Log out'), a:has-text('Log out'), button:has-text('Sign out')")
                if btn:
                    btn.click()
                    logged_out = True
        page.wait_for_timeout(2500)
        check("U5: Log out lands on /farmer/login", logged_out and page.url.rstrip("/").endswith("/farmer/login"), (logged_out, page.url))
        page.goto(base + "/")
        settle(page, 2000)
        check("U5: '/' on a device whose last sign-in was a farmer opens /farmer/login", "/farmer/login" in page.url, page.url)
        fresh = browser.new_context()
        fp = fresh.new_page()
        fp.goto(base + "/farmer/fertilizer")
        settle(fp, 2000)
        check("U5: a signed-out /farmer/fertilizer link opens /farmer/login", "/farmer/login" in fp.url, fp.url)
        fp.goto(base + "/")
        settle(fp, 1500)
        check("U5: '/' on a device with no farmer sign-in keeps the staff login", "/farmer/login" not in fp.url, fp.url)
        fresh.close()



if __name__ == "__main__":
    main()
