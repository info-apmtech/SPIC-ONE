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
    args = ap.parse_args()
    base = args.base.rstrip("/")
    refuse_non_local(base)

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
