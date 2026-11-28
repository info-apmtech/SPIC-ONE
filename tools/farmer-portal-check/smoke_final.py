"""
Farmer portal final fix wave smoke test, against a LOCAL API and the LOCAL spicone_dev database only.

    python tools/farmer-portal-check/smoke_final.py [--base http://localhost:5120] [--group A,B,C]

Needs the API in the Development environment with Sms:Provider = Log (devCode) on the local DB:

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

Group A (no data changes): the Farmer deny-by-default filter (staff APIs answer 403 to a farmer token, the farmer
APIs still answer) and the UserController account-admin roles (staff and farmers get 403; a SuperAdmin role is
refused to an Admin before anything is created).
Group C creates its own throw-away farmers (random 6xxxxxxxxx numbers) and deletes them again through the new
DELETE api/Farmer/profile; cleanup_final_local.sql removes anything a failed run leaves.
"""
import argparse
import json
import os
import random
import subprocess
import sys
import urllib.error
import urllib.request
import uuid
from urllib.parse import urlparse

sys.path.insert(0, os.path.dirname(__file__))
from smoke_task3 import ROOT, RUN_IP, call, check, failures, farmer_token, staff_token, qa_password  # noqa: E402

FARMER_DENIED = "This is not available in the farmer app."


def group_a(base, phone):
    print("Group A: Farmer deny-by-default")
    farmer = farmer_token(base, phone)
    mdo = staff_token(base, "qa.mdo", qa_password())
    admin = staff_token(base, "qa.admin", qa_password())

    denied = [
        ("POST", "api/User/create-user", {"name": "x", "email": "x@example.invalid", "userName": "smoke-never-created",
                                           "password": "Never-Created-1", "role": 14}),
        ("GET", "api/EmployeeRegistration", None),
        ("GET", "api/DealerFile/view/dealers/1/x.pdf", None),
        ("GET", "api/CSR2/by-csr1/1", None),
        ("POST", "api/Library/assistant/ask", {"question": "What is urea?"}),
        ("GET", "api/Library/assistant/conversations", None),
        ("DELETE", "api/User/delete-user/00000000-0000-0000-0000-000000000000", None),
        ("PATCH", "api/User/update-status/00000000-0000-0000-0000-000000000000?isActive=false", None),
        ("GET", "api/Sas/farmers", None),
        ("GET", "api/Help/audio/status", None),
        ("GET", "api/Metrics/summary", None),
        ("GET", "api/Profile", None),
    ]
    for method, path, body in denied:
        st, b, _ = call(base, method, path, body, token=farmer)
        msg = b.get("message") if isinstance(b, dict) else b
        check(f"farmer: {method} {path.split('?')[0]} -> 403", st == 403, (st, msg))
    st, b, _ = call(base, "GET", "api/EmployeeRegistration", token=farmer)
    check("farmer: 403 body carries the plain message", isinstance(b, dict) and b.get("message") == FARMER_DENIED, b)

    allowed = [
        ("GET", "api/Farmer/profile", (200,)),
        ("GET", "api/Authentication/permissions", (200,)),
        ("GET", "api/Community/stats", (200,)),
        ("GET", "api/Community/near", (200,)),
        ("GET", "api/State", (200,)),
        ("GET", "api/District/byState/1", (200,)),
        ("GET", "api/Lab/languages", (200,)),
        ("GET", "api/Lab/reports", (200,)),
        ("GET", "api/Sas/collections", (200,)),
        ("GET", "api/Sas/payments/me", (200,)),
        ("GET", "api/Library/1", (200, 404)),
    ]
    for method, path, ok in allowed:
        st, b, _ = call(base, method, path, token=farmer)
        check(f"farmer: {method} {path} still answers ({'/'.join(map(str, ok))})", st in ok, (st, str(b)[:200]))

    # Staff untouched by the filter.
    st, _, _ = call(base, "GET", "api/EmployeeRegistration", token=mdo)
    check("staff: MDO still reads api/EmployeeRegistration (not 403)", st != 403, st)
    st, _, _ = call(base, "GET", "api/Library/assistant/conversations", token=mdo)
    check("staff: MDO still opens the library assistant (200)", st == 200, st)
    st, _, _ = call(base, "GET", "api/Sas/lookups", token=mdo)
    check("staff: MDO still reads api/Sas/lookups (200)", st == 200, st)

    # UserController: account admin roles only; a SuperAdmin role only from a SuperAdmin.
    st, _, _ = call(base, "POST", "api/User/create-user", {"name": "x", "email": "x@example.invalid", "userName": "smoke-never-created",
                                                            "password": "Never-Created-1", "role": 8}, token=mdo)
    check("user admin: MDO create-user -> 403", st == 403, st)
    st, _, _ = call(base, "PATCH", "api/User/update-status/00000000-0000-0000-0000-000000000000?isActive=false", token=mdo)
    check("user admin: MDO update-status -> 403", st == 403, st)
    st, b, _ = call(base, "POST", "api/User/create-user", {"name": "x", "email": "x@example.invalid", "userName": "smoke-never-created",
                                                            "password": "Never-Created-1", "role": 14}, token=admin)
    check("user admin: Admin creating a SuperAdmin -> 403 (nothing created)", st == 403, (st, b))
    st, b, _ = call(base, "POST", "api/User/create-user", {"name": "x", "email": "x@example.invalid", "userName": "smoke-never-created",
                                                            "password": "Never-Created-1", "role": 12}, token=admin)
    check("user admin: Admin creating a Farmer -> 403 (nothing created)", st == 403, (st, b))
    st, _, _ = call(base, "PATCH", "api/User/update-status/00000000-0000-0000-0000-000000000000?isActive=true", token=admin)
    check("user admin: Admin update-status reaches the action (unknown id -> 400, not 403)", st == 400, st)


# ---------------------------------------------------------------- Group C helpers (local DB only)

PSQL = r"D:\Program Files\PostgreSQL\17\bin\psql.exe"


def sql(query):
    env = dict(os.environ, PGPASSWORD="1234")
    out = subprocess.run([PSQL, "-h", "localhost", "-U", "postgres", "-d", "spicone_dev", "-v", "ON_ERROR_STOP=1", "-Atc", query],
                         capture_output=True, text=True, env=env)
    if out.returncode != 0:
        sys.exit("local SQL failed: " + out.stderr.strip()[:400])
    return out.stdout.strip()


def q(text):
    return "'" + str(text).replace("'", "''") + "'"


def throwaway_phone():
    # 600xxxxxxx: the throw-away numbers cleanup_final_local.sql removes.
    return "600" + "".join(random.choice("0123456789") for _ in range(7))


def sign_in(base, phone):
    # Throw-away local numbers only: skip the 30 s resend cooldown between this script's own sign-ins.
    sql(f'delete from "FarmerOtpChallenges" where "Phone" = {q(phone)} and "ConsumedAtUtc" is not null')
    st, body, _ = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": phone})
    if st != 200 or not body.get("devCode"):
        sys.exit(f"otp/request failed ({st}): {body}")
    st, body, _ = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": phone, "code": body["devCode"]})
    if st != 200:
        sys.exit(f"otp/verify failed ({st}): {body}")
    return body


def multipart(base, path, token, files, field="files", fields=None):
    boundary = "----final" + uuid.uuid4().hex
    parts = []
    for k, v in (fields or {}).items():
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode())
    for name, data, ctype in files:
        parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"; filename="{name}"\r\n'
                      f"Content-Type: {ctype}\r\n\r\n").encode() + data + b"\r\n")
    parts.append(f"--{boundary}--\r\n".encode())
    req = urllib.request.Request(base.rstrip("/") + "/" + path, data=b"".join(parts), method="POST")
    req.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
    req.add_header("X-Forwarded-For", RUN_IP)
    req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return resp.status, json.loads(resp.read().decode() or "null")
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw


TINY_PNG = bytes.fromhex("89504e470d0a1a0a0000000d4948445200000001000000010806000000"
                         "1f15c4890000000d49444154789c6360000002000154a24f5d0000000049454e44ae426082")

PROFILE = {"name": "Smoke Final Farmer", "stateId": 1, "districtId": 1, "village": "Kovilpatti", "taluk": "Kovilpatti",
           "pinCode": "628501", "primaryCrops": ["Paddy"], "landAcres": 2.0, "irrigationType": 2, "preferredLanguage": "en",
           "latitude": 9.17, "longitude": 77.87}
YIELD = {"crop": "Paddy", "method": "canal-borewell", "areaAcres": 2.0, "ndvi": 0.72, "ndviSource": "value",
         "rainfallMm": 850, "npkPercent": 90}


def group_c(base, leaves):
    print("Group C: privacy, refresh lifetime, daily caps, account deletion")
    phone = throwaway_phone()
    login = sign_in(base, phone)
    tok, refresh = login["token"], login["refreshToken"]
    uid = sql(f'select "Id" from "AppUsers" where "UserName" = {q("f" + phone)}')
    check("signup: the throw-away farmer exists", bool(uid), phone[:2] + "xxxx")

    # ---- M4 refresh lifetime
    days = float(sql(f'select extract(epoch from max("ExpiresAt" - "CreatedAt")) / 86400 from "RefreshTokens" where "UserId" = {q(uid)}') or 0)
    check("refresh: a farmer's refresh token lives 30 days", 29.9 < days < 30.1, days)
    mdo_id = sql('select "Id" from "AppUsers" where "UserName" = \'qa.mdo\'')
    mdo = staff_token(base, "qa.mdo", qa_password())
    staff_days = float(sql(f'select extract(epoch from ("ExpiresAt" - "CreatedAt")) / 86400 from "RefreshTokens" where "UserId" = {q(mdo_id)} order by "Id" desc limit 1') or 0)
    check("refresh: staff refresh tokens keep their old lifetime (2 days, not 30)", 1.9 < staff_days < 2.1, staff_days)

    # ---- I4 consent on the first profile save
    st, b, _ = call(base, "PUT", "api/Farmer/profile", PROFILE, token=tok)
    check("privacy: first profile save -> 200", st == 200, (st, str(b)[:200]))
    row = sql(f'select coalesce("PrivacyVersion", \'\') || \'|\' || ("PrivacyAcceptedAt" is not null) from "FarmerProfiles" where "UserId" = {q(uid)}')
    check("privacy: the save recorded PrivacyAcceptedAt and version 2026-11-28", row == "2026-11-28|true", row)
    sql(f'update "FarmerProfiles" set "PrivacyVersion" = \'2020-01-01\', "PrivacyAcceptedAt" = null where "UserId" = {q(uid)}')
    login = sign_in(base, phone)
    tok, refresh = login["token"], login["refreshToken"]
    row = sql(f'select coalesce("PrivacyVersion", \'\') || \'|\' || ("PrivacyAcceptedAt" is not null) from "FarmerProfiles" where "UserId" = {q(uid)}')
    check("privacy: a sign-in past the consent line records the new version", row == "2026-11-28|true", row)

    # ---- M2 fertilizer / yield caps (50 per IST day, one SaveChanges)
    st, opt, _ = call(base, "GET", "api/Farmer/fertilizer/options", token=tok)
    norm = opt["crops"][0]["variants"][0]
    body = {"normId": norm["normId"], "soilType": norm.get("soilType") or "Red Soil", "areaAcres": 1, "n": 250, "p": 15, "k": 150, "ph": 7.0}
    st, r, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", body, token=tok)
    check("fertilizer: recommend 200", st == 200, (st, str(r)[:200]))
    run_id = r.get("runId") if isinstance(r, dict) else None
    stored = sql(f'select "ResultJson" from "FertilizerRecommendationRuns" where "Id" = {run_id or 0}')
    check("fertilizer: one save, the stored result is the full answer (not '{}')", stored not in ("", "{}"), stored[:60])
    st, again, _ = call(base, "GET", f"api/Farmer/fertilizer/history/{run_id}", token=tok)
    check("fertilizer: history shows the run with its id", st == 200 and again.get("runId") == run_id, (st, again.get("runId") if isinstance(again, dict) else again))
    for _ in range(49):
        call(base, "POST", "api/Farmer/fertilizer/recommend", body, token=tok)
    st, r, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", body, token=tok)
    check("fertilizer: the 51st plan today -> 429 with a plain message", st == 429 and "50 fertilizer plans" in r.get("message", ""), (st, r))
    count = sql(f'select count(*) from "FertilizerRecommendationRuns" where "FarmerUserId" = {q(uid)}')
    check("fertilizer: exactly 50 runs saved", count == "50", count)

    st, y, _ = call(base, "POST", "api/Farmer/yield/predict", YIELD, token=tok)
    check("yield: predict 200 with an id", st == 200 and y.get("id"), (st, str(y)[:200]))
    st, y2, _ = call(base, "GET", f"api/Farmer/yield/history/{y.get('id')}", token=tok)
    check("yield: history shows the forecast with its id", st == 200 and y2.get("id") == y.get("id"), y2)
    for _ in range(49):
        call(base, "POST", "api/Farmer/yield/predict", YIELD, token=tok)
    st, r, _ = call(base, "POST", "api/Farmer/yield/predict", YIELD, token=tok)
    check("yield: the 51st forecast today -> 429", st == 429 and "50 harvest forecasts" in r.get("message", ""), (st, r))

    # ---- M3 call-backs: 10 per IST day across officers
    st, team, _ = call(base, "GET", "api/Farmer/contacts", token=tok)
    members = team.get("members", []) if st == 200 else []
    check("callbacks: the farmer has a SPIC team", len(members) >= 1, (st, str(team)[:200]))
    if members:
        first = members[0]["contactId"]
        st, r, _ = call(base, "POST", f"api/Farmer/contacts/{first}/request", {"topic": 1, "message": "[smoke-final] call me"}, token=tok)
        check("callbacks: a request -> 200", st == 200, (st, r))
        role = sql(f'select "StaffRole" from "FarmerContactRequests" where "FarmerUserId" = {q(uid)} limit 1')
        values = ",".join(f"({q(uid)}, {q(first)}, {q(role)}, 'Other', '[smoke-final] seeded', 'Closed', now() at time zone 'utc', now() at time zone 'utc')" for _ in range(9))
        sql(f'insert into "FarmerContactRequests" ("FarmerUserId","StaffUserId","StaffRole","Topic","Message","Status","CreatedAtUtc","UpdatedAtUtc") values {values}')
        sql(f'update "FarmerContactRequests" set "Status" = \'Contacted\' where "Id" = (select min("Id") from "FarmerContactRequests" where "FarmerUserId" = {q(uid)} and "Status" = \'Closed\')')
        other = members[-1]["contactId"]
        st, r, _ = call(base, "POST", f"api/Farmer/contacts/{other}/request", {"topic": 1, "message": "[smoke-final] eleventh"}, token=tok)
        check("callbacks: the 11th request today (any officer) -> 429", st == 429 and "10 call-back requests" in r.get("message", ""), (st, r))

    # ---- I3 Community caps (5 posts, 30 replies, 20 MB, photos / PDF only)
    post_ids = []
    for i in range(5):
        st, p, _ = call(base, "POST", "api/Community/discussions", {"title": f"[smoke-final] question {i}", "body": "Paddy leaves are yellow."}, token=tok)
        if st == 200:
            post_ids.append(p["id"])
    check("community: 5 discussions today -> all 200", len(post_ids) == 5, post_ids)
    st, r, _ = call(base, "POST", "api/Community/discussions", {"title": "[smoke-final] sixth", "body": "x"}, token=tok)
    check("community: the 6th discussion today -> 429 with the plain message", st == 429 and "5 discussions a day" in r.get("message", ""), (st, r))
    replies = 0
    for i in range(30):
        st, _, _ = call(base, "POST", f"api/Community/discussions/{post_ids[0]}/replies", {"body": f"[smoke-final] reply {i}"}, token=tok)
        replies += st == 200
    check("community: 30 replies today -> all 200", replies == 30, replies)
    st, r, _ = call(base, "POST", f"api/Community/discussions/{post_ids[0]}/replies", {"body": "[smoke-final] 31st"}, token=tok)
    check("community: the 31st reply today -> 429", st == 429 and "30 replies a day" in r.get("message", ""), (st, r))
    st, r = multipart(base, f"api/Community/discussions/{post_ids[1]}/attachments", tok, [("notes.docx", b"PK\x03\x04docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")])
    check("community: a farmer's DOCX attachment -> 400 (photos and PDF only)", st == 400 and "PDF" in str(r), (st, r))
    st, r = multipart(base, f"api/Community/discussions/{post_ids[1]}/attachments", tok, [("leaf.png", TINY_PNG, "image/png")])
    check("community: a farmer's photo attachment -> 200", st == 200, (st, r))
    sql(f'insert into "CommunityPostAttachments" ("PostId","FileName","StoredPath","ContentType","Size","CreatedAt") '
        f'values ({post_ids[2]}, \'seeded.jpg\', \'Community/none/seeded.jpg\', \'image/jpeg\', {20 * 1024 * 1024 - 10}, now())')
    st, r = multipart(base, f"api/Community/discussions/{post_ids[2]}/attachments", tok, [("leaf.png", TINY_PNG, "image/png")])
    check("community: past 20 MB of attachments today -> 429", st == 429 and "20 MB" in str(r), (st, r))
    # Staff are not capped: five seeded posts for qa.mdo today, then one more through the API.
    values = ",".join(f"('[smoke-final] staff {i}', 'x', 1, {q(mdo_id)}, 'QA MDO', 'SPIC Expert', 0, 0, 0, now(), now(), now(), false)" for i in range(5))
    sql('insert into "CommunityPosts" ("Title","Body","Status","AuthorUserId","AuthorName","AuthorRole","Views","LikeCount","ReplyCount",'
        f'"LastActivityAt","CreatedAt","UpdatedAt","IsDeleted") values {values}')
    st, p, _ = call(base, "POST", "api/Community/discussions", {"title": "[smoke-final] staff sixth", "body": "x"}, token=mdo)
    check("community: staff are not capped (6th staff discussion -> 200)", st == 200, (st, str(p)[:200]))
    if st == 200:
        call(base, "DELETE", f"api/Community/discussions/{p['id']}", token=mdo)

    # ---- I4 account deletion: build up data first
    st, a, _ = call(base, "POST", "api/Farmer/assistant/ask", {"question": "How much urea for paddy?"}, token=tok)
    check("deletion setup: a SPIC AI chat", st == 200, (st, str(a)[:200]))
    if leaves:
        with open(os.path.join(leaves, "n_deficient.jpg"), "rb") as f:
            st, lc = multipart(base, "api/Farmer/leaf-check", tok, [("leaf.jpg", f.read(), "image/jpeg")], field="photo", fields={"crop": "Paddy"})
        check("deletion setup: a leaf check with a photo", st == 200, (st, str(lc)[:200]))
    sql(f'insert into "SasFarmers" ("Name","Mobile","UserId","CreatedAt","UpdatedAt","IsDeleted") values (\'[smoke-final] SAS\', {q(phone)}, {q(uid)}, now(), now(), false)')
    photo_dir = os.path.join(ROOT, "SpicAPI", "PrivateUploads", "FarmerLeaf", uid)
    if leaves:
        check("deletion setup: the leaf photo folder exists", os.path.isdir(photo_dir), photo_dir)

    st, _, _ = call(base, "DELETE", "api/Farmer/profile", token=mdo)
    check("delete: staff calling DELETE api/Farmer/profile -> 403", st == 403, st)
    st, b, _ = call(base, "DELETE", "api/Farmer/profile", token=tok)
    check("delete: DELETE api/Farmer/profile -> 204", st == 204, (st, b))

    counts = sql(
        f'select (select count(*) from "AppUsers" where "Id" = {q(uid)}) || \',\' || '
        f'(select count(*) from "FarmerProfiles" where "UserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "FarmerChatConversations" where "FarmerUserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "FarmerChatMessages" where "FarmerUserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "FarmerAiUsages" where "FarmerUserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "LeafDiagnoses" where "FarmerUserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "FertilizerRecommendationRuns" where "FarmerUserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "YieldPredictions" where "FarmerUserId" = {q(uid)}) || \',\' || '
        f'(select count(*) from "FarmerOtpChallenges" where "Phone" = {q(phone)}) || \',\' || '
        f'(select count(*) from "RefreshTokens" where "UserId" = {q(uid)})')
    check("delete: user, profile, chats, messages, AI usage, leaf checks, fertilizer, yield, OTP and refresh tokens all gone",
          counts == ",".join(["0"] * 10), counts)
    # The request log keeps its rows without who it was; only the DELETE call itself is logged after it ran.
    logs = sql(f'select count(*) from "AppRequestLogs" where "UserId" = {q(uid)}')
    check("delete: request log rows no longer name the farmer (only the DELETE call itself)", int(logs or 0) <= 1, logs)
    reqs = sql(f'select string_agg("Status", \',\' order by "Status") from "FarmerContactRequests" where "FarmerUserId" = {q(uid)}')
    check("delete: the open call-back is gone, handled ones stay closed for the officer", reqs and "Open" not in reqs and "Contacted" not in reqs and reqs.count("Closed") == 9, reqs)
    com = sql(f'select count(*) filter (where not "IsDeleted") || \',\' || count(*) filter (where "AuthorName" <> \'Deleted account\') from "CommunityPosts" where "AuthorUserId" = {q(uid)}')
    check("delete: Community discussions taken down, author name cleared", com == "0,0", com)
    rep = sql(f'select count(*) filter (where not "IsDeleted") from "CommunityPostReplies" where "AuthorUserId" = {q(uid)}')
    check("delete: Community replies taken down", rep == "0", rep)
    sas = sql(f'select count(*) || \',\' || count(*) filter (where "UserId" is null) from "SasFarmers" where "Mobile" = {q(phone)}')
    check("delete: the SAS farmer record stays, unlinked", sas == "1,1", sas)
    if leaves:
        check("delete: the leaf photo folder is deleted after the commit", not os.path.exists(photo_dir), photo_dir)
    st, _, _ = call(base, "POST", "api/Authentication/refresh", {"refreshToken": refresh})
    check("delete: the old refresh token no longer works (401)", st == 401, st)

    again = sign_in(base, phone)
    new_uid = sql(f'select "Id" from "AppUsers" where "UserName" = {q("f" + phone)}')
    check("delete: the same number signs up fresh as a new account", again.get("isNewUser") is True and new_uid and new_uid != uid, (again.get("isNewUser"), bool(new_uid)))
    st, _, _ = call(base, "DELETE", "api/Farmer/profile", token=again["token"])
    check("delete: a farmer without a profile can delete too (204)", st == 204, st)
    check("delete: that account is gone as well", sql(f'select count(*) from "AppUsers" where "UserName" = {q("f" + phone)}') == "0")
    sql(f'delete from "SasFarmers" where "Mobile" = {q(phone)} and "Name" = \'[smoke-final] SAS\'')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--group", default="A,C")
    ap.add_argument("--leaves", default="", help="folder written by make_leaves.py (adds a leaf check to the deletion test)")
    args = ap.parse_args()
    if (urlparse(args.base).hostname or "") not in ("localhost", "127.0.0.1"):
        sys.exit("Refusing a non-local API: this smoke test runs against the LOCAL API only.")
    groups = set(args.group.upper().split(","))
    if "A" in groups:
        group_a(args.base, args.phone)
    if "C" in groups:
        group_c(args.base, args.leaves)
    print()
    print("All checks passed." if not failures else f"{len(failures)} check(s) FAILED: {failures}")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
