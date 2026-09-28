#!/usr/bin/env python3
"""
End-to-end check of the metrics & error telemetry API (api/Telemetry/batch, api/Metrics/...,
docs/metrics-telemetry-plan.md) against a running API.

Generates tagged traffic as the QA accounts (X-Spic-Client: android, X-Spic-Version: 9.9.9), posts
telemetry batches as a signed-in user, anonymously and with the ingest key, waits for the background
writer, then asserts every Metrics read route: access (role bypass, 403, 401), live, summary, series,
users, pages, endpoints (hits / slow / errors), errors list / detail / resolve / unresolve. Rows it
creates are tagged with the run id and left in place (they age out by retention).

LOCAL ONLY: point it at a local API that uses the local database (never the live API). Start the API
with the same ingest key, e.g. (PowerShell):

    $env:Telemetry__IngestKey='local-test-key'; dotnet run --project SpicAPI --launch-profile http

    python tools/metrics-api-check/metrics_api_check.py                       # http://localhost:5034
    python tools/metrics-api-check/metrics_api_check.py --base http://localhost:5034 --ingest-key local-test-key
    python tools/metrics-api-check/metrics_api_check.py --no-db                # skip the direct table checks

The 404 status and "api/Telemetry is never recorded" checks read AppRequestLogs directly (psycopg2,
--db DSN); use --no-db to skip them.
"""
import argparse
import datetime as dt
import re
import sys
import time

import requests

PARSER = argparse.ArgumentParser(description="Metrics / telemetry API end-to-end check (local only)")
PARSER.add_argument("--base", default="http://localhost:5034")
PARSER.add_argument("--password", default="QaPass@2026#")
PARSER.add_argument("--ingest-key", default="local-test-key")
PARSER.add_argument("--db", default="host=localhost port=5432 dbname=spicone_dev user=postgres password=1234")
PARSER.add_argument("--no-db", action="store_true")
ARGS = PARSER.parse_args()

BASE = ARGS.base.rstrip("/") + "/"
if not re.match(r"^https?://(localhost|127\.0\.0\.1|\[::1\])(:\d+)?/$", BASE):
    sys.exit(f"Refusing to run against {BASE}: this check writes test data and is for a LOCAL API only.")

# enum values (serialized as integers)
APP_UNKNOWN, APP_WEB, APP_ANDROID = 0, 1, 2
SRC_API, SRC_WEBHOST, SRC_CLIENT, SRC_JS = 0, 1, 2, 3
KIND_PAGE, KIND_ENDPOINT = 0, 1

CLIENT_HEADERS = {"X-Spic-Client": "android", "X-Spic-Version": "9.9.9"}
READ_ROUTES = [
    "api/Metrics/summary?days=7",
    "api/Metrics/live",
    "api/Metrics/series?days=7",
    "api/Metrics/pages?days=7",
    "api/Metrics/endpoints?days=7",
    "api/Metrics/users?days=7",
    "api/Metrics/errors?days=7",
]

PASSED, FAILED = [], []


def ok(cond, msg):
    (PASSED if cond else FAILED).append(msg)
    print(f"  {'PASS' if cond else 'FAIL'}  {msg}")
    return cond


def need(cond, msg):
    if not ok(cond, msg):
        summary()
        sys.exit(f"stopping: {msg}")


def eq(actual, expected, msg):
    return ok(actual == expected, f"{msg} (expected {expected!r}, got {actual!r})")


def section(title):
    print(f"\n== {title}")


def summary():
    print(f"\n{len(PASSED)} / {len(PASSED) + len(FAILED)} checks passed, {len(FAILED)} failed")
    for f in FAILED:
        print(f"  - {f}")


class Client:
    def __init__(self, user=None, headers=None):
        self.user = user or "anonymous"
        self.s = requests.Session()
        if headers:
            self.s.headers.update(headers)
        if user:
            r = requests.post(BASE + "api/Authentication/login", json={"userName": user, "password": ARGS.password}, timeout=30)
            need(r.status_code == 200, f"login {user} -> {r.status_code} {r.text[:200]}")
            token = r.json().get("token") or r.json().get("Token")
            token = token[7:] if token.lower().startswith("bearer ") else token
            self.s.headers["Authorization"] = f"Bearer {token}"

    def call(self, method, path, expect=None, **kw):
        r = self.s.request(method, BASE + path.lstrip("/"), timeout=60, **kw)
        if expect is not None:
            ok(r.status_code == expect, f"{self.user} {method} {path} -> {r.status_code} (expected {expect}) {r.text[:200] if r.status_code != expect else ''}")
        return r

    def get(self, path, expect=None, **kw):
        return self.call("GET", path, expect, **kw)

    def j(self, method, path, expect=200, **kw):
        r = self.call(method, path, expect, **kw)
        need(r.status_code == expect, f"{self.user} {method} {path} must return {expect}")
        return r.json() if r.content else None


def now_iso(offset_days=0):
    return (dt.datetime.now(dt.timezone.utc) + dt.timedelta(days=offset_days)).strftime("%Y-%m-%dT%H:%M:%S.%fZ")


RUN = dt.datetime.now().strftime("%m%d%H%M%S")
TAG = f"qametrics{RUN}"
print(f"Metrics API check against {BASE} (run {RUN})")

# ------------------------------------------------------------------------------------------ sign in
section("sign in")
# qa.admin: role Admin, designation 9 "QA Admin All Pages". Every call carries the android headers
# so its last activity is attributed to Android 9.9.9.
admin = Client("qa.admin", CLIENT_HEADERS)
# Role Admin, designation 2 "Plain Staff QA" (no Metrics key): allowed by the role bypass.
plain = Client("qa.plainadmin")
# Role MO, designation without the Metrics key: refused.
mo = Client("qa.mo")
anon = Client()

# ------------------------------------------------------------------------------------------ access
section("access: role bypass, 403, 401")
for path in READ_ROUTES:
    admin.get(path, expect=200)
    plain.get(path, expect=200)
    mo.get(path, expect=403)
    anon.get(path, expect=401)
mo.get("api/Metrics/errors/1", expect=403)
mo.call("POST", "api/Metrics/errors/abc/resolve", expect=403, json={"resolved": True})
anon.call("POST", "api/Metrics/errors/abc/resolve", expect=401, json={"resolved": True})
r = mo.get("api/Metrics/summary")
ok(r.status_code == 403 and r.json().get("success") is False and r.json().get("message"), f"403 body is {{ success:false, message }}: {r.text[:120]}")

# ------------------------------------------------------------------------------------------ traffic
section("traffic with client headers")
for _ in range(3):
    admin.get("api/Lab/me", expect=200)
for _ in range(2):
    admin.get("api/Sas/payments/me", expect=200)
admin.get(f"api/NoSuchThing{RUN}/12345", expect=404)
anon.get("health", expect=200)
anon.get("swagger/index.html")
anon.get("")

# ------------------------------------------------------------------------------------------ ingest
section("ingest: user, anonymous, ingest key, garbage")
user_batch = {
    "app": APP_ANDROID,
    "appVersion": "9.9.9",
    "pageViews": [
        {"route": f"/QaMetrics{RUN}/Page/123", "at": now_iso()},
        {"route": f"/QaMetrics{RUN}/Page/456?secret=1", "at": now_iso(30)},  # future At -> server time, query dropped
    ],
    "errors": [{
        "source": SRC_CLIENT, "at": now_iso(), "exceptionType": "QaMetricsException",
        "message": f"QA metrics check {TAG} user error", "stackTrace": "at Qa.Metrics.Check.Run()\n at Qa.Metrics.Main()",
        "route": f"/QaMetrics{RUN}/Page/123", "category": "QaMetrics",
        "userId": "spoofed", "userName": "spoofed.user", "role": "SuperAdmin",
    }],
}
res = admin.j("POST", "api/Telemetry/batch", expect=202, json=user_batch)
eq((res or {}).get("accepted"), 3, "user batch accepted")
eq((res or {}).get("dropped"), 0, "user batch dropped")

anon_batch = {
    "app": APP_WEB,
    "pageViews": [{"route": f"/QaMetrics{RUN}/Anon", "at": now_iso()}, {"route": f"/QaMetrics{RUN}/Anon2", "at": now_iso()}],
    "errors": [{"source": SRC_JS, "exceptionType": "QaAnonError", "message": f"anon {TAG} error {i}", "route": "/login"} for i in range(7)],
}
res = anon.j("POST", "api/Telemetry/batch", expect=202, json=anon_batch)
eq((res or {}).get("accepted"), 5, "anonymous batch: only 5 errors accepted")
eq((res or {}).get("dropped"), 4, "anonymous batch: page views + extra errors dropped")

keyed = Client(headers={"X-Telemetry-Key": ARGS.ingest_key})
keyed.user = "ingest-key"
key_batch = {
    "app": APP_WEB,
    "appVersion": "web-1",
    "pageViews": [{"route": f"/QaMetrics{RUN}/Server", "at": now_iso(), "sessionId": "circuit-1"}],
    "errors": [{"source": SRC_WEBHOST, "exceptionType": "QaWebHostException", "message": f"webhost {TAG} circuit error",
                "category": "Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost", "userId": "u-webhost",
                "userName": "qa.webhost", "role": "MO", "sessionId": "circuit-1"}],
}
res = keyed.j("POST", "api/Telemetry/batch", expect=202, json=key_batch)
eq((res or {}).get("accepted"), 2, "ingest-key batch accepted (page view + error)")

wrong = Client(headers={"X-Telemetry-Key": "not-the-key"})
wrong.user = "wrong-key"
res = wrong.j("POST", "api/Telemetry/batch", expect=202, json={"app": APP_WEB, "pageViews": [{"route": "/x"}], "errors": []})
eq((res or {}).get("accepted"), 0, "wrong key is anonymous: page view dropped")

r = anon.call("POST", "api/Telemetry/batch", data="{not json", headers={"Content-Type": "application/json"})
ok(400 <= r.status_code < 500, f"garbage JSON -> {r.status_code} (4xx, not 500)")
r = anon.call("POST", "api/Telemetry/batch", data="", headers={"Content-Type": "application/json"})
ok(r.status_code < 500, f"empty body -> {r.status_code} (not 500)")
r = anon.call("POST", "api/Telemetry/batch", json={"app": 99, "errors": [None, {"source": 42, "message": "x" * 5000}]})
ok(r.status_code == 202, f"odd enum values / nulls / long strings -> {r.status_code} (202)")

print("  (waiting 3.5 s for the background writer)")
time.sleep(3.5)

# ------------------------------------------------------------------------------------------ live / summary / series
section("live, summary, series")
live = admin.j("GET", "api/Metrics/live")
ok(live["requestsLastHour"] > 0, f"live requestsLastHour > 0 ({live['requestsLastHour']})")
ok(live["activeUsers15Min"] >= 1, f"live activeUsers15Min >= 1 ({live['activeUsers15Min']})")
ok(live["errorsLastHour"] >= 7, f"live errorsLastHour >= 7 ({live['errorsLastHour']})")

s = admin.j("GET", "api/Metrics/summary?days=7")
eq(s["days"], 7, "summary days")
ok(s["activeUsersToday"] >= 1, f"summary activeUsersToday >= 1 ({s['activeUsersToday']})")
ok(s["activeUsersPeriod"] >= s["activeUsersToday"], "summary activeUsersPeriod >= today")
ok(s["totalUsers"] >= 1, f"summary totalUsers >= 1 ({s['totalUsers']})")
ok(s["requestsToday"] > 0 and s["pageViewsToday"] >= 3, f"summary requests / page views today ({s['requestsToday']}, {s['pageViewsToday']})")
ok(s["errorsToday"] >= 7, f"summary errorsToday >= 7 ({s['errorsToday']})")
ok(s["openErrorGroups"] >= 1, f"summary openErrorGroups >= 1 ({s['openErrorGroups']})")
android = [a for a in s["byApp"] if a["app"] == APP_ANDROID]
ok(len(android) == 1 and android[0]["activeUsers"] >= 1 and android[0]["requests"] >= 6, f"summary byApp has Android {android}")
ok(any(r["role"] == "Admin" and r["activeUsers"] >= 1 for r in s["byRole"]), f"summary byRole has Admin {s['byRole'][:5]}")
ok(s["droppedSinceStart"] == 0, f"summary droppedSinceStart == 0 ({s['droppedSinceStart']})")
s30 = admin.j("GET", "api/Metrics/summary?days=30")
ok(s30["activeUsers30d"] >= s["activeUsersToday"], "summary?days=30 activeUsers30d")
admin.get("api/Metrics/summary?days=9999", expect=200)

series = admin.j("GET", "api/Metrics/series?days=7")
eq(len(series["points"]), 7, "series?days=7 points")
ok(series["points"][-1]["requests"] > 0, f"series today requests > 0 ({series['points'][-1]})")
ok(series["points"][-1]["day"][:10] == dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%d"), "series last point is today (UTC)")
sa = admin.j("GET", f"api/Metrics/series?days=7&app={APP_ANDROID}")
ok(sa["app"] == APP_ANDROID and sa["points"][-1]["requests"] >= 6, f"series app=Android today ({sa['points'][-1]})")

# ------------------------------------------------------------------------------------------ users
section("users")
u = admin.j("GET", "api/Metrics/users?days=7&q=qa.admin")
row = next((x for x in u["items"] if x["userName"] == "qa.admin"), None)
need(row is not None, f"users has qa.admin {u['items'][:3]}")
eq(row["lastApp"], APP_ANDROID, "qa.admin lastApp")
eq(row["lastAppVersion"], "9.9.9", "qa.admin lastAppVersion")
ok(row["requests"] >= 6 and row["pageViews"] >= 2, f"qa.admin counts (requests {row['requests']}, page views {row['pageViews']})")
ok(row["errors"] >= 1 and row["activeDays"] >= 1, f"qa.admin errors / active days ({row['errors']}, {row['activeDays']})")
eq(row["role"], "Admin", "qa.admin role")
ok(row["designation"], f"qa.admin designation ({row['designation']})")
ok(row["lastRoute"] is not None, f"qa.admin lastRoute ({row['lastRoute']})")
u = admin.j("GET", "api/Metrics/users?days=7&role=Admin&pageSize=500")
eq(u["pageSize"], 100, "users pageSize clamped to 100")
ok(all(x["role"] == "Admin" for x in u["items"]), "users role filter")
u = admin.j("GET", "api/Metrics/users?days=7&q=no-such-user-xyz")
eq(u["total"], 0, "users q without match")

# ------------------------------------------------------------------------------------------ pages / endpoints
section("pages, endpoints")
pages = admin.j("GET", "api/Metrics/pages?days=7&top=100")
routes = {p["route"]: p for p in pages}
page = routes.get(f"/QaMetrics{RUN}/Page/{{id}}")
ok(page is not None and page["hits"] == 2 and page["users"] >= 1, f"pages has /QaMetrics{RUN}/Page/{{id}} with 2 hits, ids normalised, query dropped ({page})")
ok(page is not None and page["errors"] >= 0, "pages row errors field")
ok(f"/QaMetrics{RUN}/Anon" not in routes, "anonymous page views were dropped")
ok(f"/QaMetrics{RUN}/Server" in routes, "ingest-key page view recorded")
pa = admin.j("GET", f"api/Metrics/pages?days=7&app={APP_ANDROID}&top=100")
ok(any(p["route"] == f"/QaMetrics{RUN}/Page/{{id}}" for p in pa), "pages app=Android filter")

eps = admin.j("GET", "api/Metrics/endpoints?days=7&top=100&sort=hits")
eroutes = {e["route"]: e for e in eps}
ok("GET api/Lab/me" in eroutes and eroutes["GET api/Lab/me"]["hits"] >= 3, f"endpoints has GET api/Lab/me ({eroutes.get('GET api/Lab/me')})")
ok("GET api/Sas/payments/me" in eroutes, "endpoints has GET api/Sas/payments/me")
nf = eroutes.get(f"GET api/NoSuchThing{RUN}/{{id}}")
ok(nf is not None and nf["hits"] == 1 and nf["errors"] == 0, f"404 route recorded with {{id}} normalisation, not an error ({nf})")
ok(not any("Telemetry" in k for k in eroutes), "api/Telemetry is never recorded")
ok(any(k.startswith("GET api/Metrics/") for k in eroutes), "Metrics calls are recorded as endpoints")
ok(all(e["kind"] == KIND_ENDPOINT for e in eps), "endpoint rows kind = Endpoint")
ok(eps == sorted(eps, key=lambda e: -e["hits"]) or all(eps[i]["hits"] >= eps[i + 1]["hits"] for i in range(len(eps) - 1)), "sort=hits ordered by hits")
slow = admin.j("GET", "api/Metrics/endpoints?days=7&top=10&sort=slow")
ok(all(slow[i]["p95DurationMs"] >= slow[i + 1]["p95DurationMs"] for i in range(len(slow) - 1)), "sort=slow ordered by p95")
errs = admin.j("GET", "api/Metrics/endpoints?days=7&top=10&sort=errors")
ok(all(errs[i]["errors"] >= errs[i + 1]["errors"] for i in range(len(errs) - 1)), "sort=errors ordered by errors")
ok(len(admin.j("GET", "api/Metrics/endpoints?days=7&top=1")) <= 1, "endpoints top=1")

# ------------------------------------------------------------------------------------------ errors
section("errors: list, detail, resolve, unresolve")
e = admin.j("GET", f"api/Metrics/errors?days=7&q={TAG}%20user")
need(e["total"] == 1 and len(e["items"]) == 1, f"errors q=user error finds one group ({e['total']})")
g = e["items"][0]
eq(g["source"], SRC_CLIENT, "user error source")
eq(g["app"], APP_ANDROID, "user error app")
eq(g["count"], 1, "user error count")
eq(g["isResolved"], False, "user error open")
eq(len(g["fingerprint"]), 64, "fingerprint is 64 hex chars")
d = admin.j("GET", f"api/Metrics/errors/{g['lastId']}")
eq(d["fingerprint"], g["fingerprint"], "detail fingerprint")
eq(d["userName"], "qa.admin", "user error attributed to the token, not the body")
ok(d["role"] == "Admin" and d["userId"] != "spoofed", "body user fields ignored for a bearer caller")
eq(d["route"], f"/QaMetrics{RUN}/Page/{{id}}", "error route normalised")
eq(d["appVersion"], "9.9.9", "detail appVersion")
ok(d["stackTrace"] and d["occurrences"] >= 1 and len(d["recent"]) >= 1, "detail stack / occurrences / recent")
admin.get("api/Metrics/errors/999999999999", expect=404)

res = admin.j("POST", f"api/Metrics/errors/{g['fingerprint']}/resolve", json={"resolved": True})
ok(res["isResolved"] is True and res["resolvedBy"] == "qa.admin" and res["resolvedAt"], f"resolve sets isResolved / resolvedBy / resolvedAt ({res['resolvedBy']})")
e = admin.j("GET", f"api/Metrics/errors?days=7&q={TAG}%20user&resolved=true")
ok(any(x["fingerprint"] == g["fingerprint"] for x in e["items"]), "resolved=true lists the group")
e = admin.j("GET", f"api/Metrics/errors?days=7&q={TAG}%20user&resolved=false")
ok(not any(x["fingerprint"] == g["fingerprint"] for x in e["items"]), "resolved=false hides the group")
d = admin.j("GET", f"api/Metrics/errors/{g['lastId']}")
eq(d["isResolved"], True, "detail isResolved after resolve")
res = admin.j("POST", f"api/Metrics/errors/{g['fingerprint']}/resolve", json={"resolved": False})
ok(res["isResolved"] is False and res["resolvedBy"] is None, "unresolve clears the flag")
plain.call("POST", "api/Metrics/errors/ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff/resolve", expect=404, json={"resolved": True})

e = admin.j("GET", f"api/Metrics/errors?days=7&q=anon%20{TAG}")
ok(e["total"] == 1 and e["items"][0]["count"] == 5, f"anonymous errors: one group of 5 ({[(x['count'], x['message']) for x in e['items']]})")
ok(e["total"] == 1 and e["items"][0]["source"] == SRC_JS and e["items"][0]["users"] == 0, "anonymous errors unattributed, source Js")
e = admin.j("GET", f"api/Metrics/errors?days=7&q=webhost%20{TAG}&source={SRC_WEBHOST}")
need(e["total"] == 1, f"ingest-key error listed with source=WebHost filter ({e['total']})")
d = admin.j("GET", f"api/Metrics/errors/{e['items'][0]['lastId']}")
ok(d["userName"] == "qa.webhost" and d["source"] == SRC_WEBHOST and d["sessionId"] == "circuit-1", "ingest-key error keeps the body's user / source / session")
e = admin.j("GET", f"api/Metrics/errors?days=7&q=QaMetricsException&app={APP_ANDROID}&pageSize=1")
ok(e["pageSize"] == 1 and len(e["items"]) <= 1, "errors q on exception type + app filter + pageSize")

# ------------------------------------------------------------------------------------------ database
if not ARGS.no_db:
    section("database (direct)")
    try:
        import psycopg2
        conn = psycopg2.connect(ARGS.db)
        cur = conn.cursor()
        cur.execute('SELECT "StatusCode", "App", "AppVersion", "UserName" FROM "AppRequestLogs" WHERE "Path" = %s', (f"api/NoSuchThing{RUN}/{{id}}",))
        rows = cur.fetchall()
        ok(len(rows) == 1 and rows[0][0] == 404 and rows[0][1] == APP_ANDROID and rows[0][2] == "9.9.9" and rows[0][3] == "qa.admin",
           f"404 request row: status / app / version / user ({rows})")
        cur.execute('SELECT COUNT(*) FROM "AppRequestLogs" WHERE "Path" ILIKE %s', ("%Telemetry%",))
        eq(cur.fetchone()[0], 0, "no AppRequestLogs row for api/Telemetry")
        cur.execute('SELECT COUNT(*) FROM "AppRequestLogs" WHERE "Path" LIKE %s OR "Path" LIKE %s', ("%?%", "%secret%"))
        eq(cur.fetchone()[0], 0, "no query strings stored")
        cur.execute('SELECT COUNT(*) FROM "AppRequestLogs" WHERE "Path" IN (%s, %s)', ("health", "swagger/index.html"))
        eq(cur.fetchone()[0], 0, "excluded paths not recorded")
        conn.close()
    except ImportError:
        ok(False, "psycopg2 is not installed (pip install psycopg2-binary, or use --no-db)")

summary()
sys.exit(1 if FAILED else 0)
