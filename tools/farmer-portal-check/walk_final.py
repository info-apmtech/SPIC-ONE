"""
Farmer portal final fix wave: click through every farmer page on the LOCAL web host as a local test farmer, so the
API log shows whether the Farmer deny-by-default filter refused anything a farmer page needs.

    python tools/farmer-portal-check/walk_final.py [--base http://localhost:5027] [--phone 6777777703] [--api-log <file>]

The web host is Blazor Server: the API calls happen on the server, not in the browser, so the check is the API side.
With --api-log (the local API's console output) the script counts "Farmer role refused" lines written during the walk
and fails when there is any. It also checks the local request log (AppRequestLogs, local spicone_dev) for Farmer 403s.
Changes no data (only reads; the farmer signs in with the Development devCode).
"""
import argparse
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(__file__))
from playwright.sync_api import sync_playwright  # noqa: E402
from shoot import farmer_sign_in, new_context, refuse_non_local, settle  # noqa: E402

PSQL = r"D:\Program Files\PostgreSQL\17\bin\psql.exe"
PAGES = [
    "/farmer", "/farmer/profile", "/farmer/dealers", "/farmer/team", "/farmer/requests", "/farmer/trends",
    "/farmer/community", "/farmer/fertilizer", "/farmer/fertilizer/history", "/farmer/assistant",
    "/farmer/assistant/chats", "/farmer/leaf-check", "/farmer/leaf-check/history", "/farmer/yield",
    "/farmer/yield/history", "/Community", "/Community/discussions", "/Community/new",
    "/Sas/MySamples", "/Sas/MyReports",
]


def local_sql(sql):
    env = dict(os.environ, PGPASSWORD="1234")
    out = subprocess.run([PSQL, "-h", "localhost", "-U", "postgres", "-d", "spicone_dev", "-Atc", sql],
                         capture_output=True, text=True, env=env, check=True)
    return out.stdout.strip()


def refused_lines(path):
    if not path or not os.path.exists(path):
        return None
    with open(path, encoding="utf-8", errors="replace") as f:
        return [l.strip() for l in f if "Farmer role refused" in l]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5027")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--api-log", default="")
    args = ap.parse_args()
    base = args.base.rstrip("/")
    refuse_non_local(base)

    before = refused_lines(args.api_log)
    last_id = local_sql('select coalesce(max("Id"),0) from "AppRequestLogs"')
    discussion = local_sql('select "Id" from "CommunityPosts" where not "IsDeleted" order by "Id" desc limit 1')
    library = local_sql('select "Id" from "LibraryContents" order by "Id" limit 1')
    pages = list(PAGES)
    if discussion:
        pages.append(f"/Community/discussion/{discussion}")
    if library:
        pages.append(f"/DigitalLibrary/view/{library}")

    with sync_playwright() as p:
        browser = p.chromium.launch(channel="msedge", headless=True)
        ctx = new_context(browser, "desktop")
        page = ctx.new_page()
        farmer_sign_in(page, base, args.phone)
        for url in pages:
            page.goto(base + url)
            settle(page, 1800)
            print(f"  visited {url} -> {page.url.replace(base, '')}")
        ctx.close()
        browser.close()

    failures = 0
    rows = local_sql(f'select "Method"||\' \'||"Path" from "AppRequestLogs" where "Id" > {last_id} '
                     f'and "Role" = \'Farmer\' and "StatusCode" = 403')
    calls = local_sql(f'select count(*) from "AppRequestLogs" where "Id" > {last_id} and "Role" = \'Farmer\'')
    print(f"  farmer API calls during the walk: {calls}")
    if rows:
        failures += 1
        print("  FAIL  farmer 403s in the request log:\n    " + rows.replace("\n", "\n    "))
    else:
        print("  PASS  no farmer 403 in the request log")
    after = refused_lines(args.api_log)
    if after is not None:
        new = after[len(before):]
        if new:
            failures += 1
            print("  FAIL  'Farmer role refused' in the API log:\n    " + "\n    ".join(new))
        else:
            print("  PASS  no 'Farmer role refused' line in the API log during the walk")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
