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
  * optional: a farmer account sees only its own reports, no batch routes, farmer languages only;
  * phase 2b fertilizer schedule: every soil report's FertilizerSchedule has two crop columns
    (Crop1, Crop2 or Crop1 again; "General" rows when the crop has none, Banana never General),
    each row's factor / status used / adjusted quantity is recomputed from the report's parameter
    statuses and the rules of SpicAPI/appsettings.json (Sas:Lab:DoseFactors, NutrientParameters,
    NutrientDoseFactors; --appsettings), the XLSX prints the adjusted quantities, water reports
    have no schedule. --expect-cases asserts the rule cases seen in the batch (n-deficient,
    k-excess, ph-alkaline, ph-acidic, general, two-crops).

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


# ---------------------------------------------------------------- fertilizer schedule (phase 2b)

STATUS_NAMES = {0: "Normal", 1: "Deficient", 2: "Moderate", 3: "Excess"}
DEFAULT_RULES = {
    "DoseFactors": {"Deficient": 1.25, "Moderate": 1.10, "Normal": 1.00, "Excess": 0.75, "NotTested": 1.00},
    "NutrientParameters": {"N": "S-N", "P": "S-P", "K": "S-K", "Zn": "S-ZN", "Fe": "S-FE", "Mn": "S-MN", "Cu": "S-CU",
                           "S": "S-S", "B": "S-B", "Organic": "S-OC", "Gypsum": "S-PH"},
    "NutrientDoseFactors": {
        "Organic": {"Deficient": 1.25, "Moderate": 1.00, "Normal": 1.00, "Excess": 1.00, "NotTested": 1.00},
        "Gypsum": {"Deficient": 0.00, "Moderate": 1.00, "Normal": 1.00, "Excess": 1.25, "NotTested": 1.00},
    },
}
CASES = ["n-deficient", "k-excess", "ph-alkaline", "ph-acidic", "general", "two-crops"]
SEEN = {c: [] for c in CASES}


def load_rules(path):
    """Sas:Lab schedule rules from appsettings.json (full-line // comments stripped), over the defaults."""
    rules = {k: {kk: (dict(vv) if isinstance(vv, dict) else vv) for kk, vv in v.items()} for k, v in DEFAULT_RULES.items()}
    if not path or not os.path.exists(path):
        return rules
    text = "\n".join(l for l in open(path, encoding="utf-8-sig").read().splitlines() if not l.strip().startswith("//"))
    lab = (json.loads(text).get("Sas") or {}).get("Lab") or {}
    for k, v in (lab.get("DoseFactors") or {}).items():
        rules["DoseFactors"][k] = float(v)
    for k, v in (lab.get("NutrientParameters") or {}).items():
        rules["NutrientParameters"][k] = v
    for n, table in (lab.get("NutrientDoseFactors") or {}).items():
        own = rules["NutrientDoseFactors"].setdefault(n, {})
        for k, v in table.items():
            own[k] = float(v)
    return rules


def _ci(d, key):
    for k, v in d.items():
        if k.lower() == key.lower():
            return v
    return None


def expected_factor(rules, nutrient, status):
    own = _ci(rules["NutrientDoseFactors"], nutrient)
    if own is not None and _ci(own, status) is not None:
        return _ci(own, status)
    f = _ci(rules["DoseFactors"], status)
    return f if f is not None else (_ci(rules["DoseFactors"], "NotTested") or 1.0)


def money(x):
    from decimal import Decimal, ROUND_HALF_UP
    return Decimal(str(x)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def xlsx_texts(content):
    """Every cell text of an xlsx (shared strings, inline strings and numbers)."""
    z = zipfile.ZipFile(io.BytesIO(content))
    texts = set()
    for name in z.namelist():
        if name == "xl/sharedStrings.xml" or name.startswith("xl/worksheets/sheet"):
            xml = z.read(name).decode("utf-8-sig", "replace")
            # ClosedXML writes prefixed tags (<x:t>, <x:v>)
            texts.update(re.findall(r"<(?:\w+:)?t(?:\s[^>]*)?>([^<]*)</(?:\w+:)?t>", xml))
            if name != "xl/sharedStrings.xml":
                texts.update(re.findall(r"<(?:\w+:)?c [^>]*?(?<!t=\"s\")>\s*<(?:\w+:)?v>([^<]*)</(?:\w+:)?v>", xml))
    return texts


def check_schedule(rules, report, detail, general_crops):
    """Recomputes every schedule row from the report's parameter statuses; returns the rows of the first column."""
    code = report["code"]
    sched = detail["sample"].get("fertilizerSchedule")
    if report["sampleType"] == 1:
        check(sched == [], f"schedule {code}: water report has none", f"{len(sched or [])} columns")
        return []
    if not check(isinstance(sched, list) and len(sched) == 2, f"schedule {code}: two crop columns", f"{len(sched or [])} columns"):
        return []
    crop1 = (report.get("crop") or "").strip()
    c0, c1 = sched
    check(c0["crop"].lower() == crop1.lower(), f"schedule {code}: first column is Crop1", f"{c0['crop']!r} vs {crop1!r}")
    if not c1.get("crop") or c1["crop"].lower() == crop1.lower():
        check(c1["crop"].lower() == c0["crop"].lower(), f"schedule {code}: one crop printed twice", c1["crop"])
    else:
        SEEN["two-crops"].append(f"{code} {c0['crop']}+{c1['crop']}")
    for col in sched:
        crop = col["crop"] or ""
        if col["isGeneral"]:
            SEEN["general"].append(f"{code} {crop or '(none)'}")
            check(col["scheduleCrop"] == "General", f"schedule {code} {crop}: General rows", col["scheduleCrop"])
        else:
            check(col["scheduleCrop"].lower() == crop.lower() and crop, f"schedule {code} {crop}: own rows", col["scheduleCrop"])
        if crop.lower() == "banana":
            check(not col["isGeneral"], f"schedule {code}: Banana uses its own rows")
        if crop.lower() in general_crops:
            check(col["isGeneral"], f"schedule {code}: {crop} uses General")
        check(len(col["rows"]) > 0, f"schedule {code} {crop}: has rows", f"{len(col['rows'])} rows")

    status_by_code = {p["code"].upper(): (STATUS_NAMES.get(p["status"]) if p.get("status") is not None else "NotTested", p)
                      for p in detail["sample"]["parameters"] if p.get("code")}
    bad = []
    for col in sched:
        for r in col["rows"]:
            nutrients = [n.strip() for n in (r.get("nutrient") or "").split(",") if n.strip()]
            factor, status, by = 1.0, "", None
            for i, n in enumerate(nutrients):
                pcode = _ci(rules["NutrientParameters"], n)
                st = status_by_code.get((pcode or "").upper(), ("NotTested", None))[0]
                f = expected_factor(rules, n, st)
                if i == 0 or f > factor:
                    factor, status, by = f, st, n
            adjusted = money(0) if money(r["baseKgPerAcre"]) == 0 else money(float(r["baseKgPerAcre"]) * factor)
            if (money(r["factor"]) != money(factor) or r["statusUsed"] != status or money(r["adjustedKgPerAcre"]) != adjusted
                    or bool(r["notRequired"]) != (bool(nutrients) and factor == 0)):
                bad.append(f"{col['crop']}/{r['stageName']}/{r['product']}: got x{r['factor']} {r['statusUsed']} {r['adjustedKgPerAcre']}"
                           f" nr={r['notRequired']}, want x{factor} {status} {adjusted}")
            # the cases the product owner named
            if r["product"] in ("SPIC Urea", "SPIC DAP") and status == "Deficient" and by == "N" and money(r["baseKgPerAcre"]) > 0:
                SEEN["n-deficient"].append(f"{code} {r['product']} {r['baseKgPerAcre']} x{r['factor']} = {r['adjustedKgPerAcre']}")
            if r["product"] == "Potash" and status == "Excess" and money(r["baseKgPerAcre"]) > 0:
                SEEN["k-excess"].append(f"{code} Potash {r['baseKgPerAcre']} x{r['factor']} = {r['adjustedKgPerAcre']}")
            if "Gypsum" in nutrients and status == "Excess":
                SEEN["ph-alkaline"].append(f"{code} {r['product']} {r['baseKgPerAcre']} x{r['factor']} = {r['adjustedKgPerAcre']}")
            if "Gypsum" in nutrients and status == "Deficient":
                SEEN["ph-acidic"].append(f"{code} {r['product']} {r['adjustedKgPerAcre']} not required={r['notRequired']}")
    rows = sum(len(c["rows"]) for c in sched)
    check(not bad, f"schedule {code}: factors, status used and adjusted quantities", "; ".join(bad[:4]) or f"{rows} rows")
    return c0["rows"]


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
    ap.add_argument("--appsettings", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "SpicAPI", "appsettings.json"),
                    help="appsettings.json with the Sas:Lab schedule rules (defaults apply when missing)")
    ap.add_argument("--general-crops", default="Paddy", help="comma list of crops without their own schedule (must print General)")
    ap.add_argument("--expect-cases", default="", help=f"comma list of schedule cases the batch must contain: {', '.join(CASES)}")
    args = ap.parse_args()
    rules = load_rules(args.appsettings)
    general_crops = {c.strip().lower() for c in args.general_crops.split(",") if c.strip()}
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
        if before and "status=Completed" in route:
            msg = b2.get("message") if isinstance(b2, dict) else None
            check(s2 == 409 and msg == "Batch is already completed and its reports exist",
                  "completing again with reports -> 409", f"HTTP {s2} {msg}")

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
    with_reports, page = 0, 1
    while True:
        s, _, body = api.get(f"api/Lab/reports/batches?page={page}&pageSize=50")
        if s != 200:
            break
        with_reports += sum(1 for b in body["items"] if b["reportsGenerated"] > 0)
        if not body.get("hasMore"):
            break
        page += 1
    check(stats["batchGroups"] == with_reports, "reports/stats BatchGroups = batches with at least one report",
          f"{stats['batchGroups']} vs {with_reports}")

    # display ids: the batch's samples list, the report list and the report detail agree
    sample_ids, page = {}, 1
    while True:
        s, _, body = api.get(f"api/Lab/batches/{bid}/samples?page={page}&pageSize=50")
        if s != 200:
            break
        sample_ids.update({x["sampleItemId"]: x["sampleId"] for x in body["items"]})
        if not body.get("hasMore"):
            break
        page += 1
    if sample_ids:
        bad = [r["code"] for r in reports if r.get("sampleId") != sample_ids.get(r["sampleItemId"])]
        check(not bad, "report list SampleId = batch samples SampleId", ", ".join(bad) or f"{len(reports)} reports")
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
        if sample_ids:
            check(detail["sample"]["sample"]["sampleId"] == sample_ids.get(r["sampleItemId"]), f"detail SampleId (Lab Number) {r['code']}",
                  detail["sample"]["sample"]["sampleId"])
        lines = [l for g in detail["sample"]["recommendations"] for l in g["lines"]]
        doubled = [l for l in lines if re.match(r"^(\w[\w ]*?) is (high|low)\b.* because \1 is ", l, re.I)]
        check(not doubled, f"recommendation wording {r['code']}", "; ".join(doubled) or f"{len(lines)} lines")
        schedule_rows = check_schedule(rules, r, detail, general_crops)
        before_count = detail["summary"]["downloadCount"]
        for lang in detail["languages"]:
            s, h, content = api.get(f"api/Lab/reports/{r['id']}/pdf?lang={lang}", raw=True)
            fb = header(h, "X-Report-Language-Fallback")
            check(s == 200 and ctype(h) == PDF and content[:4] == b"%PDF", f"pdf {r['code']} {lang}",
                  f"{len(content)} bytes" + (f", fallback from {fb}" if fb else ""))
            # file name: the requested language when the PDF is in it, else -en with the fallback header
            name = re.search(r'filename="?([^";]+)', header(h, "Content-Disposition") or "")
            want = f"{r['code']}-{'en' if fb else lang}.pdf"
            check(name and name.group(1) == want and (fb is None or (fb == lang and lang != "en")),
                  f"pdf file name {r['code']} {lang}", name.group(1) if name else "no Content-Disposition")
            if out and s == 200:
                open(os.path.join(out, f"{r['code']}-{lang}.pdf"), "wb").write(content)
        s, h, content = api.get(f"api/Lab/reports/{r['id']}/xlsx?lang=en", raw=True)
        check(s == 200 and ctype(h) == XLSX and content[:2] == b"PK", f"xlsx {r['code']}", f"{len(content)} bytes")
        if s == 200 and schedule_rows:
            texts = xlsx_texts(content)
            missing = sorted({f"{money(x['adjustedKgPerAcre']):.2f}" for x in schedule_rows} - texts)
            check(not missing, f"xlsx {r['code']} prints the adjusted quantities", ", ".join(missing) or f"{len(schedule_rows)} rows")
        if out and s == 200:
            open(os.path.join(out, f"{r['code']}-en.xlsx"), "wb").write(content)
        _, _, again = api.get(f"api/Lab/reports/{r['id']}")
        downloads = len(detail["languages"]) + 1
        check(again["summary"]["downloadCount"] == before_count + downloads and again["summary"]["status"] in (1, 2),
              f"download counted {r['code']}", f"{before_count} -> {again['summary']['downloadCount']}, status {again['summary']['status']}")

    # ---------------------------------------------------------------- CORS: the web client can read the fallback header
    first = reports[0]
    req = urllib.request.Request(api.base + f"api/Lab/reports/{first['id']}/pdf?lang=en", method="GET",
                                 headers={"Authorization": "Bearer " + api.token, "Origin": "http://localhost:5031"})
    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            exposed = resp.headers.get("Access-Control-Expose-Headers") or ""
    except urllib.error.HTTPError as e:
        exposed = e.headers.get("Access-Control-Expose-Headers") or ""
    check("x-report-language-fallback" in exposed.lower(), "CORS exposes X-Report-Language-Fallback", exposed)

    # ---------------------------------------------------------------- access_token on file routes
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

    # ---------------------------------------------------------------- schedule cases seen
    for case in CASES:
        print(f"INFO schedule case {case}: " + ("; ".join(SEEN[case][:3]) if SEEN[case] else "not in this batch"))
    for case in [c.strip() for c in args.expect_cases.split(",") if c.strip()]:
        check(SEEN.get(case), f"schedule case {case} present", "; ".join((SEEN.get(case) or [])[:2]))

    return finish()


def finish():
    failed = [r for r in RESULTS if not r[0]]
    print(f"\n{len(RESULTS) - len(failed)}/{len(RESULTS)} checks passed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
