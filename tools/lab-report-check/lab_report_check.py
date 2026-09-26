#!/usr/bin/env python3
"""
SAS Lab report check (docs/sas-lab-portal-plan.md, reports workstream).

Generates the sample reports of one batch through the API and asserts:
  * one report per sample (two for Soil & Water samples), no pending reports;
  * codes RPT-SAS-yyyy-nnn (unique), batch code REP-SAS-yyyy-nnn, financial year = April-March;
  * generation is idempotent (a second call creates nothing);
  * every report's PDF (each offered language) and XLSX download with the right content type,
    and a download moves the status to Downloaded and counts it;
  * the batch download as one PDF and as a zip of PDFs named by report code;
  * ?access_token= works for the file routes; POST printed sets Printed and a later download keeps it;
  * optional: a farmer account sees only its own reports, no batch routes, farmer languages only.

Local use only (never the live API). Standard library only.

  python tools/lab-report-check/lab_report_check.py --batch-id 3 \
      --user qa.labcoord --farmer-user qa.farmer \
      --generate-route "api/Lab/batches/{id}/status?status=Completed" --generate-method PATCH

Password: --password or env LAB_CHECK_PASSWORD. Exit code 0 when every check passes.
"""
import argparse
import io
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from datetime import datetime

PDF = "application/pdf"
XLSX = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
ZIP = "application/zip"

RESULTS = []


def check(ok, label, detail=""):
    RESULTS.append((bool(ok), label, detail))
    print(("PASS " if ok else "FAIL ") + label + (f"  ({detail})" if detail else ""))
    return ok


class Api:
    def __init__(self, base, token=None):
        self.base = base.rstrip("/") + "/"
        self.token = token

    def call(self, method, path, body=None, auth=True, raw=False):
        url = self.base + path.lstrip("/")
        data = None
        headers = {"Accept": "*/*"}
        if body is not None:
            data = json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json"
        if auth and self.token:
            headers["Authorization"] = "Bearer " + self.token
        req = urllib.request.Request(url, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(req, timeout=180) as resp:
                content = resp.read()
                return resp.status, dict(resp.headers), content if raw else _json(content)
        except urllib.error.HTTPError as e:
            content = e.read()
            return e.code, dict(e.headers), content if raw else _json(content)

    def get(self, path, **kw):
        return self.call("GET", path, **kw)


def _json(content):
    try:
        return json.loads(content.decode("utf-8")) if content else None
    except (ValueError, UnicodeDecodeError):
        return content


def login(base, user, password):
    api = Api(base)
    status, _, body = api.call("POST", "api/Authentication/login", {"userName": user, "UserName": user, "password": password, "Password": password}, auth=False)
    token = None
    if isinstance(body, dict):
        for key in ("token", "Token", "accessToken", "AccessToken", "jwt"):
            if body.get(key):
                token = body[key]
                break
        if token is None and isinstance(body.get("data"), dict):
            token = body["data"].get("token") or body["data"].get("Token")
    if not token:
        raise SystemExit(f"login failed for {user}: HTTP {status} {str(body)[:200]}")
    if token.lower().startswith("bearer "):  # the login response carries "Bearer <jwt>"
        token = token[7:]
    return Api(base, token)


def ctype(headers):
    for k, v in headers.items():
        if k.lower() == "content-type":
            return v.split(";")[0].strip()
    return ""


def header(headers, name):
    for k, v in headers.items():
        if k.lower() == name.lower():
            return v
    return None


def fy_start(dt):
    return dt.year if dt.month >= 4 else dt.year - 1


def all_reports(api, batch_id):
    items, page = [], 1
    while True:
        status, _, body = api.get(f"api/Lab/reports?batchId={batch_id}&page={page}&pageSize=50")
        if status != 200:
            raise SystemExit(f"reports list failed: HTTP {status} {body}")
        items += body["items"]
        if not body.get("hasMore"):
            return items
        page += 1


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base", default="http://localhost:5122")
    ap.add_argument("--batch-id", type=int, required=True)
    ap.add_argument("--user", default="qa.labcoord", help="lab coordinator or admin account")
    ap.add_argument("--password", default=os.environ.get("LAB_CHECK_PASSWORD"))
    ap.add_argument("--farmer-user", help="optional farmer account for the scoping checks")
    ap.add_argument("--generate-route", default="api/Lab/batches/{id}/status?status=Completed",
                    help="route that creates the reports (called twice to prove idempotency)")
    ap.add_argument("--no-generate", action="store_true", help="skip generation (reports already exist)")
    ap.add_argument("--generate-method", default="PATCH")
    ap.add_argument("--out", help="folder to save the downloaded files into")
    args = ap.parse_args()
    if not args.password:
        raise SystemExit("password missing: --password or LAB_CHECK_PASSWORD")

    api = login(args.base, args.user, args.password)
    bid = args.batch_id

    # ---------------------------------------------------------------- generate (twice)
    if args.generate_route and not args.no_generate:
        route = args.generate_route.replace("{id}", str(bid))
        s1, _, b1 = api.call(args.generate_method, route)
        before = all_reports(api, bid)
        s2, _, b2 = api.call(args.generate_method, route)
        after = all_reports(api, bid)
        check(s1 < 300 or before, "generate call", f"HTTP {s1}" + ("" if s1 < 300 else f" {str(b1)[:120]}; reports already exist"))
        check(len(before) == len(after), "generation is idempotent", f"{len(before)} -> {len(after)} reports (second call HTTP {s2})")

    # ---------------------------------------------------------------- batch summary and codes
    status, _, batch = api.get(f"api/Lab/batches/{bid}/report")
    if not check(status == 200, "GET batches/{id}/report", f"HTTP {status}"):
        return finish()
    summary = batch["summary"]
    reports = all_reports(api, bid)
    expected = summary["soilSamples"] + summary["waterSamples"]
    check(len(reports) == expected, "one report per sample (two for Soil & Water)",
          f"{len(reports)} reports, soil {summary['soilSamples']} + water {summary['waterSamples']}")
    check(summary["reportsGenerated"] == expected and summary["pendingReports"] == 0, "batch summary counts",
          f"generated {summary['reportsGenerated']}, pending {summary['pendingReports']}")
    check(re.fullmatch(r"REP-SAS-\d{4}-\d{3,}", summary.get("reportCode") or ""), "batch report code", summary.get("reportCode"))
    codes = [r["code"] for r in reports]
    check(all(re.fullmatch(r"RPT-SAS-\d{4}-\d{3,}", c) for c in codes) and len(set(codes)) == len(codes),
          "sample report codes RPT-SAS-yyyy-nnn, unique", ", ".join(codes))
    fy_ok = all(r["financialYearStart"] == fy_start(datetime.fromisoformat(r["generatedAt"][:19])) for r in reports)
    check(fy_ok, "financial year start = April-March of GeneratedAt")
    check(len(batch["sampleReports"]) == len(reports), "batch report lists every sample report")

    status, _, stats = api.get("api/Lab/reports/stats")
    check(status == 200 and stats["totalReports"] >= len(reports), "GET reports/stats", json.dumps(stats))
    status, _, rb = api.get(f"api/Lab/reports/batches?q={urllib.parse.quote(summary['batchCode'])}")
    check(status == 200 and any(r["batchId"] == bid for r in rb["items"]), "batch in reports/batches")

    # ---------------------------------------------------------------- files per report
    out = args.out
    if out:
        os.makedirs(out, exist_ok=True)
    for r in reports:
        status, _, detail = api.get(f"api/Lab/reports/{r['id']}")
        check(status == 200 and detail["sample"]["parameters"], f"detail {r['code']}",
              f"{len(detail['sample']['parameters'])} rows, overall {detail['sample']['overallStatusText']}")
        before_count = detail["summary"]["downloadCount"]
        for lang in detail["languages"]:
            s, h, content = api.get(f"api/Lab/reports/{r['id']}/pdf?lang={lang}", raw=True)
            fb = header(h, "X-Report-Language-Fallback")
            check(s == 200 and ctype(h) == PDF and content[:4] == b"%PDF", f"pdf {r['code']} {lang}",
                  f"{len(content)} bytes" + (f", fallback from {fb}" if fb else ""))
            if out and s == 200:
                open(os.path.join(out, f"{r['code']}-{lang}.pdf"), "wb").write(content)
        s, h, content = api.get(f"api/Lab/reports/{r['id']}/xlsx?lang=en", raw=True)
        check(s == 200 and ctype(h) == XLSX and content[:2] == b"PK", f"xlsx {r['code']}", f"{len(content)} bytes")
        if out and s == 200:
            open(os.path.join(out, f"{r['code']}-en.xlsx"), "wb").write(content)
        _, _, again = api.get(f"api/Lab/reports/{r['id']}")
        downloads = len(detail["languages"]) + 1
        check(again["summary"]["downloadCount"] == before_count + downloads and again["summary"]["status"] in (1, 2),
              f"download counted {r['code']}", f"{before_count} -> {again['summary']['downloadCount']}, status {again['summary']['status']}")

    # ---------------------------------------------------------------- access_token on file routes
    first = reports[0]
    s, h, content = Api(args.base).get(f"api/Lab/reports/{first['id']}/pdf?lang=en&access_token={api.token}", auth=False, raw=True)
    check(s == 200 and ctype(h) == PDF, "pdf with ?access_token=", f"HTTP {s}")

    # ---------------------------------------------------------------- batch downloads
    s, h, content = api.get(f"api/Lab/batches/{bid}/report/download?lang=en&format=pdf", raw=True)
    check(s == 200 and ctype(h) == PDF and content[:4] == b"%PDF", "batch download pdf", f"{len(content)} bytes")
    if out and s == 200:
        open(os.path.join(out, f"{summary['batchCode']}-en.pdf"), "wb").write(content)
    s, h, content = api.get(f"api/Lab/batches/{bid}/report/download?lang=en&format=zip", raw=True)
    ok = s == 200 and ctype(h) == ZIP
    names = []
    if ok:
        names = zipfile.ZipFile(io.BytesIO(content)).namelist()
    check(ok and sorted(n.split("-en.pdf")[0] for n in names) == sorted(codes), "batch download zip named by report code", ", ".join(names))

    # ---------------------------------------------------------------- printed
    s, _, row = api.call("POST", f"api/Lab/reports/{first['id']}/printed")
    check(s == 200 and row["status"] == 2, "POST printed -> Printed", f"HTTP {s}")
    api.get(f"api/Lab/reports/{first['id']}/pdf?lang=en", raw=True)
    _, _, d = api.get(f"api/Lab/reports/{first['id']}")
    check(d["summary"]["status"] == 2, "download after printed keeps Printed")

    # ---------------------------------------------------------------- farmer scoping
    if args.farmer_user:
        farmer = login(args.base, args.farmer_user, args.password)
        s, _, mine = farmer.get("api/Lab/reports?pageSize=50")
        check(s == 200, "farmer: reports list", f"{mine['total'] if s == 200 else s} report(s)")
        mine_ids = {r["id"] for r in mine["items"]} if s == 200 else set()
        others = [r for r in reports if r["id"] not in mine_ids]
        if others:
            s, _, _ = farmer.get(f"api/Lab/reports/{others[0]['id']}")
            check(s == 403, "farmer: other farmer's report is 403", f"HTTP {s}")
        s, _, _ = farmer.get(f"api/Lab/batches/{bid}/report")
        check(s == 403, "farmer: batch report is 403", f"HTTP {s}")
        if mine_ids:
            rid = next(iter(mine_ids))
            s, h, _ = farmer.get(f"api/Lab/reports/{rid}/pdf?lang=ta", raw=True)
            check(s == 200 and ctype(h) == PDF, "farmer: own pdf in Tamil", f"HTTP {s}")
            s, _, _ = farmer.get(f"api/Lab/reports/{rid}/pdf?lang=te", raw=True)
            check(s == 400, "farmer: non-farmer language refused", f"HTTP {s}")
            s, _, _ = farmer.call("POST", f"api/Lab/reports/{rid}/printed")
            check(s == 403, "farmer: cannot mark printed", f"HTTP {s}")

    return finish()


def finish():
    failed = [r for r in RESULTS if not r[0]]
    print(f"\n{len(RESULTS) - len(failed)}/{len(RESULTS)} checks passed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
