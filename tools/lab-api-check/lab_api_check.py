#!/usr/bin/env python3
"""
End-to-end check of the SAS Lab portal API (api/Lab/..., LabController) against a running API.

Builds fresh version 1 data through the v1 API (farmers, free and paid collections, payment,
admin approval, consignments) as the QA accounts, then walks the whole lab flow:
receive -> create batches -> assign -> preview / draft / submit values -> documents -> complete,
asserting every KPI delta, status, analysis day, activity kind, document upload / delete and
export on the way. All rows it creates are prefixed QA-LAB and are left in place.

LOCAL ONLY: point it at a local API that uses the local database (never the live API).

    python tools/lab-api-check/lab_api_check.py                       # http://localhost:5121
    python tools/lab-api-check/lab_api_check.py --base http://localhost:5121 --password "QaPass@2026#"
    python tools/lab-api-check/lab_api_check.py --no-db                # skip the delayed-batch step

The delayed-analysis step back-dates one batch's TakenForAnalysisAt by 7 days directly in the
local database (psycopg2, --db DSN); use --no-db to skip it.
"""
import argparse
import datetime as dt
import io
import json
import random
import re
import sys
import zipfile

import requests

PARSER = argparse.ArgumentParser(description="SAS Lab API end-to-end check (local only)")
PARSER.add_argument("--base", default="http://localhost:5121")
PARSER.add_argument("--password", default="QaPass@2026#")
PARSER.add_argument("--db", default="host=localhost port=5432 dbname=spicone_dev user=postgres password=1234")
PARSER.add_argument("--no-db", action="store_true")
ARGS = PARSER.parse_args()

BASE = ARGS.base.rstrip("/") + "/"
if not re.match(r"^https?://(localhost|127\.0\.0\.1|\[::1\])(:\d+)?/$", BASE):
    sys.exit(f"Refusing to run against {BASE}: this check writes test data and is for a LOCAL API only.")

# enum values (serialized as integers)
FREE, PAID = 0, 1
SOIL, WATER, SOIL_AND_WATER = 0, 1, 2
CONS_DISPATCHED, CONS_DELIVERED, CONS_COMPLETED = 1, 3, 4
COL_DELIVERED, COL_TEST, COL_COMPLETED, COL_READY = 7, 8, 9, 4
LAB_INTRANSIT, LAB_PENDING, LAB_CREATED, LAB_COMPLETED = 0, 1, 2, 3
B_CREATED, B_TAKEN, B_INPROGRESS, B_ANALYSED, B_COMPLETED = 0, 1, 2, 3, 4
S_NOTSTARTED, S_INPROGRESS, S_COMPLETED = 0, 1, 2
R_NORMAL, R_DEFICIENT, R_MODERATE, R_EXCESS = 0, 1, 2, 3
O_GOOD, O_NEEDS, O_POOR = 0, 1, 2
PRIORITY_MEDIUM, PRIORITY_HIGH = 1, 2
K = {n: i for i, n in enumerate(["BatchCreated", "SampleReceived", "SampleLogged", "ParameterAssigned", "AnalysisStarted",
                                 "AnalysisInProgress", "DocumentUploaded", "StatusUpdated", "ResultEntered", "ReportGenerated",
                                 "DocumentDeleted", "Assigned"])}

PASSED, FAILED = [], []


def ok(cond, msg):
    (PASSED if cond else FAILED).append(msg)
    if not cond:
        print(f"  FAIL  {msg}")
    return cond


def need(cond, msg):
    if not ok(cond, msg):
        summary()
        sys.exit(f"stopping: {msg}")


def eq(actual, expected, msg):
    return ok(actual == expected, f"{msg} (expected {expected!r}, got {actual!r})")


def section(title):
    print(f"\n== {title}")


class Client:
    def __init__(self, user):
        self.user = user
        r = requests.post(BASE + "api/Authentication/login", json={"userName": user, "password": ARGS.password}, timeout=30)
        need(r.status_code == 200, f"login {user} -> {r.status_code} {r.text[:200]}")
        token = r.json().get("token") or r.json().get("Token")
        self.token = token[7:] if token.lower().startswith("bearer ") else token
        self.s = requests.Session()
        self.s.headers["Authorization"] = f"Bearer {self.token}"
        self.id = self.get("api/Lab/me").json()["userId"]

    def call(self, method, path, expect=None, **kw):
        r = self.s.request(method, BASE + path.lstrip("/"), timeout=60, **kw)
        if expect is not None:
            ok(r.status_code == expect, f"{self.user} {method} {path} -> {r.status_code} (expected {expect}) {r.text[:300] if r.status_code != expect else ''}")
        return r

    def get(self, path, expect=None, **kw):
        return self.call("GET", path, expect, **kw)

    def j(self, method, path, expect=200, **kw):
        r = self.call(method, path, expect, **kw)
        need(r.status_code == expect, f"{self.user} {method} {path} must return {expect}")
        return r.json() if r.content else None


def summary():
    print(f"\n{len(PASSED)} checks passed, {len(FAILED)} failed")
    for f in FAILED:
        print(f"  - {f}")


def xlsx_rows(content):
    with zipfile.ZipFile(io.BytesIO(content)) as z:
        xml = z.read("xl/worksheets/sheet1.xml").decode("utf-8")
    return len(re.findall(r"<(?:x:)?row\b", xml))


def activity_kinds(client, batch_id):
    page = client.j("GET", f"api/Lab/batches/{batch_id}/activities?pageSize=50")
    return [a["kind"] for a in page["items"]], page["items"]


RUN = dt.datetime.now().strftime("%m%d%H%M%S")
print(f"SAS Lab API check against {BASE} (run {RUN})")

# ------------------------------------------------------------------------------------------ sign in
section("sign in")
mdo, admin, coord = Client("qa.mdo"), Client("qa.admin"), Client("qa.labcoord")
analyst, analyst2 = Client("qa.analyst"), Client("qa.analyst2")
farmer, dealer, finance = Client("qa.farmer"), Client("qa.dealer"), Client("qa.finance")

# ------------------------------------------------------------------------------------------ access
section("access: me, 403s, languages")
me = coord.j("GET", "api/Lab/me")
ok(me["isCoordinator"] and me["canWrite"] and not me["isAnalyst"], f"coordinator me {me}")
me = analyst.j("GET", "api/Lab/me")
ok(me["isAnalyst"] and not me["isCoordinator"] and not me["canWrite"], f"analyst me {me}")
me = admin.j("GET", "api/Lab/me")
ok(me["isCoordinator"] and me["canWrite"], f"admin me {me}")
eq(me["name"], "QA Admin", "me Name")
for c in (farmer, dealer, finance, mdo):
    m = c.j("GET", "api/Lab/me")
    ok(not (m["isCoordinator"] or m["isAnalyst"] or m["canWrite"]), f"{c.user} has no lab rights")
    for path in ("api/Lab/dashboard", "api/Lab/batches/stats", "api/Lab/batches", "api/Lab/consignments", "api/Lab/analysts"):
        c.get(path, expect=403)
analyst.call("POST", "api/Lab/batches", expect=403, json={"consignmentIds": [1]})

langs = coord.j("GET", "api/Lab/languages")
eq([l["code"] for l in langs], ["en", "ta", "te", "mr"], "lab languages")
eq([l["nativeName"] for l in langs][:2], ["English", "தமிழ்"], "native names")
eq([l["code"] for l in farmer.j("GET", "api/Lab/languages")], ["en", "ta"], "farmer languages")

analysts = coord.j("GET", "api/Lab/analysts")
names = {a["userId"]: a for a in analysts}
ok(analyst.id in names and analyst2.id in names and admin.id in names, "analysts list has qa.analyst, qa.analyst2 and admins")
ok(coord.id not in names and dealer.id not in names, "analysts list excludes coordinator and dealer")

# ------------------------------------------------------------------------------------------ baselines
section("baselines")
dash0 = coord.j("GET", "api/Lab/dashboard")
cstats0 = coord.j("GET", "api/Lab/consignments/stats")
bstats0 = coord.j("GET", "api/Lab/batches/stats")
adash0 = analyst.j("GET", "api/Lab/analyst/dashboard")
a2dash0 = analyst2.j("GET", "api/Lab/analyst/dashboard")

# ------------------------------------------------------------------------------------------ v1 data
section("v1 data as qa.mdo (farmers, collections, payment, consignments)")
lookups = mdo.j("GET", "api/Sas/lookups")
state = next((s for s in lookups["states"] if s["districts"]), None)


def new_farmer(letter, village):
    body = {"name": f"QA-LAB Farmer {letter} {RUN}", "mobile": "9" + "".join(random.choice("0123456789") for _ in range(9)),
            "village": village, "address1": "QA-LAB street",
            "stateId": state["id"] if state else None,
            "districtId": state["districts"][0]["id"] if state else None}
    return mdo.j("POST", "api/Sas/farmers", json=body)


fa, fb, fc, fd = new_farmer("A", "Kovilpatti"), new_farmer("B", "Ettayapuram"), new_farmer("C", "Vilathikulam"), new_farmer("D", "Ottapidaram")


def collection(payment_type, items, remark):
    body = {"collectionDate": dt.datetime.now().isoformat(), "paymentType": payment_type,
            "paidCategory": 0 if payment_type == PAID else None, "remarks": f"QA-LAB {remark} {RUN}",
            "items": items, "saveAsDraft": False}
    return mdo.j("POST", "api/Sas/collections", json=body)


c1 = collection(FREE, [{"farmerId": fa["id"], "sampleType": SOIL, "crop1": "Paddy"},
                       {"farmerId": fb["id"], "sampleType": WATER, "crop1": "Paddy"}], "free soil + water")
c2 = collection(PAID, [{"farmerId": fc["id"], "sampleType": SOIL, "crop1": "Banana"},
                       {"farmerId": fd["id"], "sampleType": SOIL_AND_WATER, "crop1": "Sugarcane"}], "paid soil + soil&water")
c3 = collection(FREE, [{"farmerId": fa["id"], "sampleType": SOIL, "crop1": "Tomato"}], "free soil second batch")
eq(c1["status"], COL_READY, "free collection ready for consignment")
pay = mdo.j("POST", f"api/Sas/collections/{c2['id']}/payment",
            json={"transactionId": f"QA-LAB-TXN-{RUN}", "paidByName": "QA-LAB Payer", "bankGateway": "UPI"})
admin.j("PATCH", f"api/Sas/payments/{pay['id']}/status?status=Approved")
eq(mdo.j("GET", f"api/Sas/collections/{c2['id']}")["status"], COL_READY, "paid collection approved -> ready")


def consignment(col, n):
    return mdo.j("POST", "api/Sas/consignments", json={"collectionIds": [col["id"]], "courierService": "DTDC",
                                                         "trackingNumber": f"QA-LAB-AWB-{RUN}-{n}",
                                                         "notes": f"QA-LAB consignment {n} {RUN}"})


k1, k2, k3 = consignment(c1, 1), consignment(c2, 2), consignment(c3, 3)
print(f"  consignments {k1['code']}, {k2['code']}, {k3['code']}")

# ------------------------------------------------------------------------------------------ consignments
section("consignments: list, stats, receive")
page = coord.j("GET", f"api/Lab/consignments?q={k1['code']}")
eq(page["total"], 1, "consignment search by code")
row = page["items"][0]
eq(row["labStatus"], LAB_INTRANSIT, "dispatched consignment lab status InTransit")
eq((row["soilSamples"], row["waterSamples"], row["totalSamples"], row["sampleType"]), (1, 1, 2, FREE), "K1 counts and Free")
r2 = coord.j("GET", f"api/Lab/consignments?q={k2['code']}")["items"][0]
eq((r2["soilSamples"], r2["waterSamples"], r2["totalSamples"], r2["sampleType"]), (2, 1, 2, PAID), "K2 counts (SoilAndWater counts in both) and Paid")
cstats = coord.j("GET", "api/Lab/consignments/stats")
eq(cstats["totalConsignments"] - cstats0["totalConsignments"], 3, "stats TotalConsignments +3")
eq(cstats["soilSamples"] - cstats0["soilSamples"], 4, "stats SoilSamples +4")
eq(cstats["waterSamples"] - cstats0["waterSamples"], 2, "stats WaterSamples +2")
coord.call("POST", "api/Lab/batches", expect=400, json={"consignmentIds": [k1["id"]]})
analyst.call("POST", f"api/Lab/consignments/{k1['id']}/receive", expect=403)
for k in (k1, k2, k3):
    rec = coord.j("POST", f"api/Lab/consignments/{k['id']}/receive")
    eq((rec["status"], rec["labStatus"]), (CONS_DELIVERED, LAB_PENDING), f"{k['code']} received -> Delivered / BatchPending")
    ok(rec["deliveredAt"] is not None, "DeliveredAt set")
coord.call("POST", f"api/Lab/consignments/{k1['id']}/receive", expect=400)
v1 = mdo.j("GET", f"api/Sas/collections/{c1['id']}")
eq(v1["status"], COL_DELIVERED, "v1 collection DeliveredToLab")
ok(any(e["status"] == "DeliveredToLab" for e in v1["timeline"]), "v1 timeline DeliveredToLab event")
v1k = mdo.j("GET", f"api/Sas/consignments/{k1['id']}")
ok(v1k["status"] == CONS_DELIVERED and any(e["status"] == "Delivered" for e in v1k["timeline"]), "v1 consignment Delivered event")
dash = coord.j("GET", "api/Lab/dashboard")
eq(dash["totalConsignmentsReceived"] - dash0["totalConsignmentsReceived"], 3, "dashboard TotalConsignmentsReceived +3")
eq(dash["pendingBatchCreation"] - dash0["pendingBatchCreation"], 3, "dashboard PendingBatchCreation +3")
ok(any(c["id"] == k3["id"] for c in dash["recentConsignments"]), "recent consignments show the received ones")
pending = coord.j("GET", "api/Lab/consignments?status=BatchPending&pageSize=50")
ok({k1["id"], k2["id"], k3["id"]} <= {c["id"] for c in pending["items"]}, "BatchPending filter")

# ------------------------------------------------------------------------------------------ batches
section("batches: create, assign, statuses")
coord.call("POST", "api/Lab/batches", expect=400, json={"consignmentIds": [k1["id"]], "assignedToUserId": dealer.id})
b1 = coord.j("POST", "api/Lab/batches", json={"consignmentIds": [k1["id"], k2["id"]], "assignedToUserId": analyst.id,
                                               "priority": PRIORITY_HIGH, "remarks": f"QA-LAB batch one {RUN}"})
h1 = b1["header"]
B1 = h1["id"]
print(f"  batch {h1['code']}")
ok(re.match(r"^BAT-SAS-\d{4}-\d{3,}$", h1["code"]) is not None, f"batch code format {h1['code']}")
eq((h1["status"], h1["sampleCount"], h1["priority"], h1["sampleType"]), (B_TAKEN, 4, PRIORITY_HIGH, PAID), "B1 TakenForAnalysis, 4 samples, High, Paid")
eq(sorted(h1["consignmentCodes"]), sorted([k1["code"], k2["code"]]), "B1 consignment codes")
eq((h1["analysisDays"], h1["isDelayed"]), (0, False), "B1 analysis days 0, not delayed")
eq((h1["assignedToUserId"], h1["assignedByName"]), (analyst.id, "QA Lab Coordinator"), "B1 assigned to qa.analyst by the coordinator")
ok(b1["takenForAnalysisAt"] is not None, "TakenForAnalysisAt set")
eq(b1["parameterRowCount"], 70, "B1 parameter rows (soil 14 + water 14 + soil 14 + soil&water 28)")
eq(len(b1["progress"]), 4, "4 progress steps")
eq([s["isDone"] for s in b1["progress"]], [True, False, False, False], "progress: taken done")
eq([s["isCurrent"] for s in b1["progress"]], [True, False, False, False], "progress: taken current")
kinds, acts = activity_kinds(coord, B1)
for kind in ("BatchCreated", "SampleReceived", "SampleLogged", "ParameterAssigned", "Assigned", "StatusUpdated"):
    ok(K[kind] in kinds, f"B1 activity {kind}")
eq(kinds.count(K["SampleReceived"]), 2, "one SampleReceived per consignment")
ok(any(a["description"] == "All 4 samples logged into system" for a in acts), "SampleLogged text")
ok(any(a["description"] == "70 parameters assigned to 4 samples" for a in acts), "ParameterAssigned text")
ok(all(a["byName"] == "QA Lab Coordinator" and a["byRole"] == "QA Lab Coordinator" for a in acts), "activities stamped with the coordinator")
eq([t["kind"] for t in b1["timeline"]], [K["BatchCreated"], K["StatusUpdated"]], "timeline: created, taken for analysis")
eq(mdo.j("GET", f"api/Sas/collections/{c1['id']}")["status"], COL_TEST, "v1 collection TestInProgress once taken")
eq(coord.j("GET", f"api/Lab/consignments/{k1['id']}")["summary"]["labStatus"], LAB_CREATED, "K1 BatchCreated")
coord.call("POST", "api/Lab/batches", expect=400, json={"consignmentIds": [k1["id"]]})

b2 = coord.j("POST", "api/Lab/batches", json={"consignmentIds": [k3["id"]], "remarks": f"QA-LAB batch two {RUN}"})
B2 = b2["header"]["id"]
print(f"  batch {b2['header']['code']}")
eq((b2["header"]["status"], b2["header"]["analysisDays"], b2["header"]["priority"]), (B_CREATED, None, PRIORITY_MEDIUM), "B2 Created, days Pending, Medium")
eq(mdo.j("GET", f"api/Sas/collections/{c3['id']}")["status"], COL_DELIVERED, "unassigned batch leaves v1 at DeliveredToLab")
coord.call("PATCH", f"api/Lab/batches/{B2}/status?status=TakenForAnalysis", expect=400)
coord.call("PATCH", f"api/Lab/batches/{B2}/status?status=InProgress", expect=400)
coord.call("PATCH", f"api/Lab/batches/{B2}/status?status=AnalysisCompleted", expect=400)
analyst.call("PATCH", f"api/Lab/batches/{B2}/assign", expect=403, json={"assignedToUserId": analyst.id})
b2 = coord.j("PATCH", f"api/Lab/batches/{B2}/assign", json={"assignedToUserId": analyst2.id})
eq((b2["header"]["status"], b2["header"]["assignedToUserId"]), (B_TAKEN, analyst2.id), "assign moves B2 to TakenForAnalysis")
eq(mdo.j("GET", f"api/Sas/collections/{c3['id']}")["status"], COL_TEST, "v1 TestInProgress after assign")

bstats = coord.j("GET", "api/Lab/batches/stats")
eq(bstats["totalBatches"] - bstats0["totalBatches"], 2, "batch stats Total +2")
eq(bstats["takenForAnalysis"] - bstats0["takenForAnalysis"], 2, "batch stats TakenForAnalysis +2")
dash = coord.j("GET", "api/Lab/dashboard")
eq(dash["totalBatchesCreated"] - dash0["totalBatchesCreated"], 2, "dashboard TotalBatchesCreated +2")
eq(dash["pendingBatchCreation"] - dash0["pendingBatchCreation"], 0, "dashboard PendingBatchCreation back to baseline")
ok(dash["recentBatches"][0]["id"] == B2, "recent batches newest first")

# list filters
eq(coord.j("GET", f"api/Lab/batches?q={h1['code']}")["total"], 1, "batch search by code")
eq([b["id"] for b in coord.j("GET", f"api/Lab/batches?consignmentId={k3['id']}")["items"]], [B2], "consignmentId filter")
eq([b["id"] for b in coord.j("GET", f"api/Lab/batches?batchId={B1}")["items"]], [B1], "batchId filter")
ok(B1 in [b["id"] for b in coord.j("GET", "api/Lab/batches?priority=High&pageSize=50")["items"]], "priority filter")
ids = [b["id"] for b in coord.j("GET", "api/Lab/batches?sampleType=Paid&pageSize=50")["items"]]
ok(B1 in ids and B2 not in ids, "sampleType=Paid filter")
fy = dt.date.today().year if dt.date.today().month >= 4 else dt.date.today().year - 1
ok(B1 in [b["id"] for b in coord.j("GET", f"api/Lab/batches?financialYear={fy}&pageSize=50")["items"]], "financialYear filter")
ok(fy in coord.j("GET", "api/Lab/financial-years"), "financial years include the current FY")
ok(B2 in [b["id"] for b in coord.j("GET", "api/Lab/batches?stage=TakenForAnalysis&pageSize=50")["items"]], "stage filter")
p = coord.j("GET", "api/Lab/batches?pageSize=500")
eq(p["pageSize"], 50, "page size capped at 50")
eq(coord.j("GET", "api/Lab/batches")["pageSize"], 16, "default page size 16")

# ------------------------------------------------------------------------------------------ analyst scope
section("analyst scope")
mine = analyst.j("GET", "api/Lab/batches?pageSize=50")
ok(all(b["assignedToUserId"] == analyst.id for b in mine["items"]), "analyst sees only own batches")
ok(B1 in [b["id"] for b in mine["items"]] and B2 not in [b["id"] for b in mine["items"]], "analyst list has B1, not B2")
analyst.get(f"api/Lab/batches/{B2}", expect=404)
adash = analyst.j("GET", "api/Lab/analyst/dashboard")
eq(adash["assignedBatches"] - adash0["assignedBatches"], 1, "analyst AssignedBatches +1")
eq(adash["pendingValueEntry"] - adash0["pendingValueEntry"], 4, "analyst PendingValueEntry +4")
eq(adash["pendingEntryBatches"] - adash0["pendingEntryBatches"], 1, "analyst PendingEntryBatches +1")
astats = analyst.j("GET", "api/Lab/batches/stats")
eq(astats["totalBatches"], adash["assignedBatches"], "analyst batch stats scoped to own batches")

# ------------------------------------------------------------------------------------------ samples
section("samples list and ids")
samples = coord.j("GET", f"api/Lab/batches/{B1}/samples")
eq(samples["total"], 4, "B1 4 samples")
sid = {s["sampleId"]: s for s in samples["items"]}
eq(sorted(sid), ["SAS-SOIL-001", "SAS-SOIL-002", "SAS-SW-001", "SAS-WATER-001"], "display ids by type in code order")
eq(sid["SAS-SOIL-001"]["farmerName"], fa["name"], "SOIL-001 is the first collection's soil sample")
ok(all(s["status"] == S_NOTSTARTED and s["progressPercent"] == 0 for s in samples["items"]), "all NotStarted, 0%")
eq(sid["SAS-SW-001"]["parameterCount"], 28, "SoilAndWater sample gets both parameter sets")
eq(coord.j("GET", f"api/Lab/batches/{B1}/samples/stats"), {"total": 4, "notStarted": 4, "inProgress": 0, "completed": 0}, "sample stats")
eq(coord.j("GET", f"api/Lab/batches/{B1}/samples?sampleType=Water")["total"], 1, "samples sampleType filter")
eq(coord.j("GET", f"api/Lab/batches/{B1}/samples?q=SAS-SOIL")["total"], 2, "samples q filter")


def entry(client, sample, expect=200):
    return client.j("GET", f"api/Lab/samples/{sample['sampleItemId']}", expect=expect)


def values_for(e, by_code):
    ids = {p["code"]: p["labParameterId"] for p in e["result"]["parameters"]}
    return [{"labParameterId": ids[c], "value": v} for c, v in by_code.items()]


SOIL_NI = {"S-TEX": "sandy clay silt", "S-PH": "6.8", "S-EC": "0.4", "S-OC": "0.42", "S-N": "210", "S-P": "30", "S-K": "150",
           "S-ZN": "0.8", "S-FE": "5", "S-MN": "3", "S-CU": "0.3", "S-B": "0.7", "S-S": "15"}
SOIL_POOR = dict(SOIL_NI, **{"S-TEX": "Clay", "S-PH": "8.2", "S-OC": "0.9", "S-N": "600", "S-ZN": "0.3"})
SOIL_OK = dict(SOIL_NI, **{"S-TEX": "Loam", "S-PH": "7.0", "S-OC": "0.6", "S-N": "300"})
WATER_OK = {"W-PH": "7.2", "W-EC": "0.5", "W-TDS": "300", "W-CL": "2", "W-SO4": "2", "W-CO3": "0.2", "W-HCO3": "1.5",
            "W-NA": "2", "W-CA": "3", "W-MG": "1.5", "W-SAR": "4", "W-RSC": "0.5", "W-NO3": "5", "W-MB": "0"}
WATER_NA = dict(WATER_OK, **{"W-NA": "5"})

# ------------------------------------------------------------------------------------------ value entry
section("value entry: preview, draft, submit")
s_soil1, s_water1, s_soil2, s_sw = sid["SAS-SOIL-001"], sid["SAS-WATER-001"], sid["SAS-SOIL-002"], sid["SAS-SW-001"]
e = entry(analyst, s_soil1)
eq((e["batch"]["id"], e["pendingInBatch"], e["completedInBatch"], e["isLocked"]), (B1, 4, 0, False), "entry header")
eq(e["farmerMobile"], fa["mobile"], "entry farmer mobile")
eq(len(e["result"]["parameters"]), 14, "soil sample shows 14 parameters")
analyst2.call("PUT", f"api/Lab/samples/{s_soil1['sampleItemId']}/values", expect=404,
              json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(e, {"S-PH": "7"}), "submit": False})
analyst2.get(f"api/Lab/samples/{s_soil1['sampleItemId']}", expect=404)

bad = analyst.call("POST", "api/Lab/samples/preview", expect=400,
                   json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(e, {"S-PH": "abc"})})
prev = analyst.j("POST", "api/Lab/samples/preview", json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(e, SOIL_NI)})
rows = {p["code"]: p for p in prev["parameters"]}
eq((rows["S-PH"]["resultLabel"], rows["S-PH"]["status"]), ("Neutral", R_NORMAL), "pH 6.8 -> Neutral / Normal")
eq((rows["S-EC"]["resultLabel"], rows["S-EC"]["status"]), ("Safe", R_NORMAL), "EC 0.4 -> Safe / Normal")
eq((rows["S-N"]["resultLabel"], rows["S-N"]["status"], rows["S-N"]["hint"]), ("Low", R_DEFICIENT, "Apply nitrogen fertilizer"), "N 210 -> Low / Deficient / LowHint")
eq((rows["S-OC"]["resultLabel"], rows["S-OC"]["status"]), ("Low", R_DEFICIENT), "OC 0.42 -> Low / Deficient")
eq((rows["S-OM"]["enteredValue"], rows["S-OM"]["status"]), ("0.72", R_DEFICIENT), "Organic Matter derived 0.42 x 1.724 = 0.72")
eq((rows["S-TEX"]["enteredValue"], rows["S-TEX"]["status"], rows["S-TEX"]["resultLabel"]), ("Sandy Clay Silt", None, None), "Texture stored as text, no status")
eq((prev["overallStatus"], prev["overallStatusText"]), (O_NEEDS, "Needs Improvement"), "two problems -> Needs Improvement")
groups = {g["group"]: g["lines"] for g in prev["recommendations"]}
eq(groups.get("Fertilizer"), ["Apply nitrogen fertilizer because nitrogen is below normal range."], "Fertilizer recommendation")
eq(groups.get("Organic"), ["Add organic manure / compost because organic carbon is below normal range."], "Organic recommendation")
eq(prev["cropSuitabilityNote"], "Soil is suitable for Paddy after correcting low organic carbon and low nitrogen.", "crop suitability note")
e = entry(analyst, s_soil1)
ok(all(p["enteredValue"] is None for p in e["result"]["parameters"]) and e["sample"]["status"] == S_NOTSTARTED, "preview saves nothing")

analyst.call("PUT", f"api/Lab/samples/{s_soil1['sampleItemId']}/values", expect=400,
             json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(e, {"S-PH": "6.8"}), "submit": True})
d = analyst.j("PUT", f"api/Lab/samples/{s_soil1['sampleItemId']}/values",
              json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(e, {"S-PH": "6.8", "S-EC": "0.4"}), "submit": False})
eq((d["sample"]["status"], d["sample"]["enteredCount"], d["sample"]["progressPercent"], d["sample"]["currentParameter"]),
   (S_INPROGRESS, 2, 14, "EC"), "draft -> InProgress, 2 of 14 (14%), current EC")
eq(d["batch"]["status"], B_INPROGRESS, "batch InProgress on the first value")
rowstat = {p["code"]: p["rowStatus"] for p in d["result"]["parameters"]}
eq((rowstat["S-PH"], rowstat["S-N"]), (S_INPROGRESS, S_NOTSTARTED), "parameter row statuses after draft")
b = coord.j("GET", f"api/Lab/batches/{B1}")
ok(b["analysisStartedAt"] is not None, "batch AnalysisStartedAt set")
eq([s["isDone"] for s in b["progress"]], [True, True, False, False], "progress: in progress done")
kinds, _ = activity_kinds(coord, B1)
ok(K["AnalysisStarted"] in kinds and K["AnalysisInProgress"] in kinds, "AnalysisStarted and AnalysisInProgress logged")
v1 = mdo.j("GET", f"api/Sas/collections/{c1['id']}")
item = next(i for i in v1["items"] if i["id"] == s_soil1["sampleItemId"])
eq(len(item["results"]), 2, "draft visible as v1 SampleLabResult rows")

sub = analyst.j("PUT", f"api/Lab/samples/{s_soil1['sampleItemId']}/values",
                json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(e, SOIL_NI), "submit": True})
eq((sub["sample"]["status"], sub["sample"]["progressPercent"], sub["sample"]["currentParameter"]), (S_COMPLETED, 100, "Sulphur"), "submit -> Completed 100%")
eq(sub["result"]["overallStatus"], O_NEEDS, "stored result overall")
eq(sub["batch"]["status"], B_INPROGRESS, "batch still InProgress with samples pending")

e = entry(analyst, s_water1)
w = analyst.j("PUT", f"api/Lab/samples/{s_water1['sampleItemId']}/values",
              json={"sampleItemId": s_water1["sampleItemId"], "values": values_for(e, WATER_OK), "submit": True})
eq((w["result"]["overallStatus"], w["result"]["cropSuitabilityNote"], w["result"]["recommendations"]), (O_GOOD, "Suitable for irrigation", []), "water all normal -> Good, suitable for irrigation")

e = entry(analyst, s_soil2)
pr = analyst.j("PUT", f"api/Lab/samples/{s_soil2['sampleItemId']}/values",
               json={"sampleItemId": s_soil2["sampleItemId"], "values": values_for(e, SOIL_POOR), "submit": True})
eq((pr["result"]["overallStatus"], pr["result"]["overallStatusText"]), (O_POOR, "Poor"), "four problems -> Poor")
eq(pr["result"]["cropSuitabilityNote"], "Soil is suitable for Banana after correcting high pH, high organic carbon, high nitrogen, and low zinc.", "Poor note")
prow = {p["code"]: p for p in pr["result"]["parameters"]}
eq((prow["S-PH"]["resultLabel"], prow["S-PH"]["status"]), ("Alkaline", R_EXCESS), "pH 8.2 -> Alkaline / Excess")

e = entry(analyst, s_sw)
eq(len(e["result"]["parameters"]), 28, "SoilAndWater entry has both sets")
sw_values = values_for(e, dict(SOIL_OK, **WATER_NA))
sw = analyst.j("PUT", f"api/Lab/samples/{s_sw['sampleItemId']}/values", json={"sampleItemId": s_sw["sampleItemId"], "values": sw_values, "submit": True})
eq(sw["result"]["overallStatus"], O_NEEDS, "SoilAndWater with high sodium -> Needs Improvement")
eq(sw["result"]["cropSuitabilityNote"], "Soil is suitable for Sugarcane. Water: Use with caution: high sodium.", "SoilAndWater note")
eq(sw["batch"]["status"], B_ANALYSED, "all samples submitted -> batch AnalysisCompleted")

# reopen and resubmit (still editable until the report is generated)
back = analyst.j("PUT", f"api/Lab/samples/{s_sw['sampleItemId']}/values", json={"sampleItemId": s_sw["sampleItemId"], "values": sw_values, "submit": False})
eq((back["sample"]["status"], back["batch"]["status"]), (S_INPROGRESS, B_INPROGRESS), "draft on a submitted sample reopens the batch")
again = analyst.j("PUT", f"api/Lab/samples/{s_sw['sampleItemId']}/values", json={"sampleItemId": s_sw["sampleItemId"], "values": sw_values, "submit": True})
eq(again["batch"]["status"], B_ANALYSED, "resubmit -> AnalysisCompleted again")
b = coord.j("GET", f"api/Lab/batches/{B1}")
ok(b["analysisCompletedAt"] is not None, "AnalysisCompletedAt set")
eq([s["isDone"] for s in b["progress"]], [True, True, True, False], "progress: analysis completed")
eq(b["header"]["analysisDays"], 0, "analysis days fixed at completion (0)")
kinds, acts = activity_kinds(coord, B1)
ok(K["ResultEntered"] in kinds, "ResultEntered logged")
ok(any(a["description"] == "Status updated to Analysis Completed" for a in acts), "StatusUpdated Analysis Completed logged")
ok(any(a["description"] == "Status updated to In Progress" for a in acts), "StatusUpdated In Progress logged on reopen")
ok(all(a["byName"] == "QA Lab Analyst" for a in acts if a["kind"] in (K["ResultEntered"], K["AnalysisInProgress"])), "entry activities stamped with the analyst")

eq(coord.j("GET", f"api/Lab/batches/{B1}/samples/stats"), {"total": 4, "notStarted": 0, "inProgress": 0, "completed": 4}, "sample stats after entry")
eq(coord.j("GET", f"api/Lab/batches/{B1}/parameters/stats"), {"totalParameters": 70, "completed": 70, "inProgress": 0, "notStarted": 0}, "parameter stats after entry")
params = coord.j("GET", f"api/Lab/batches/{B1}/parameters")
eq((params["total"], [len(s["parameters"]) for s in params["items"]]), (4, [14, 14, 14, 28]), "parameters tab: one row per sample")
one = coord.j("GET", f"api/Lab/batches/{B1}/parameters?parameter=S-N")
eq((one["total"], {len(s["parameters"]) for s in one["items"]}), (3, {1}), "parameters filter by code keeps matching rows")
eq(coord.j("GET", f"api/Lab/batches/{B1}/samples?parameter=Sulphur")["total"], 2, "samples filter by current parameter")
eq(coord.j("GET", f"api/Lab/batches/{B1}/samples?status=Completed")["total"], 4, "samples status filter")

adash = analyst.j("GET", "api/Lab/analyst/dashboard")
eq(adash["autoResultReady"] - adash0["autoResultReady"], 4, "analyst AutoResultReady +4")
eq(adash["autoResultReadyBatches"] - adash0["autoResultReadyBatches"], 1, "analyst AutoResultReadyBatches +1")
eq(adash["pendingValueEntry"], adash0["pendingValueEntry"], "analyst PendingValueEntry back to baseline")
eq(coord.j("GET", "api/Lab/batches/stats")["analysisCompleted"] - bstats0["analysisCompleted"], 1, "batch stats AnalysisCompleted +1")

# ------------------------------------------------------------------------------------------ exports
section("exports (?access_token=)")
r = requests.get(BASE + f"api/Lab/batches/{B1}/samples/export?access_token={analyst.token}", timeout=60)
ok(r.status_code == 200 and r.content[:2] == b"PK", f"samples export via access_token -> {r.status_code}")
eq(xlsx_rows(r.content), 5, "samples export rows (header + 4)")
r = coord.get(f"api/Lab/batches/{B1}/parameters/export")
ok(r.status_code == 200 and "spreadsheetml" in r.headers.get("Content-Type", ""), "parameters export content type")
eq(xlsx_rows(r.content), 71, "parameters export rows (header + 70)")
eq(requests.get(BASE + f"api/Lab/batches/{B1}/samples/export", timeout=60).status_code, 401, "export without a token -> 401")
eq(requests.get(BASE + f"api/Lab/batches/{B1}/samples/export?access_token=bogus", timeout=60).status_code, 401, "export with a bad token -> 401")
eq(requests.get(BASE + f"api/Lab/batches/{B1}/samples/export?access_token={farmer.token}", timeout=60).status_code, 403, "export as farmer -> 403")

# ------------------------------------------------------------------------------------------ documents
section("documents")
pdf = b"%PDF-1.4\n% QA-LAB test document\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n"
png = bytes.fromhex("89504e470d0a1a0a0000000d4948445200000001000000010806000000")
analyst.call("POST", f"api/Lab/batches/{B1}/documents?kind=Batch", expect=403, files=[("files", ("qa.pdf", pdf, "application/pdf"))])
coord.call("POST", f"api/Lab/batches/{B1}/documents?kind=Batch", expect=400, files=[("files", ("qa.exe", b"MZ", "application/octet-stream"))])
docs = coord.j("POST", f"api/Lab/batches/{B1}/documents?kind=Batch&description=QA-LAB%20batch%20sheet",
               files=[("files", ("QA-LAB batch sheet.pdf", pdf, "application/pdf")), ("files", ("QA-LAB photo.png", png, "image/png"))])
eq(len(docs), 2, "two documents uploaded")
ok(all(re.match(rf"^api/Sas/file/Sas/lab/{B1}/[0-9a-f]{{32}}\.(pdf|png)$", d["url"]) for d in docs), "document urls under api/Sas/file/Sas/lab/{batch}")
eq((docs[0]["extension"], docs[0]["contentType"], docs[0]["size"]), ("PDF", "application/pdf", len(pdf)), "document metadata")
f = requests.get(BASE + docs[0]["url"] + f"?access_token={coord.token}", timeout=60)
eq((f.status_code, f.content), (200, pdf), "document served by the v1 file route")
ref = coord.j("POST", f"api/Lab/batches/{B1}/documents?kind=Reference", files=[("files", ("QA-LAB reference.pdf", pdf, "application/pdf"))])
eq(coord.j("GET", f"api/Lab/batches/{B1}/documents")["total"], 3, "3 documents listed")
eq(coord.j("GET", f"api/Lab/batches/{B1}/documents?kind=Reference")["total"], 1, "documents kind filter")
eq(analyst.j("GET", f"api/Lab/batches/{B1}/documents?q=photo")["total"], 1, "documents q filter (analyst reads)")
analyst.call("DELETE", f"api/Lab/documents/{ref[0]['id']}", expect=403)
coord.j("DELETE", f"api/Lab/documents/{ref[0]['id']}")
coord.call("DELETE", f"api/Lab/documents/{ref[0]['id']}", expect=404)
eq(coord.j("GET", f"api/Lab/batches/{B1}/documents")["total"], 2, "deleted document hidden")
eq(coord.j("GET", f"api/Lab/batches/{B1}")["documentCount"], 2, "DocumentCount")
kinds, _ = activity_kinds(coord, B1)
eq((kinds.count(K["DocumentUploaded"]), kinds.count(K["DocumentDeleted"])), (3, 1), "DocumentUploaded x3, DocumentDeleted x1")
eq(coord.j("GET", f"api/Lab/batches/{B1}/activities?kind=DocumentUploaded")["total"], 3, "activities kind filter")
eq(coord.j("GET", f"api/Lab/batches/{B1}/activities?by=QA%20Lab%20Analyst&kind=ResultEntered")["total"], 5, "activities by filter (4 submits + 1 resubmit)")

# ------------------------------------------------------------------------------------------ complete
section("complete batch")
analyst.call("PATCH", f"api/Lab/batches/{B1}/status?status=Completed", expect=403)
coord.call("PATCH", f"api/Lab/batches/{B2}/status?status=Completed", expect=400)
done = coord.j("PATCH", f"api/Lab/batches/{B1}/status?status=Completed&remarks=QA-LAB%20final%20remarks")
eq((done["header"]["status"], done["finalRemarks"]), (B_COMPLETED, "QA-LAB final remarks"), "B1 Completed with final remarks")
ok(done["completedAt"] is not None, "CompletedAt set")
eq([s["isDone"] for s in done["progress"]], [True, True, True, True], "progress all done")
eq([s["isCurrent"] for s in done["progress"]], [False, False, False, True], "Completed step current")
eq(done["progress"][3]["byName"], "QA Lab Coordinator", "completed by the coordinator")
ok(any(t["kind"] == K["StatusUpdated"] and t["description"].startswith("Status updated to Completed") for t in done["timeline"]), "timeline has Completed")
report_made = bool(done["header"]["reportCode"] or done["header"]["reportId"])
eq(K["ReportGenerated"] in [t["kind"] for t in done["timeline"]], report_made, "ReportGenerated timeline step iff the report service produced reports")
coord.call("PATCH", f"api/Lab/batches/{B1}/status?status=Completed", expect=409)
coord.call("PATCH", f"api/Lab/batches/{B1}/assign", expect=409, json={"priority": 0})
analyst.call("PUT", f"api/Lab/samples/{s_soil1['sampleItemId']}/values", expect=409,
             json={"sampleItemId": s_soil1["sampleItemId"], "values": values_for(entry(analyst, s_soil1), SOIL_NI), "submit": True})
eq(entry(analyst, s_soil1)["isLocked"], True, "entry locked")
ok(all(s["reportGenerated"] for s in coord.j("GET", f"api/Lab/batches/{B1}/samples")["items"]), "samples ReportGenerated")
for col in (c1, c2):
    v1 = mdo.j("GET", f"api/Sas/collections/{col['id']}")
    eq(v1["status"], COL_COMPLETED, f"v1 collection {col['code']} Completed")
    ok(v1["completedAt"] is not None, "v1 CompletedAt")
    ok(all(len(i["results"]) == (28 if i["sampleType"] == SOIL_AND_WATER else 14) for i in v1["items"]), "v1 details show the lab results")
    ok(any(ev["status"] == "TestInProgress" for ev in v1["timeline"]) and any(ev["status"] == "Completed" for ev in v1["timeline"]), "v1 timeline TestInProgress + Completed")
eq(mdo.j("GET", f"api/Sas/consignments/{k1['id']}")["status"], CONS_COMPLETED, "v1 consignment Completed")
cd = coord.j("GET", f"api/Lab/consignments/{k2['id']}")
eq((cd["summary"]["labStatus"], cd["batch"]["id"], cd["consignment"]["code"]), (LAB_COMPLETED, B1, k2["code"]), "consignment detail: Completed, batch, v1 detail")
dash = coord.j("GET", "api/Lab/dashboard")
eq(dash["completedBatches"] - dash0["completedBatches"], 1, "dashboard CompletedBatches +1")
eq(dash["reportsGenerated"] - dash0["reportsGenerated"], 1 if report_made else 0, "dashboard ReportsGenerated")
eq(dash["takenForAnalysis"] - dash0["takenForAnalysis"], 1, "dashboard TakenForAnalysis +1 (B2)")
eq(dash["analysisCompleted"], dash0["analysisCompleted"], "dashboard AnalysisCompleted back to baseline")
adash = analyst.j("GET", "api/Lab/analyst/dashboard")
eq(adash["reportsToDownload"] - adash0["reportsToDownload"], 1, "analyst ReportsToDownload +1")
eq(adash["autoResultReady"], adash0["autoResultReady"], "analyst AutoResultReady back to baseline")
cstats = coord.j("GET", "api/Lab/consignments/stats")
eq(cstats["batchedConsignments"] - cstats0["batchedConsignments"], 3, "consignment stats Batched +3")

# ------------------------------------------------------------------------------------------ second analyst, continue, delayed
section("second analyst: continue batch, delayed analysis")
s = analyst2.j("GET", f"api/Lab/batches/{B2}/samples")["items"][0]
e = entry(analyst2, s)
analyst2.j("PUT", f"api/Lab/samples/{s['sampleItemId']}/values",
           json={"sampleItemId": s["sampleItemId"], "values": values_for(e, {"S-TEX": "Loam", "S-PH": "7.1"}), "submit": False})
a2 = analyst2.j("GET", "api/Lab/analyst/dashboard")
eq(a2["continueBatchId"], B2, "ContinueBatchId = most recent InProgress batch")
eq(a2["assignedBatches"] - a2dash0["assignedBatches"], 1, "analyst2 AssignedBatches +1")

if not ARGS.no_db:
    try:
        import psycopg2
        with psycopg2.connect(ARGS.db) as conn, conn.cursor() as cur:
            cur.execute('UPDATE "SampleBatches" SET "TakenForAnalysisAt" = now()::timestamp - interval \'7 days 1 hour\' WHERE "Id" = %s', (B2,))
        h2 = coord.j("GET", f"api/Lab/batches/{B2}")["header"]
        eq((h2["analysisDays"], h2["isDelayed"]), (7, True), "back-dated batch: 7 analysis days, delayed")
        eq(coord.j("GET", "api/Lab/batches/stats")["delayedAnalysis"] - bstats0["delayedAnalysis"], 1, "DelayedAnalysis +1")
        ok(B2 in [b["id"] for b in coord.j("GET", "api/Lab/batches?status=Delayed&pageSize=50")["items"]], "status=Delayed filter")
    except ImportError:
        print("  (psycopg2 not installed: delayed step skipped)")

print(f"\nQA-LAB data of this run: farmers {fa['name']}..D, collections {c1['code']}, {c2['code']}, {c3['code']}; "
      f"consignments {k1['code']}, {k2['code']}, {k3['code']}; batches {h1['code']} (Completed), {b2['header']['code']} (In Progress, analyst2)")
summary()
sys.exit(1 if FAILED else 0)
