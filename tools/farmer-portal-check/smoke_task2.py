"""
Farmer portal Task 2 smoke test: dealers near me, my SPIC team, call-back requests and the staff
side, against a LOCAL API and the LOCAL spicone_dev database only.

    psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task2_local.sql
    python tools/farmer-portal-check/smoke_task2.py [--base http://localhost:5120] [--phone 6777777703]

Needs the API in the Development environment with Sms:Provider = Log (devCode) on the local DB:

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

The farmer (default the local test farmer 6777777703, Kovilpatti, Thoothukudi) signs in through the
devCode. Staff sign in as the local QA users (password from tools/help-demo/shots.json). Requests it
creates carry the message prefix "[smoke]" and are closed again; cleanup_task2_local.sql deletes them.
"""
import argparse
import json
import os
import random
import sys
import urllib.error
import urllib.parse
import urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
failures = []
RUN_IP = "198.18.%d.%d" % (random.randint(0, 255), random.randint(1, 254))

KOVILPATTI = (9.17, 77.87)
CHENNAI = (13.08, 80.27)
NEVER_LISTED = {"Demo Terminated Agro", "Demo To Be Terminated", "Demo New Dealer Pending"}
PRIVATE_KEYS = {"panNumber", "accountNumber", "ifsc", "gstNumber", "aadhaarNumber", "accountHolderName", "branch"}


def call(base, method, path, body=None, token=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(base.rstrip("/") + "/" + path.lstrip("/"), data=data, method=method)
    req.add_header("Content-Type", "application/json")
    req.add_header("X-Forwarded-For", RUN_IP)
    if token:
        req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            raw = resp.read().decode()
            return resp.status, (json.loads(raw) if raw.strip().startswith(("{", "[")) else raw)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw


def check(name, ok, detail=""):
    print(("  PASS  " if ok else "  FAIL  ") + name + ("" if ok else "  -- " + str(detail)[:400]))
    if not ok:
        failures.append(name)


def farmer_token(base, phone):
    st, body = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": phone})
    if st != 200 or not body.get("devCode"):
        sys.exit(f"otp/request failed ({st}): {body}. Is the API in Development with Sms:Provider=Log?")
    st, body = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": phone, "code": body["devCode"]})
    if st != 200:
        sys.exit(f"otp/verify failed ({st}): {body}")
    return body["token"]


def staff_token(base, user, password):
    st, body = call(base, "POST", "api/Authentication/login", {"userName": user, "password": password})
    if st != 200:
        sys.exit(f"staff login {user} failed ({st}); run seed_task2_local.sql (it activates the QA staff users).")
    return body["token"]


def names(listing):
    return [d["firmName"] for d in listing["items"]]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703")
    args = ap.parse_args()
    if urllib.parse.urlparse(args.base).hostname not in ("localhost", "127.0.0.1", "::1"):
        sys.exit("Refusing a non-local API: this smoke test writes test rows.")
    base = args.base
    with open(os.path.join(ROOT, "tools", "help-demo", "shots.json"), encoding="utf-8") as f:
        qa_password = json.load(f)["password"]

    farmer = farmer_token(base, args.phone)

    print("Dealer locator")
    st, near = call(base, "GET", f"api/Farmer/dealers?lat={KOVILPATTI[0]}&lng={KOVILPATTI[1]}", token=farmer)
    check("nearby: 200", st == 200, near)
    check("nearby: scope Nearby (0)", near.get("scope") == 0, near.get("scope"))
    check("nearby: default radius 25", near.get("radiusKm") == 25, near.get("radiusKm"))
    dists = [d["distanceKm"] for d in near.get("items", [])]
    check("nearby: sorted by distance, all within 25 km", dists == sorted(dists) and all(0 <= x <= 25 for x in dists), dists)
    check("nearby: Demo Agro Centre first", names(near)[:1] == ["Demo Agro Centre"], names(near))
    check("nearby: never terminated / to-be-terminated / unapproved new", not (set(names(near)) & NEVER_LISTED), names(near))
    flat = json.dumps(near).lower()
    check("nearby: no PAN / bank / GST / Aadhaar values", "demopan" not in flat and "demoacc" not in flat and "demogst" not in flat and "demoaadhaar" not in flat)
    check("nearby: no private keys in items", all(not (PRIVATE_KEYS & set(d.keys())) for d in near.get("items", [])))
    first = near["items"][0] if near.get("items") else {}
    check("nearby: card has phone, WhatsApp and directions", first.get("phone") == "9000000001" and first.get("whatsApp") == "9000000001"
          and str(first.get("directionsUrl", "")).startswith("https://www.google.com/maps/dir/"), first)
    land = next((d for d in near.get("items", []) if d["firmName"] == "Demo Farm Inputs"), {})
    check("nearby: landline is dialable, WhatsApp kept separately", land.get("phone") == "04612000001" and land.get("whatsApp") == "9000000002", land)

    st, wide = call(base, "GET", f"api/Farmer/dealers?lat={KOVILPATTI[0]}&lng={KOVILPATTI[1]}&radiusKm=500", token=farmer)
    check("radius: capped at 100", wide.get("radiusKm") == 100, wide.get("radiusKm"))
    check("radius: 100 km reaches Tirunelveli", "Demo Green Traders" in names(wide), names(wide))

    st, far = call(base, "GET", f"api/Farmer/dealers?lat={CHENNAI[0]}&lng={CHENNAI[1]}", token=farmer)
    check("fallback: GPS far away -> District (1)", far.get("scope") == 1, far.get("scope"))
    check("fallback: district list includes the no-GPS dealer last", names(far)[-1:] == ["Demo Sakthi Fertilisers"], names(far))
    check("fallback: district list is only Thoothukudi", "Demo Green Traders" not in names(far), names(far))

    st, nogps = call(base, "GET", "api/Farmer/dealers", token=farmer)
    check("no GPS: District scope, by name", nogps.get("scope") == 1 and names(nogps) == sorted(names(nogps), key=str.lower), names(nogps))
    check("no GPS: no distances", all(d.get("distanceKm") is None for d in nogps.get("items", [])))
    check("I3: an approved new dealer in a maintenance cycle (AVPApproved reset) is still listed",
          "Demo Maintenance Agro" in names(nogps), names(nogps))

    st, q = call(base, "GET", "api/Farmer/dealers?q=ettaya", token=farmer)
    check("search: village search", names(q) == ["Demo Farm Inputs"], names(q))
    st, q2 = call(base, "GET", "api/Farmer/dealers?q=%25", token=farmer)
    check("search: a % is literal, not a wildcard", q2.get("total") == 0, q2.get("total"))

    st, other = call(base, "GET", "api/Farmer/dealers?districtId=2", token=farmer)
    check("district parameter: Tirunelveli", names(other) == ["Demo Green Traders"], names(other))
    st, bad = call(base, "GET", "api/Farmer/dealers?districtId=999999", token=farmer)
    check("district parameter: unknown district is 400", st == 400, st)

    st, paged = call(base, "GET", "api/Farmer/dealers?pageSize=2&page=2", token=farmer)
    check("paging: page 2 of size 2", paged.get("page") == 2 and len(paged.get("items", [])) == 2 and paged.get("total", 0) >= 4, paged)

    print("My SPIC team")
    st, team = call(base, "GET", "api/Farmer/contacts", token=farmer)
    check("team: 200", st == 200, team)
    roles = [m["role"] for m in team.get("members", [])]
    check("team: district basis (0)", team.get("basis") == 0, team.get("basis"))
    order = {10: 0, 8: 1, 6: 2, 7: 3}
    member_names = [m["name"] for m in team.get("members", [])]
    check("I1: an MO account with an MDO login row (qa.mo) is not on the team", "QA MO" not in member_names, member_names)
    check("team: JMDO, MDO, RM, RMD (in that order, all four present)",
          [order[r] for r in roles] == sorted(order[r] for r in roles) and set(roles) == {10, 8, 6, 7}, roles)
    check("team: role labels in plain words", team["members"][0]["roleLabel"] == "Junior Market Development Officer", team["members"][0])
    check("team: JMDO area is the headquarter", team["members"][0].get("areaName") == "Madurai HQ" and team["members"][0].get("areaKind") == "Headquarter", team["members"][0])
    check("team: phone numbers hidden by default", team.get("phonesShown") is False and all(m.get("phone") is None for m in team["members"]))
    jmdo = team["members"][0]
    check("team: names come from the employee record", jmdo["name"] == "Demo JMDO Priya S", jmdo["name"])

    print("Call-back requests")
    st, err = call(base, "POST", f"api/Farmer/contacts/{jmdo['contactId']}/request", {"topic": 5, "message": "  "}, token=farmer)
    check("request: Other without a message is 400", st == 400 and "message" in (err.get("errors") or {}), err)
    st, err = call(base, "POST", f"api/Farmer/contacts/{jmdo['contactId']}/request", {"topic": 0, "message": "x" * 501}, token=farmer)
    check("request: 501 characters is 400", st == 400, st)
    st, err = call(base, "POST", "api/Farmer/contacts/not-a-staff-id/request", {"topic": 0}, token=farmer)
    check("request: someone not on the team is 404", st == 404, st)

    # Close anything a previous run left open so the cap check starts from zero.
    jmdo_staff = staff_token(base, "qa.jmdo", qa_password)
    st, mine = call(base, "GET", "api/FarmerRequests?status=Open&pageSize=100", token=jmdo_staff)
    for r in mine.get("items", []):
        call(base, "PUT", f"api/FarmerRequests/{r['id']}", {"status": 2, "note": "[smoke] reset"}, token=jmdo_staff)

    created = []
    for i in range(3):
        st, body = call(base, "POST", f"api/Farmer/contacts/{jmdo['contactId']}/request",
                        {"topic": 1, "message": f"[smoke] leaves turning yellow {i}", "preferredTime": "Morning"}, token=farmer)
        check(f"request: open request {i + 1} accepted", st == 200 and body.get("status") == 0, body)
        created.append(body.get("id"))
    st, body = call(base, "POST", f"api/Farmer/contacts/{jmdo['contactId']}/request", {"topic": 0, "message": "[smoke] fourth"}, token=farmer)
    check("request: the 4th open request to one officer is 409", st == 409, (st, body))
    st, body = call(base, "POST", f"api/Farmer/contacts/{team['members'][1]['contactId']}/request",
                    {"topic": 2, "message": "[smoke] soil test please", "preferredTime": "Anytime"}, token=farmer)
    check("request: another officer still accepts", st == 200, body)
    created.append(body.get("id"))

    st, mine = call(base, "GET", "api/Farmer/contacts/requests", token=farmer)
    check("my requests: newest first, includes the new ones", st == 200 and all(i in [r["id"] for r in mine] for i in created), mine)
    check("my requests: no staff note field for the farmer", all("staffNote" not in r for r in mine))
    st, team2 = call(base, "GET", "api/Farmer/contacts", token=farmer)
    check("team: open count per officer", team2["members"][0]["openRequests"] == 3, team2["members"][0])

    print("Staff side")
    st, lst = call(base, "GET", "api/FarmerRequests?status=Open", token=jmdo_staff)
    check("staff: JMDO sees its open requests", st == 200 and lst.get("open", 0) >= 3 and not lst.get("adminView"), lst)
    row = next((r for r in lst.get("items", []) if r["id"] == created[0]), None)
    check("I2: the addressed officer gets the full phone, not masked", row is not None and row.get("farmerPhone") == args.phone
          and row.get("farmerPhoneMasked") is False, row)
    check("staff: farmer phone, village and district shown", row is not None and row.get("farmerPhone") == args.phone
          and row.get("village") and row.get("districtName") == "Thoothukudi", row)
    check("staff: only requests addressed to the caller", all(r["staffRole"] == 10 for r in lst.get("items", [])))

    mdo_staff = staff_token(base, "qa.mdo", qa_password)
    st, body = call(base, "PUT", f"api/FarmerRequests/{created[0]}", {"status": 1, "note": "[smoke] not mine"}, token=mdo_staff)
    check("staff: another officer cannot update it (404)", st == 404, st)
    st, body = call(base, "PUT", f"api/FarmerRequests/{created[0]}", {"status": 1, "note": "[smoke] Called, will visit Monday"}, token=jmdo_staff)
    check("staff: addressed JMDO marks it Contacted with a note", st == 200 and body.get("status") == 1 and body.get("staffNote", "").endswith("Monday"), body)
    st, body = call(base, "PUT", f"api/FarmerRequests/{created[0]}", {"status": 1, "note": "n" * 501}, token=jmdo_staff)
    check("staff: a note over 500 characters is 400", st == 400, st)

    admin = staff_token(base, "qa.admin", qa_password)
    st, alist = call(base, "GET", "api/FarmerRequests?status=Contacted", token=admin)
    check("staff: Admin sees every request, read-only", st == 200 and alist.get("adminView") is True
          and any(r["id"] == created[0] for r in alist.get("items", [])) and all(not r["canUpdate"] for r in alist.get("items", [])), alist)
    arow = next((r for r in alist.get("items", []) if r["id"] == created[0]), {})
    masked = args.phone[:2] + "x" * 6 + args.phone[-2:]
    check("I2: Admin sees the farmer phone masked (98xxxxxx21 style)", arow.get("farmerPhone") == masked and arow.get("farmerPhoneMasked") is True, arow)
    check("I2: no full farmer phone anywhere in the Admin list", args.phone not in json.dumps(alist))
    st, _ = call(base, "PUT", f"api/FarmerRequests/{created[0]}", {"status": 2}, token=admin)
    check("staff: Admin cannot update (403)", st == 403, st)
    st, fbody = call(base, "GET", "api/FarmerRequests", token=farmer)
    check("staff: a farmer cannot open the staff list (403)", st == 403, st)
    mo = None
    try:
        mo = staff_token(base, "qa.mo", qa_password)
    except SystemExit:
        pass
    if mo:
        st, _ = call(base, "GET", "api/FarmerRequests", token=mo)
        check("staff: MO cannot open the staff list (403)", st == 403, st)
    st, _ = call(base, "GET", "api/Farmer/dealers", token=jmdo_staff)
    check("farmer API: staff token gets 403", st == 403, st)
    st, _ = call(base, "GET", "api/Farmer/contacts")
    check("farmer API: anonymous gets 401", st == 401, st)

    # Leave the JMDO's queue as it was: close what this run opened.
    for rid in created:
        token = jmdo_staff if rid != created[-1] else mdo_staff
        call(base, "PUT", f"api/FarmerRequests/{rid}", {"status": 2, "note": "[smoke] closed by the smoke test"}, token=token)

    print()
    print(f"{'FAILED' if failures else 'OK'}: {len(failures)} failure(s)")
    for f in failures:
        print("  -", f)
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
