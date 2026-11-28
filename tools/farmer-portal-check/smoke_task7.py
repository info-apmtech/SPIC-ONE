"""
Farmer portal Task 7 smoke test: the harvest yield forecast, against a LOCAL API and the LOCAL spicone_dev database only.

    python tools/farmer-portal-check/smoke_task7.py [--base http://localhost:5120]

Needs the API in the Development environment with Sms:Provider = Log (devCode) on the local DB:

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

Signs in as the local test farmers 6777777703 (profile Paddy / Chilli, 2.5 acres, borewell, with fertilizer runs) and
6163665844 (no irrigation in the profile), and as qa.mdo (staff). Saves a few forecasts for those test farmers
(cleanup_task7_local.sql removes them). No external calls, no AI.
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from smoke_task3 import call, check, failures, farmer_token, staff_token, qa_password  # noqa: E402

SKETCH = {"crop": "Paddy", "method": "canal-borewell", "areaAcres": 3.0, "ndvi": 0.72, "ndviSource": "value",
          "rainfallMm": 850, "npkPercent": 90}


def req(**kw):
    body = dict(SKETCH)
    body.update(kw)
    return body


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--other-phone", default="6163665844")
    args = ap.parse_args()
    host = args.base.split("//")[-1].split(":")[0].split("/")[0]
    if host not in ("localhost", "127.0.0.1"):
        sys.exit("smoke_task7.py runs against a LOCAL API only")
    base = args.base

    # ---- access
    st, _, _ = call(base, "GET", "api/Farmer/yield/options")
    check("options: 401 without a token", st == 401, st)
    staff = staff_token(base, "qa.mdo", qa_password())
    st, _, _ = call(base, "GET", "api/Farmer/yield/options", token=staff)
    check("options: 403 for staff", st == 403, st)
    st, _, _ = call(base, "POST", "api/Farmer/yield/predict", SKETCH, token=staff)
    check("predict: 403 for staff", st == 403, st)
    st, _, _ = call(base, "GET", "api/Farmer/yield/history", token=staff)
    check("history: 403 for staff", st == 403, st)

    tok = farmer_token(base, args.phone)
    other = farmer_token(base, args.other_phone)

    # ---- options and prefill
    st, opt, _ = call(base, "GET", "api/Farmer/yield/options", token=tok)
    check("options: 200", st == 200, opt)
    crops = [c["crop"] for c in opt["crops"]]
    check("options: the eight seeded crops", crops == ["Paddy", "Maize", "Cotton", "Groundnut", "Sugarcane", "Banana", "Black gram", "Green gram"], crops)
    cane = next(c for c in opt["crops"] if c["crop"] == "Sugarcane")
    check("options: sugarcane has only an irrigated norm", [n["irrigationClass"] for n in cane["norms"]] == ["Irrigated"], cane)
    check("options: the sketch's four methods", [m["label"] for m in opt["methods"]] == ["Canal / Borewell Irrigated", "Rainfed", "Drip", "Sprinkler"], opt["methods"])
    check("options: NDVI 0.35-0.92 default 0.72", (opt["ndvi"]["min"], opt["ndvi"]["max"], opt["ndvi"]["default"]) == (0.35, 0.92, 0.72), opt["ndvi"])
    check("options: rainfall 400-1400 default 850", (opt["rainfall"]["min"], opt["rainfall"]["max"], opt["rainfall"]["default"]) == (400, 1400, 850), opt["rainfall"])
    check("options: NPK 50-120 default 90", (opt["npk"]["min"], opt["npk"]["max"], opt["npk"]["default"]) == (50, 120, 90), opt["npk"])
    check("options: sparse / medium / dense looks", [l["key"] for l in opt["looks"]] == ["sparse", "medium", "dense"], opt["looks"])
    pre = opt["prefill"]
    check("prefill: Paddy from the profile (Chilli has no norm)", pre["crop"] == "Paddy" and pre["cropFromProfile"], pre)
    check("prefill: 2.5 acres and borewell from the profile", pre["areaAcres"] == 2.5 and pre["method"] == "canal-borewell" and pre["methodFromProfile"], pre)
    check("prefill: NPK 90 %, latest fertilizer advice named", pre["npkPercent"] == 90 and pre.get("lastFertilizerCrop"), pre)
    st, opt2, _ = call(base, "GET", "api/Farmer/yield/options", token=other)
    check("prefill: no irrigation in the profile -> canal / borewell, not from the profile",
          st == 200 and opt2["prefill"]["method"] == "canal-borewell" and not opt2["prefill"]["methodFromProfile"], opt2.get("prefill"))

    # ---- the sketch
    st, r, _ = call(base, "POST", "api/Farmer/yield/predict", SKETCH, token=tok)
    check("predict: 200 for the sketch values", st == 200, r)
    check("predict: about 6.9 t (Paddy 3 acres)", 6.8 < r["expectedT"] < 7.0, r.get("expectedT"))
    check("predict: per hectare inside 4.5-7.0 t/ha", 4.5 <= r["perHectareT"] <= 7.0, r.get("perHectareT"))
    check("predict: low <= expected <= high", r["lowT"] <= r["expectedT"] <= r["highT"], (r["lowT"], r["expectedT"], r["highT"]))
    check("predict: +/- 12 %, high confidence", r["rangePercent"] == 12 and r["confidenceLabel"] == "High confidence", (r["rangePercent"], r["confidenceLabel"]))
    check("predict: four factors, NPK is the biggest loss", [f["code"] for f in r["factors"]] == ["ndvi", "rain", "npk", "irrigation"] and r["biggestLoss"] == "npk", r["factors"])
    check("predict: the first tip is the full NPK dose", r["tips"] and r["tips"][0]["code"] == "npk", r["tips"])
    check("predict: saved with an id", r["id"] > 0, r.get("id"))
    check("predict: sources named", "TNAU" in r["source"] and "FAO" in r["source"], r.get("source"))
    check("predict: an NDVI value is reported as entered", "Crop vigour was entered as an NDVI value." in r["confidenceReasons"], r.get("confidenceReasons"))
    check("predict: two or three tips", 2 <= len(r["tips"]) <= 3, [t["code"] for t in r["tips"]])
    first_id = r["id"]

    # Fix round 1: the untouched default NDVI (what the screen sends) and an omitted source are never "measured".
    for name, body in [("ndviSource default", req(ndviSource="default")), ("ndviSource omitted", {k: v for k, v in SKETCH.items() if k != "ndviSource"})]:
        st, d, _ = call(base, "POST", "api/Farmer/yield/predict", body, token=tok)
        check(f"predict ({name}): 20 %, medium confidence", st == 200 and d["rangePercent"] == 20 and d["confidenceLabel"] == "Medium confidence"
              and d["ndviSource"] == "default", (st, d.get("rangePercent"), d.get("confidenceLabel"), d.get("ndviSource")))
        check(f"predict ({name}): says the vigour was left at the default, not entered",
              any("left at the default" in x for x in d["confidenceReasons"]) and not any("entered as an NDVI value" in x for x in d["confidenceReasons"]), d.get("confidenceReasons"))
        check(f"predict ({name}): NPK tip then the canopy check", [t["code"] for t in d["tips"]][:2] == ["npk", "canopy"], [t["code"] for t in d["tips"]])
    st, rows, _ = call(base, "POST", "api/Farmer/yield/predict", req(ndviSource="default"), token=tok)
    check("predict: paddy's source says its Ky is assumed", "Ky assumed" in rows["source"] and "and Ky (FAO I&D paper 33)" not in rows["source"], rows.get("source"))
    st, bg, _ = call(base, "POST", "api/Farmer/yield/predict", req(crop="Black gram"), token=tok)
    check("predict: black gram's source names the bean proxy", st == 200 and "bean as a pulses proxy" in bg["source"], bg.get("source"))

    st, look, _ = call(base, "POST", "api/Farmer/yield/predict", req(ndvi=0.45, ndviSource="look", method="rainfed", rainfallMm=550, npkPercent=60), token=tok)
    check("predict: low inputs by look -> 25 %, low confidence", st == 200 and look["rangePercent"] == 25 and look["confidenceLabel"] == "Low confidence", look)
    check("predict: low inputs give much less than the sketch", look["expectedT"] < r["expectedT"] / 2, look.get("expectedT"))
    st, over, _ = call(base, "POST", "api/Farmer/yield/predict", req(npkPercent=120), token=tok)
    check("predict: 120 % NPK warns about over-application", st == 200 and any("adds cost" in w for w in over["warnings"]), over.get("warnings"))
    st, cane_r, _ = call(base, "POST", "api/Farmer/yield/predict", req(crop="sugarcane", method="DRIP", areaAcres=2), token=tok)
    check("predict: names and methods without case (sugarcane, drip)", st == 200 and cane_r["crop"] == "Sugarcane" and cane_r["method"] == "drip", cane_r)

    # ---- validation
    for name, body, key in [
        ("area 0", req(areaAcres=0), "areaAcres"),
        ("NDVI 0.2", req(ndvi=0.2), "ndvi"),
        ("rainfall 1500", req(rainfallMm=1500), "rainfallMm"),
        ("NPK 130", req(npkPercent=130), "npkPercent"),
        ("unknown crop", req(crop="Poppy"), "crop"),
        ("unknown method", req(method="flood"), "method"),
        ("rainfed sugarcane", req(crop="Sugarcane", method="rainfed"), "method"),
        ("bad NDVI source", req(ndviSource="drone"), "ndviSource"),
    ]:
        st, body_r, _ = call(base, "POST", "api/Farmer/yield/predict", body, token=tok)
        check(f"validate: {name} -> 400 with {key}", st == 400 and key in (body_r.get("errors") or {}), (st, body_r))

    # ---- history, scoped to the caller
    st, hist, _ = call(base, "GET", "api/Farmer/yield/history", token=tok)
    check("history: 200, newest first, at most 20", st == 200 and 0 < len(hist) <= 20 and hist[0]["id"] >= hist[-1]["id"], len(hist) if isinstance(hist, list) else hist)
    check("history: holds the sketch forecast", any(h["id"] == first_id for h in hist), [h["id"] for h in hist][:5])
    st, one, _ = call(base, "GET", f"api/Farmer/yield/history/{first_id}", token=tok)
    check("history: one forecast in full", st == 200 and one["id"] == first_id and one["expectedT"] == r["expectedT"] and len(one["factors"]) == 4, one)
    st, _, _ = call(base, "GET", f"api/Farmer/yield/history/{first_id}", token=other)
    check("history: another farmer's forecast is 404", st == 404, st)
    st, hist2, _ = call(base, "GET", "api/Farmer/yield/history", token=other)
    check("history: the other farmer does not see it", st == 200 and all(h["id"] != first_id for h in hist2), hist2)

    print()
    print(f"{len(failures)} failure(s)" if failures else "All checks passed.")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
