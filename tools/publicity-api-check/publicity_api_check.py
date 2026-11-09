"""
Publicity Material Tracking - end-to-end API check (LOCAL database only, never the live API).

    python tools/publicity-api-check/publicity_api_check.py --base http://localhost:5131

Needs the local QA users qa.rmd (HQ 1), qa.rmd2 (second HQ in region 1), qa.rm, qa.smd, qa.smm,
qa.mdadmin, qa.mdhod (see README.md for the seed). Walks the whole workflow:
  draft -> submit -> RM edit + approve -> SMD bulk -> SMM consolidation -> MD CS -> DGM final
  -> return / resubmit -> vendor dispatch -> in transit -> delivered -> acknowledgement with
  shortage + damage -> exception -> stock transfer order from another HQ's excess -> courier proof
  (+ OCR) -> admin confirms -> delivered -> acknowledgement -> completed.
"""
import argparse
import base64
import sys
import requests

PARSER = argparse.ArgumentParser()
PARSER.add_argument("--base", default="http://localhost:5131/")
PARSER.add_argument("--password", default="QaPass@2026#")
ARGS = PARSER.parse_args()
BASE = ARGS.base.rstrip("/") + "/"
PASSED, FAILED = [], []

# 1x1 PNG
PNG = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==")

STATUS = {0: "Draft", 1: "PendingApproval", 2: "PendingFinalApproval", 3: "FinalApproved", 4: "InDispatch", 5: "Delivered",
          6: "AcknowledgementPending", 7: "ExceptionRaised", 8: "ReInitiationInProgress", 9: "TransferInProgress",
          10: "ExceptionResolved", 11: "Completed", 12: "Rejected", 13: "TransferOrderReceived", 14: "Returned"}


def ok(cond, msg):
    (PASSED if cond else FAILED).append(msg)
    print(("  ok    " if cond else "  FAIL  ") + msg)
    return cond


def need(cond, msg):
    if not ok(cond, msg):
        summary()
        sys.exit(1)


def summary():
    print(f"\n{len(PASSED)} checks passed, {len(FAILED)} failed")
    for f in FAILED:
        print("  - " + f)


class Client:
    def __init__(self, user):
        self.user = user
        r = requests.post(BASE + "api/Authentication/login", json={"userName": user, "password": ARGS.password}, timeout=30)
        need(r.status_code == 200, f"login {user} -> {r.status_code} {r.text[:150]}")
        token = r.json().get("token") or r.json().get("Token")
        self.token = token[7:] if token.lower().startswith("bearer ") else token
        self.s = requests.Session()
        self.s.headers["Authorization"] = "Bearer " + self.token

    def call(self, method, path, expect=200, **kw):
        r = self.s.request(method, BASE + "api/Publicity/" + path, timeout=90, **kw)
        ok(r.status_code == expect, f"{self.user} {method} {path} -> {r.status_code}" + ("" if r.status_code == expect else f" {r.text[:300]}"))
        return r.json() if r.content and r.headers.get("content-type", "").startswith("application/json") else None

    def get(self, path, expect=200):
        return self.call("GET", path, expect)

    def post(self, path, body=None, expect=200):
        return self.call("POST", path, expect, json=body)

    def put(self, path, body=None, expect=200):
        return self.call("PUT", path, expect, json=body)

    def upload(self, rid, kind, dispatch=None, name="file.png"):
        q = f"requests/{rid}/files?kind={kind}" + (f"&dispatchId={dispatch}" if dispatch else "")
        return self.call("POST", q, 200, files={"files": (name, PNG, "image/png")})


def status(c, rid):
    d = c.get(f"requests/{rid}")
    return STATUS[d["status"]], d


print(f"Publicity API check against {BASE}")
rmd, rmd2, rm, smd, smm, mda, mdh = (Client(u) for u in ["qa.rmd", "qa.rmd2", "qa.rm", "qa.smd", "qa.smm", "qa.mdadmin", "qa.mdhod"])

lk = rmd.get("lookups")
need(len(lk["materials"]) >= 8 and len(lk["products"]) >= 1, "lookups: 8 materials and products")
mat, prod = lk["materials"][0]["id"], lk["products"][0]["id"]
hq1 = lk["myHqId"]
hq2 = rmd2.get("lookups")["myHqId"]
need(hq1 > 0 and hq2 > 0 and hq1 != hq2, "two RMDO users on different HQs")


def create(c, hq, qty, submit=True):
    body = {"materialTypeId": mat, "productId": prod, "quantity": qty, "stateId": 1, "regionId": 1, "headquarterId": hq,
            "remarks": "Need posters for farmer meetings", "submit": submit}
    return c.post("requests", body)["id"]


def approve_all(rid):
    for c in (rm, smd, smm, mda, mdh):
        c.post(f"requests/{rid}/review", {"action": "Approve"})


# ---------------------------------------------------------------- B: excess source at HQ 2
print("\n== source request with excess (HQ 2)")
b = create(rmd2, hq2, 100)
approve_all(b)
ok(status(mda, b)[0] == "FinalApproved", "B final approved after 5 approvals")
db = mda.post(f"requests/{b}/dispatches", {"kind": 0, "vendorName": "Creative Prints Pvt Ltd", "quantity": 120, "transporter": "Blue Dart Express",
                                           "trackingNo": "BD123", "dispatchDate": "2026-11-08T00:00:00", "destinationAddress": "Chennai HQ"})
rmd2.put(f"dispatches/{db['id']}/ack", {"receivedQty": 120, "submit": True})
st, dB = status(rmd2, b)
ok(st == "Completed" and dB["summary"]["excess"] == 20, f"B completed with 20 excess ({st}, excess {dB['summary']['excess']})")

# ---------------------------------------------------------------- A: main flow at HQ 1
print("\n== draft, design file, submit")
a = create(rmd, hq1, 100, submit=False)
ok(status(rmd, a)[0] == "Draft", "A is a draft")
rm.get(f"requests/{a}", expect=404)  # drafts are private
rmd.upload(a, 0, name="neem_gold_poster.png")
rmd.put(f"requests/{a}", {"materialTypeId": mat, "productId": prod, "quantity": 100, "stateId": 1, "regionId": 1, "headquarterId": hq1, "submit": True})
st, d = status(rmd, a)
ok(st == "PendingApproval" and d["stage"] == 1, "A submitted to RM review")
ok(any(f["kind"] == 0 for f in d["files"]), "design file attached")

print("\n== approvals")
smd.post(f"requests/{a}/review", {"action": "Approve"}, expect=400)  # not SMD's turn
rm.post(f"requests/{a}/review", {"action": "Edit", "quantity": 100, "remarks": "checked"})
smd.put(f"requests/{a}", {"materialTypeId": mat, "productId": prod, "quantity": 95, "stateId": 1, "regionId": 1, "headquarterId": hq1, "remarks": "Need posters for farmer meetings"}, expect=400)  # not SMD's turn
rm.put(f"requests/{a}", {"materialTypeId": mat, "productId": prod, "quantity": 95, "stateId": 1, "regionId": 1, "headquarterId": hq1, "remarks": "Need posters for farmer meetings"})
st, d = status(rm, a)
ok(st == "PendingApproval" and d["stage"] == 1 and d["quantity"] == 95 and any("95" in (h["remarks"] or "") for h in d["history"]), "RM full edit keeps the stage and logs the change")
rm.put(f"requests/{a}", {"materialTypeId": mat, "productId": prod, "quantity": 100, "stateId": 1, "regionId": 1, "headquarterId": hq1, "remarks": "Need posters for farmer meetings"})
rm.post(f"requests/{a}/review", {"action": "Approve", "remarks": "ok"})
smd.post("requests/review-bulk", {"ids": [a]})
con = smm.get("consolidation")
ok(any(r["id"] == a for g in con["groups"] for r in g["rows"]), "A appears in the SMM consolidation")
smm.post("requests/review-bulk", {"ids": [a]})
ok(rmd.post(f"requests/{a}/dispatches", {"kind": 0, "quantity": 1}, expect=403) is None, "RMDO cannot dispatch")
mda.post(f"requests/{a}/review", {"action": "Approve"})
ok(mdh.get("requests/stats")["withDgm"] >= 1, "stats: one request with DGM-MD CS")
mdh.post(f"requests/{a}/review", {"action": "Approve"})
st, d = status(mda, a)
ok(st == "FinalApproved" and d["canDispatch"], "A final approved, MD CS can dispatch")
ok([h["stage"] for h in d["history"]][:4] == ["Indent Creation", "Submitted to RM", "RM Review", "RM Review"], "approval history order")

print("\n== return and resubmit")
c2 = create(rmd, hq1, 50)
rm.post(f"requests/{c2}/review", {"action": "Return"}, expect=400)  # needs remarks
rm.post(f"requests/{c2}/review", {"action": "Return", "remarks": "Reduce quantity"})
st, d = status(rmd, c2)
ok(st == "Returned" and d["canEdit"], "returned request is editable by RMDO")
rmd.put(f"requests/{c2}", {"materialTypeId": mat, "productId": prod, "quantity": 40, "stateId": 1, "regionId": 1, "headquarterId": hq1, "submit": True})
ok(status(rmd, c2)[0] == "PendingApproval", "resubmitted")

print("\n== vendor dispatch, transit, delivery")
da = mda.post(f"requests/{a}/dispatches", {"kind": 0, "vendorName": "Creative Prints Pvt Ltd", "quantity": 100, "transporter": "Blue Dart Express",
                                           "trackingNo": "BD12345679IN", "dispatchDate": "2026-11-08T00:00:00", "expectedDeliveryDate": "2026-11-12T00:00:00",
                                           "destinationAddress": "123, SPIC Building, Anna Salai, Chennai"})
mda.upload(a, 7, da["id"], "note_approval.png")
ok(status(rmd, a)[0] == "InDispatch", "A in dispatch")
mda.post(f"dispatches/{da['id']}/status", {"status": 3, "currentLocation": "Bengaluru Hub"})
mda.post(f"dispatches/{da['id']}/status", {"status": 4, "currentLocation": "Chennai Hub"})
st, d = status(rmd, a)
ok(st == "AcknowledgementPending" and d["dispatches"][0]["canAcknowledge"], "delivered, RMDO can acknowledge")

print("\n== acknowledgement with shortage + damage")
rmd.put(f"dispatches/{da['id']}/ack", {"receivedQty": 90, "isDamaged": True, "damagedQty": 5, "remarks": "draft"})
ok(status(rmd, a)[0] == "AcknowledgementPending", "draft ack does not move the status")
rmd.upload(a, 4, da["id"], "ack_photo.png")
rmd.put(f"dispatches/{da['id']}/ack", {"receivedQty": 90, "isDamaged": True, "damagedQty": 95}, expect=400)
rmd.put(f"dispatches/{da['id']}/ack", {"receivedQty": 90, "isDamaged": True, "damagedQty": 5,
                                       "remarks": "Received 90 posters. 5 damaged, 10 missing.", "submit": True})
st, d = status(mda, a)
s = d["summary"]
ok(st == "ExceptionRaised" and s["outstanding"] == 15 and s["shortage"] == 10 and s["damaged"] == 5, f"exception raised, outstanding 15 ({s})")
ok(d["canResolve"] and any(x["requestId"] == b and x["remaining"] == 20 for x in d["excessSources"]), "B offered as excess source (20)")
ok(any(n["type"] == 1 for n in mda.get("notifications")), "MD CS got the exception notification")

print("\n== stock transfer order from HQ 2")
mda.post(f"requests/{a}/dispatches", {"kind": 2, "sourceRequestId": b, "quantity": 25}, expect=400)  # more than excess
tr = mda.post(f"requests/{a}/dispatches", {"kind": 2, "sourceRequestId": b, "quantity": 15, "remarks": "Shortage resolution for Madurai HQ"})
ok(status(rmd, a)[0] == "TransferInProgress", "A transfer in progress")
ok(status(rmd2, b)[0] == "TransferOrderReceived", "B transfer order received")
notes = rmd2.get("notifications")
note = next((n for n in notes if n["dispatchId"] == tr["id"]), None)
need(note is not None, "source RMDO notified")
nd = rmd2.get(f"notifications/{note['id']}")
ok(nd["transferQty"] == 15 and nd["remainingExcess"] == 20, "drawer shows transfer 15 of 20 remaining")
rmd2.post(f"notifications/{note['id']}/read")
ok(all(n["isRead"] for n in rmd2.get("notifications") if n["id"] == note["id"]), "marked as read")
rmd.post(f"dispatches/{tr['id']}/proof", {"transporter": "DTDC", "trackingNo": "X1", "dispatchDate": "2026-11-08T00:00:00"}, expect=403)
rmd2.post(f"dispatches/{tr['id']}/proof", {"transporter": "DTDC", "trackingNo": "X1", "dispatchDate": "2026-11-08T00:00:00"}, expect=400)  # no proof file
rmd2.upload(b, 5, tr["id"], "Dispatch_Proof.png")
ocr = rmd2.call("POST", "dispatches/ocr", 200, files={"file": ("p.png", PNG, "image/png")})
ok(ocr is not None and "found" in ocr, "OCR endpoint answers")
rmd2.post(f"dispatches/{tr['id']}/proof", {"transporter": "Blue Dart Express", "trackingNo": "AWB-9842019", "dispatchDate": "2026-11-08T00:00:00", "vehicleNo": "TN 37 AB 1234"})
st, d = status(mda, a)
ok(d["dispatches"][1]["status"] == 1 and d["dispatches"][1]["steps"][2]["state"] == 1, "proof submitted, admin review is the current step")
mda.post(f"dispatches/{tr['id']}/status", {"status": 2, "expectedDeliveryDate": "2026-11-10T00:00:00"})
mda.post(f"dispatches/{tr['id']}/status", {"status": 4})
rmd.put(f"dispatches/{tr['id']}/ack", {"receivedQty": 15, "submit": True})
st, d = status(rmd, a)
s = d["summary"]
ok(st == "Completed" and s["outstanding"] == 0 and s["exceptionStatus"] == "Resolved" and s["usable"] == 100, f"A completed, exception resolved ({s})")
ok(status(rmd2, b)[0] == "Completed", "B back to completed")

print("\n== scope, files, list")
ok(all(r["id"] != b for r in rmd.get("requests?size=100")["items"]), "RMDO HQ1 does not list HQ2's request")
ok(any(r["id"] == a for r in mda.get("requests?size=100&submittedOnly=true")["items"]), "MD CS lists every request")
path = d["files"][0]["path"]
r = requests.get(BASE + "api/Publicity/file/" + path, params={"access_token": rmd.token}, timeout=30)
ok(r.status_code == 200 and r.content == PNG, "file served with access_token")
r = requests.get(BASE + "api/Publicity/file/../appsettings.json", params={"access_token": rmd.token}, timeout=30)
ok(r.status_code == 404, "path traversal refused")
ok(">" in rm.get("lookups")["myScope"] and "State Level" in smd.get("lookups")["myScope"], "scope chips for RM and SMD")
ok(rm.get("requests/stats")["approvedByMe"] >= 2, "RM stats count requests it approved")
stats = rmd.get("requests/stats")
ok(stats["completed"] >= 1, f"RMDO stats {stats}")

summary()
sys.exit(1 if FAILED else 0)
