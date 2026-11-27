"""
Farmer portal Task 3 smoke test: trends in my area, the home weather strip and community near me,
against a LOCAL API and the LOCAL spicone_dev database only.

    psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task2_local.sql
    psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task3_local.sql
    python tools/farmer-portal-check/smoke_task3.py [--base http://localhost:5120]

Needs the API in the Development environment with Sms:Provider = Log (devCode) on the local DB:

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

Signs in as the local test farmer 6777777703 (no GPS: weather = NoLocation) and the local smoke farmer
6999174165 (GPS Kovilpatti: weather from Open-Meteo, the only external call), and as qa.mdo (staff).
It changes no data except one discussion it posts as the test farmer (title "[smoke] ..."), which it
deletes again; cleanup_task3_local.sql removes leftovers.
"""
import argparse
import json
import os
import random
import sys
import time
import urllib.error
import urllib.request
from urllib.parse import urlparse

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
failures = []
RUN_IP = "198.18.%d.%d" % (random.randint(0, 255), random.randint(1, 254))


def call(base, method, path, body=None, token=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(base.rstrip("/") + "/" + path.lstrip("/"), data=data, method=method)
    req.add_header("Content-Type", "application/json")
    req.add_header("X-Forwarded-For", RUN_IP)
    if token:
        req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    t0 = time.time()
    try:
        with urllib.request.urlopen(req, timeout=40) as resp:
            raw = resp.read().decode()
            return resp.status, (json.loads(raw) if raw.strip().startswith(("{", "[")) else raw), time.time() - t0
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw), time.time() - t0
        except ValueError:
            return e.code, raw, time.time() - t0


def check(name, ok, detail=""):
    print(("  PASS  " if ok else "  FAIL  ") + name + ("" if ok else "  -- " + str(detail)[:400]))
    if not ok:
        failures.append(name)


def farmer_token(base, phone):
    st, body, _ = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": phone})
    if st != 200 or not body.get("devCode"):
        sys.exit(f"otp/request failed ({st}): {body}. Is the API in Development with Sms:Provider=Log?")
    st, body, _ = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": phone, "code": body["devCode"]})
    if st != 200:
        sys.exit(f"otp/verify failed ({st}): {body}")
    return body["token"]


def staff_token(base, user, password):
    st, body, _ = call(base, "POST", "api/Authentication/login", {"userName": user, "password": password})
    if st != 200:
        sys.exit(f"staff login {user} failed ({st}); run seed_task2_local.sql (it activates the QA staff users).")
    return body["token"]


def qa_password():
    with open(os.path.join(ROOT, "tools", "help-demo", "shots.json"), encoding="utf-8") as f:
        return json.load(f)["password"]


PERSONAL_KEYS = {"phone", "mobile", "userId", "authorUserId", "latitude", "longitude", "village", "name"}


def walk_keys(value, path=""):
    if isinstance(value, dict):
        for k, v in value.items():
            yield path + "." + k, k
            yield from walk_keys(v, path + "." + k)
    elif isinstance(value, list):
        for i, v in enumerate(value):
            yield from walk_keys(v, f"{path}[{i}]")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703", help="local test farmer WITHOUT GPS")
    ap.add_argument("--gps-phone", default="6999174165", help="local test farmer WITH GPS")
    args = ap.parse_args()
    if (urlparse(args.base).hostname or "") not in ("localhost", "127.0.0.1"):
        sys.exit("Refusing a non-local API: this smoke test runs against the LOCAL API only.")
    base = args.base

    print("Anonymous / wrong role")
    st, _, _ = call(base, "GET", "api/Farmer/trends")
    check("trends without a token is 401", st == 401, st)
    st, _, _ = call(base, "GET", "api/Farmer/weather")
    check("weather without a token is 401", st == 401, st)
    st, _, _ = call(base, "GET", "api/Community/near")
    check("community near without a token is 401", st == 401, st)
    mdo = staff_token(base, "qa.mdo", qa_password())
    st, _, _ = call(base, "GET", "api/Farmer/trends", token=mdo)
    check("trends for staff is 403", st == 403, st)

    print("Farmer without GPS")
    farmer = farmer_token(base, args.phone)
    st, w, _ = call(base, "GET", "api/Farmer/weather", token=farmer)
    check("weather 200", st == 200, w)
    check("no GPS = NoLocation (1), no days", st == 200 and w["status"] == 1 and w["days"] == [], w)

    st, t, secs = call(base, "GET", "api/Farmer/trends", token=farmer)
    check("trends 200", st == 200, t)
    if st == 200:
        print(f"    (first call {secs:.2f}s)")
        check("trends: district name", t["districtName"] == "Thoothukudi", t.get("districtName"))
        check("trends: 90-day window", t["windowDays"] == 90, t.get("windowDays"))
        check("trends: weather NoLocation", t["weather"]["status"] == 1, t["weather"])
        soil = t["soil"]
        check("soil: district scope with >= 5 samples", soil["scope"] == 0 and soil["sampleCount"] >= 5, soil)
        check("soil: every item rests on >= 5 samples", all(i["samples"] >= 5 for i in soil["items"]), soil["items"])
        check("soil: items are aggregates only", all(set(i) <= {"parameter", "short", "label", "samples", "lowSamples", "lowPct"} for i in soil["items"]), soil["items"])
        check("soil: nitrogen / zinc lows found in the local lab data",
              {"Nitrogen", "Zinc"} <= {i["parameter"] for i in soil["items"]}, [i["parameter"] for i in soil["items"]])
        crops = t["crops"]
        check("crops: district scope with >= 5 farmers", crops["scope"] == 0 and crops["farmerCount"] >= 5, crops)
        check("crops: every crop has >= 2 farmers", all(c["farmers"] >= 2 for c in crops["items"]), crops["items"])
        talked = t["talked"]
        check("talked: the demo discussions of the district", talked["scope"] == 0 and len(talked["items"]) >= 3, talked)
        check("talked: at most 5", len(talked["items"]) <= 5, len(talked["items"]))
        check("talked: busiest first (2 recent replies x3 + 9 likes + 120 views / 10)", talked["items"] and talked["items"][0]["title"].startswith("Zinc shortage"),
              [i["title"] for i in talked["items"]])
        # Same listable rule as Task 2's dealer list (no GPS = the district list).
        _, dl, _ = call(base, "GET", "api/Farmer/dealers?pageSize=1", token=farmer)
        check("dealers: count equals the district dealer list (Task 2 listable rule)",
              t["dealers"]["scope"] == 0 and t["dealers"]["areaName"] == "Thoothukudi" and t["dealers"]["count"] == dl.get("total") and dl.get("scope") == 1,
              (t["dealers"], dl.get("scope"), dl.get("total")))
        print(f"    (scopes: crops {crops['scope']}, soil {soil['scope']}, talked {talked['scope']}, dealers {t['dealers']['scope']}; 0 = district, 1 = state)")
        leaked = [p for p, k in walk_keys(t) if k in PERSONAL_KEYS]
        check("trends carry no personal field (phone, user id, GPS, village, name)", not leaked, leaked)
        st2, t2, secs2 = call(base, "GET", "api/Farmer/trends", token=farmer)
        check("trends: second call served from the 1 h cache (same computed time)",
              st2 == 200 and t2["areaComputedAtUtc"] == t["areaComputedAtUtc"], (t.get("areaComputedAtUtc"), t2.get("areaComputedAtUtc")))
        print(f"    (cached call {secs2:.2f}s)")

    print("Community near me")
    st, near, _ = call(base, "GET", "api/Community/near?page=1&pageSize=3", token=farmer)
    check("near 200", st == 200, near)
    if st == 200:
        check("near: district scope (5 district discussions >= the 3 threshold)", near["scope"] == 0 and near["areaName"] == "Thoothukudi", near)
        check("near: paging (3 of 5)", len(near["items"]) == 3 and near["total"] == 5 and near["hasMore"], near)
        authors = {i["author"]["name"] for i in near["items"]}
        check("near: authors are the local farmers / Madurai HQ staff",
              authors <= {"Test Farmer Murugan", "Test Farmer Selvi", "QA MDO", "QA JMDO"}, authors)
    st, tab, _ = call(base, "GET", "api/Community/discussions?tab=near&pageSize=50", token=farmer)
    check("discussions?tab=near gives the same 5", st == 200 and tab["total"] == 5, tab if st != 200 else tab["total"])
    st, alltab, _ = call(base, "GET", "api/Community/discussions?pageSize=50", token=farmer)
    check("the All tab still lists every discussion", st == 200 and alltab["total"] > 5, alltab.get("total") if st == 200 else alltab)

    st, near_staff, _ = call(base, "GET", "api/Community/near", token=mdo)
    check("near for staff: their state (Tamil Nadu)", st == 200 and near_staff["scope"] == 1 and near_staff["areaName"] == "Tamil Nadu", near_staff)

    print("Farmer author shows profile name and village / district")
    st, post, _ = call(base, "POST", "api/Community/discussions",
                       {"title": "[smoke] Task 3 author check", "body": "Local smoke test post.", "category": "Soil Health", "tags": []}, token=farmer)
    check("farmer can post", st == 200, post)
    if st == 200:
        _, prof, _ = call(base, "GET", "api/Farmer/profile", token=farmer)
        check("author name from the farmer profile", post["author"]["name"] == prof["name"], (post["author"], prof.get("name")))
        check("author location = village, district", post["author"]["location"] == f'{prof["village"]}, {prof["districtName"]}', post["author"])
        check("author role Farmer", post["author"]["role"] == "Farmer", post["author"])
        st, _, _ = call(base, "DELETE", f"api/Community/discussions/{post['id']}", token=farmer)
        check("smoke post deleted again", st == 200, st)

    print("Farmer with GPS (Open-Meteo)")
    gps = farmer_token(base, args.gps_phone)
    st, w, secs = call(base, "GET", "api/Farmer/weather", token=gps)
    check("weather 200", st == 200, w)
    if st == 200 and w["status"] == 0:
        check("7 forecast days", len(w["days"]) == 7, len(w["days"]))
        check("days carry max / min / rain", all("maxTempC" in d and "rainMm" in d for d in w["days"]), w["days"][:1])
        check("at most 3 advisories", len(w["advisories"]) <= 3, w["advisories"])
        print(f"    (forecast {secs:.2f}s; advisories: {[a['code'] for a in w['advisories']]})")
        st2, w2, secs2 = call(base, "GET", "api/Farmer/weather", token=gps)
        check("weather: second call from the cache (same fetch time)", w2["fetchedAtUtc"] == w["fetchedAtUtc"], (w["fetchedAtUtc"], w2["fetchedAtUtc"]))
    elif st == 200:
        check("Open-Meteo unreachable: status Unavailable (2), section omitted", w["status"] == 2, w)
        print("    (Open-Meteo was not reachable from this machine; the Unavailable path was checked instead)")

    print()
    if failures:
        print(f"{len(failures)} FAILED: " + ", ".join(failures))
        sys.exit(1)
    print("All Task 3 smoke checks passed.")


if __name__ == "__main__":
    main()
