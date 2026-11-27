"""
Farmer portal Task 4 smoke test: fertilizer recommendation, against a LOCAL API and the LOCAL spicone_dev
database only.

    psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task4_local.sql
    python tools/farmer-portal-check/smoke_task4.py [--base http://localhost:5120]

Needs the API in the Development environment with Sms:Provider = Log (devCode) on the local DB:

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

Signs in as the local test farmer 6777777703 (linked to the demo soil report of seed_task4_local.sql), the
unlinked local test farmer 6163665844 and qa.mdo (staff). Saves a few fertilizer runs for those test farmers
(cleanup_task4_local.sql removes them). No external calls.
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from smoke_task3 import call, check, failures, farmer_token, staff_token, qa_password  # noqa: E402


def norm_id(options, crop, variant_start, soil=None):
    for c in options["crops"]:
        if c["crop"] != crop:
            continue
        for v in c["variants"]:
            if v["variant"].startswith(variant_start) and v.get("soilType") == soil:
                return v["normId"]
    sys.exit(f"no norm {crop} / {variant_start} / {soil}")


def kg(lines, code):
    return next((l["kg"] for l in lines if l["code"] == code), 0)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703", help="local test farmer linked to the demo soil report")
    ap.add_argument("--other-phone", default="6163665844", help="local test farmer without a soil report")
    args = ap.parse_args()
    host = args.base.split("//")[-1].split(":")[0].split("/")[0]
    if host not in ("localhost", "127.0.0.1"):
        sys.exit("smoke_task4.py runs against a LOCAL API only")
    base = args.base

    st, _, _ = call(base, "GET", "api/Farmer/fertilizer/options")
    check("options: 401 without a token", st == 401, st)
    staff = staff_token(base, "qa.mdo", qa_password())
    st, _, _ = call(base, "GET", "api/Farmer/fertilizer/options", token=staff)
    check("options: 403 for staff", st == 403, st)
    st, _, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", {"normId": 1}, token=staff)
    check("recommend: 403 for staff", st == 403, st)

    tok = farmer_token(base, args.phone)
    other = farmer_token(base, args.other_phone)

    st, options, _ = call(base, "GET", "api/Farmer/fertilizer/options", token=tok)
    check("options: 200", st == 200, options)
    crops = [c["crop"] for c in options["crops"]]
    check("options: seeded crops", {"Paddy", "Cotton", "Maize", "Banana", "Coconut", "Turmeric"} <= set(crops), crops)
    check("options: no sugarcane (left out: sources disagree)", "Sugarcane" not in crops, crops)
    check("options: seven soil types", len(options["soilTypes"]) == 7, options["soilTypes"])
    ranges = {r["code"]: r for r in options["ranges"]}
    check("options: N range from the lab master 280 / 560", ranges["N"]["lowBelow"] == 280 and ranges["N"]["highAbove"] == 560 and ranges["N"]["fromLabMaster"], ranges["N"])
    check("options: P range from the lab master 22 / 56, slider to 90", ranges["P"]["lowBelow"] == 22 and ranges["P"]["highAbove"] == 56 and ranges["P"]["max"] == 90, ranges["P"])
    check("options: K range from the lab master 110 / 280", ranges["K"]["lowBelow"] == 110 and ranges["K"]["highAbove"] == 280, ranges["K"])
    check("options: pH 4.5-9.5, default 7.0, lab labels", ranges["pH"]["min"] == 4.5 and ranges["pH"]["max"] == 9.5 and ranges["pH"]["default"] == 7.0
          and ranges["pH"]["lowLabel"] == "Acidic" and ranges["pH"]["highLabel"] == "Alkaline", ranges["pH"])
    check("options: products with grades; SPIC names only for urea and DAP",
          {p["code"]: p.get("spicName") for p in options["products"]} == {"DAP": "SPIC DAP", "Urea": "SPIC Urea", "MOP": None, "SSP": None}, options["products"])

    # ---- soil report
    st, report, _ = call(base, "GET", "api/Farmer/fertilizer/soil-report", token=tok)
    check("soil report: linked farmer gets the demo report", st == 200 and report.get("available") and report.get("reportCode") == "DEMO-T4-RPT-1", report)
    check("soil report: N 180 / P 18 / K 95 / pH 8.1 (kg/ha, no conversion)",
          report.get("n") == 180 and report.get("p") == 18 and report.get("k") == 95 and report.get("ph") == 8.1 and not report.get("notes"), report)
    st, none, _ = call(base, "GET", "api/Farmer/fertilizer/soil-report", token=other)
    check("soil report: an unlinked farmer gets a plain message", st == 200 and not none.get("available") and none.get("message"), none)

    # ---- the sketch: cotton hybrids on black cotton soil, N 100, P 5, K 50
    hybrids = norm_id(options, "Cotton", "Irrigated hybrids")
    body = {"normId": hybrids, "soilType": "Black Cotton Soil", "areaAcres": 1, "n": 100, "p": 5, "k": 50, "ph": 7.0}
    st, r, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", body, token=tok)
    check("recommend: 200", st == 200, r)
    if st == 200:
        levels = [n["level"] for n in r["nutrients"]]
        check("recommend: N, P, K Low", levels == ["Low", "Low", "Low"], levels)
        check("recommend: dose 150:75:75 kg/ha after +25 %", [n["adjustedDose"] for n in r["nutrients"]] == [150, 75, 75], r["nutrients"])
        check("recommend: need 60.7 : 30.4 : 30.4 kg/acre", [n["needKgPerAcre"] for n in r["nutrients"]] == [60.7, 30.4, 30.4], r["nutrients"])
        check("recommend: basal DAP 66 kg, urea 18 kg, MOP 25 kg",
              (kg(r["stages"][0]["products"], "DAP"), kg(r["stages"][0]["products"], "Urea"), kg(r["stages"][0]["products"], "MOP")) == (66, 18, 25), r["stages"][0])
        check("recommend: season urea 106 kg, DAP 66 kg, MOP 51 kg",
              (kg(r["totals"], "Urea"), kg(r["totals"], "DAP"), kg(r["totals"], "MOP")) == (106, 66, 51), r["totals"])
        check("recommend: pH 7.0 neutral", r["ph"]["band"] == "Neutral", r["ph"])
        check("recommend: water and soil tips for black soil cotton", any(t["text"].startswith("Heavy (black) soil") for t in r["waterTips"])
              and any(t["text"].startswith("Black soil") for t in r["soilTips"]), r["waterTips"])
        check("recommend: every tip has a source", all(t["sourceUrl"].startswith("https://agritech.tnau.ac.in/") for t in r["waterTips"] + r["soilTips"] + r["nutrientTips"]))
        check("recommend: disclaimer", r["disclaimer"].startswith("General guidance from TNAU/ICAR norms."), r["disclaimer"])
        check("recommend: saved as a run", r["runId"] > 0, r.get("runId"))
        first_run = r["runId"]

    # ---- acidic laterite paddy from the soil report values
    paddy = norm_id(options, "Paddy", "Medium duration")
    body = {"normId": paddy, "soilType": "Laterite Soil", "areaAcres": 2.5, "n": 180, "p": 18, "k": 95, "ph": 5.2, "soilReportId": report.get("reportId")}
    st, r2, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", body, token=tok)
    check("recommend: paddy with the report id 200", st == 200 and r2.get("fromSoilReport"), r2)
    if st == 200:
        check("recommend: pH 5.2 strongly acidic, lime requirement test", r2["ph"]["band"] == "StronglyAcidic"
              and any("lime requirement test" in a for a in r2["ph"]["advice"]), r2["ph"])
        check("recommend: paddy four stages", len(r2["stages"]) == 4, r2["stages"])

    # another farmer cannot claim this report
    body["soilReportId"] = report.get("reportId")
    st, err, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", body, token=other)
    check("recommend: someone else's report id is refused (400)", st == 400 and "soilReportId" in err.get("errors", {}), err)

    # ---- validation
    st, err, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", {"normId": 99999, "soilType": "Red Soil", "areaAcres": 1, "n": 1, "p": 1, "k": 1, "ph": 7}, token=tok)
    check("recommend: unknown crop 400", st == 400 and "normId" in err.get("errors", {}), err)
    maize_black = norm_id(options, "Maize", "Rainfed", "Black Cotton Soil")
    st, err, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", {"normId": maize_black, "soilType": "Red Soil", "areaAcres": 1, "n": 1, "p": 1, "k": 1, "ph": 7}, token=tok)
    check("recommend: soil-specific norm on another soil 400", st == 400 and "soilType" in err.get("errors", {}), err)
    st, err, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", {"normId": paddy, "soilType": "Red Soil", "areaAcres": 0, "n": 1, "p": 1, "k": 1, "ph": 12}, token=tok)
    check("recommend: area and pH errors", st == 400 and {"areaAcres", "ph"} <= set(err.get("errors", {})), err)

    # ---- banana per plant
    banana = norm_id(options, "Banana", "Garden land, varieties")
    st, r3, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", {"normId": banana, "soilType": "Alluvial Soil", "areaAcres": 1, "n": 300, "p": 30, "k": 150, "ph": 7}, token=tok)
    check("recommend: banana 1,246 plants, grams per plant", st == 200 and r3["plantCount"] == 1246 and all(p.get("gramsPerPlant") for s in r3["stages"] for p in s["products"]), r3 if st != 200 else r3["plantCount"])

    # ---- history
    st, hist, _ = call(base, "GET", "api/Farmer/fertilizer/history", token=tok)
    check("history: newest first, at most 20", st == 200 and 1 <= len(hist) <= 20 and hist[0]["crop"] == "Banana", hist[:2] if isinstance(hist, list) else hist)
    check("history: summary fields for later tasks", st == 200 and all(k in hist[0] for k in ("nStatus", "needNKgPerAcre", "summary", "soilPh")), hist[0] if hist else hist)
    st, other_hist, _ = call(base, "GET", "api/Farmer/fertilizer/history", token=other)
    check("history: scoped to the caller", st == 200 and all(h["id"] not in {x["id"] for x in hist} for h in other_hist), other_hist)
    st, one, _ = call(base, "GET", f"api/Farmer/fertilizer/history/{first_run}", token=tok)
    check("history: one run in full", st == 200 and one["runId"] == first_run and one["crop"] == "Cotton" and len(one["stages"]) == 3, one)
    st, _, _ = call(base, "GET", f"api/Farmer/fertilizer/history/{first_run}", token=other)
    check("history: another farmer's run is 404", st == 404, st)
    st, latest, _ = call(base, "GET", "api/Farmer/fertilizer/latest", token=tok)
    check("latest: the newest run in short", st == 200 and latest["id"] == hist[0]["id"], latest)

    print()
    print(f"smoke_task4: {'OK' if not failures else str(len(failures)) + ' FAILED'}")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
