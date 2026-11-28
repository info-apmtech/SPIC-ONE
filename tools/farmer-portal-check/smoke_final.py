"""
Farmer portal final fix wave smoke test, against a LOCAL API and the LOCAL spicone_dev database only.

    python tools/farmer-portal-check/smoke_final.py [--base http://localhost:5120] [--group A,B,C]

Needs the API in the Development environment with Sms:Provider = Log (devCode) on the local DB:

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

Group A (no data changes): the Farmer deny-by-default filter (staff APIs answer 403 to a farmer token, the farmer
APIs still answer) and the UserController account-admin roles (staff and farmers get 403; a SuperAdmin role is
refused to an Admin before anything is created).
Group R (recheck fixes) seeds a shared SAS collection and probe files under SpicAPI/Uploads and removes them again.
Group C creates its own throw-away farmers (random 6xxxxxxxxx numbers) and deletes them again through the new
DELETE api/Farmer/profile; cleanup_final_local.sql removes anything a failed run leaves.
"""
import argparse
import json
import os
import random
import subprocess
import time
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

    # Residual fix R1: [AllowAnonymous] endpoints that trust "any signed-in user" refuse a Farmer token too.
    for method, path in (("GET", "api/IfmsAutomation/status"), ("GET", "api/IfmsAutomation/challenge/pending"),
                         ("GET", "api/IfmsAutomation/alerts/email")):
        st, b, _ = call(base, method, path, token=farmer)
        check(f"farmer (R1): {method} {path} -> 403", st == 403 and isinstance(b, dict) and b.get("message") == FARMER_DENIED, (st, b))
    st, b = multipart(base, "api/ExcelBulkUpload/import", farmer, [("dealers.xlsx", b"PK\x03\x04not-a-real-sheet", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")],
                      field="file", fields={"categoryId": "1"})
    check("farmer (R1): POST api/ExcelBulkUpload/import -> 403 before any file handling", st == 403 and isinstance(b, dict) and b.get("message") == FARMER_DENIED, (st, b))
    st, _, _ = call(base, "GET", "api/IfmsAutomation/status")
    check("anonymous (R1): IfmsAutomation/status without a token is not 403 (unchanged)", st != 403, st)
    st, _, _ = call(base, "GET", "api/IfmsAutomation/status", token=mdo)
    check("staff (R1): IfmsAutomation/status for MDO is not 403 (unchanged)", st != 403, st)
    st, b, _ = call(base, "POST", "api/Telemetry/batch", {"app": 1, "pageViews": [], "errors": []}, token=farmer)
    check("farmer (R1): the telemetry batch still answers (marked)", st == 202, (st, b))

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
    omitted = {k: v for k, v in body.items() if k not in ("n", "ph")}
    st, r, _ = call(base, "POST", "api/Farmer/fertilizer/recommend", omitted, token=tok)
    check("fertilizer (#27): omitted n and ph -> 400 naming both", st == 400 and {"n", "ph"} <= set((r.get("errors") or {}).keys()), (st, r))
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
    # Recheck B7 setup (before the 20 MB seed below): a photo on one of the farmer's replies.
    reply_id = sql(f'select min("Id") from "CommunityPostReplies" where "AuthorUserId" = {q(uid)} and not "IsDeleted"')
    st, r = multipart(base, f"api/Community/replies/{reply_id}/attachments", tok, [("reply.png", TINY_PNG, "image/png")])
    check("deletion setup (B7): a photo on the farmer's reply", st == 200, (st, r))
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

    # R2: a usage row the global AI ceiling counts (no AI key locally, so none is written by the API itself).
    sql(f'insert into "FarmerAiUsages" ("Kind","FarmerUserId","CreatedAtUtc") values (\'Chat\', {q(uid)}, now() at time zone \'utc\')')
    usage_today = 'select count(*) from "FarmerAiUsages" where "CreatedAtUtc" >= (now() at time zone \'utc\') - interval \'1 hour\''
    usages_before = int(sql(usage_today))
    otps_before = int(sql(f'select count(*) from "FarmerOtpChallenges" where "Phone" = {q(phone)}'))

    # Recheck B7: an attachment on one of the farmer's replies, a page view in the farmer's name, and the stored paths of
    # the farmer's attachments (checked on disk after the deletion).
    sql(f'insert into "AppPageViews" ("At","Route","App","UserId","UserName") values (now(), \'/smoke-final-b7\', 1, {q(uid)}, \'f60xxxxxx00\')')
    own_files = [os.path.join(ROOT, "SpicAPI", "Uploads", *path.split("/")) for path in sql(
        f'select string_agg("StoredPath", \'|\') from "CommunityPostAttachments" a where "StoredPath" not like \'Community/none/%\' and '
        f'(("ReplyId" is null and "PostId" in (select "Id" from "CommunityPosts" where "AuthorUserId" = {q(uid)})) or '
        f'"ReplyId" in (select "Id" from "CommunityPostReplies" where "AuthorUserId" = {q(uid)}))').split("|") if path]
    check("deletion setup (B7): the farmer's attachment files exist (post and reply)", len(own_files) >= 2 and all(os.path.isfile(f) for f in own_files), own_files)

    st, _, _ = call(base, "DELETE", "api/Farmer/profile", token=mdo)
    check("delete: staff calling DELETE api/Farmer/profile -> 403", st == 403, st)
    # The request log is written in 2 s batches: let the farmer's earlier requests reach the table first, so the check
    # below sees what the deletion anonymised (rows still queued at the moment of deletion keep the id: known limit).
    time.sleep(3)
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
        f'(select count(*) from "RefreshTokens" where "UserId" = {q(uid)})')
    check("delete: user, profile, chats, messages, AI usage link, leaf checks, fertilizer, yield and refresh tokens all gone",
          counts == ",".join(["0"] * 9), counts)
    # R2: the counters the global limits read are still there, without who it was.
    check("delete (R2): the global AI ceiling still counts the farmer's usage (row kept, tombstoned)",
          int(sql(usage_today)) == usages_before and int(sql('select count(*) from "FarmerAiUsages" where "FarmerUserId" = \'deleted-account\'')) >= 1,
          (usages_before, sql(usage_today)))
    otps_after = int(sql(f'select count(*) from "FarmerOtpChallenges" where "Phone" = {q(phone)}'))
    check("delete (R2): the number's OTP challenges are kept for the send caps (one-day purge removes them)",
          otps_before > 0 and otps_after == otps_before, (otps_before, otps_after))
    # The request log keeps its rows without who it was; only the DELETE call itself is logged after it ran.
    logs = sql(f'select count(*) from "AppRequestLogs" where "UserId" = {q(uid)}')
    check("delete: request log rows no longer name the farmer (only the DELETE call itself)", int(logs or 0) <= 1, logs)
    reqs = sql(f'select string_agg("Status", \',\' order by "Status") from "FarmerContactRequests" where "FarmerUserId" = {q(uid)}')
    check("delete: the open call-back is gone, handled ones stay closed for the officer", reqs and "Open" not in reqs and "Contacted" not in reqs and reqs.count("Closed") == 9, reqs)
    com = sql(f'select count(*) filter (where not "IsDeleted") || \',\' || count(*) filter (where "AuthorName" <> \'Deleted account\') from "CommunityPosts" where "AuthorUserId" = {q(uid)}')
    check("delete: Community discussions taken down, author name cleared", com == "0,0", com)
    rep = sql(f'select count(*) filter (where not "IsDeleted") from "CommunityPostReplies" where "AuthorUserId" = {q(uid)}')
    check("delete: Community replies taken down", rep == "0", rep)
    # Recheck B7: attachments (rows and files), page views and kept call-back messages.
    att = sql(f'select count(*) from "CommunityPostAttachments" where ("ReplyId" is null and "PostId" in (select "Id" from "CommunityPosts" where "AuthorUserId" = {q(uid)})) '
              f'or "ReplyId" in (select "Id" from "CommunityPostReplies" where "AuthorUserId" = {q(uid)})')
    check("delete (B7): the farmer's Community attachment rows are gone (posts and replies)", att == "0", att)
    check("delete (B7): the farmer's Community attachment files are deleted after the commit", not any(os.path.exists(f) for f in own_files), own_files)
    pv = sql(f'select count(*) from "AppPageViews" where "UserId" = {q(uid)}') + "," + sql('select count(*) from "AppPageViews" where "Route" = \'/smoke-final-b7\' and "UserId" is null and "UserName" is null')
    check("delete (B7): page views keep their rows but lose the user id and name", pv == "0,1", pv)
    sql('delete from "AppPageViews" where "Route" = \'/smoke-final-b7\'')
    msgs = sql(f'select count(*) filter (where "Message" <> \'\') from "FarmerContactRequests" where "FarmerUserId" = {q(uid)}')
    check("delete (B7): the kept call-backs lose their message text", msgs == "0", msgs)
    sas = sql(f'select count(*) || \',\' || count(*) filter (where "UserId" is null) from "SasFarmers" where "Mobile" = {q(phone)}')
    check("delete: the SAS farmer record stays, unlinked", sas == "1,1", sas)
    if leaves:
        check("delete: the leaf photo folder is deleted after the commit", not os.path.exists(photo_dir), photo_dir)
    st, _, _ = call(base, "POST", "api/Authentication/refresh", {"refreshToken": refresh})
    check("delete: the old refresh token no longer works (401)", st == 401, st)

    again = sign_in(base, phone)
    new_uid = sql(f'select "Id" from "AppUsers" where "UserName" = {q("f" + phone)}')
    check("delete: the same number signs up fresh as a new account", again.get("isNewUser") is True and new_uid and new_uid != uid, (again.get("isNewUser"), bool(new_uid)))
    # R3: a farmer gets the anonymous telemetry batch rate (60 a minute), counted per farmer.
    # The bucket is per clock minute, so a run that crosses a minute boundary starts a fresh bucket: post until the
    # first 429 (at most 130 tries) and require that it comes only after 60 accepted batches in its minute.
    statuses = []
    for i in range(130):
        st, _, _ = call(base, "POST", "api/Telemetry/batch",
                        {"app": 1, "pageViews": [{"route": "/smoke-final-r3", "at": "2026-11-28T00:00:00Z"}], "errors": []}, token=again["token"])
        statuses.append(st)
        if st == 429:
            break
    check("telemetry (R3): a farmer's batches are capped (429 after 60 in a minute)",
          statuses[-1] == 429 and len(statuses) >= 61 and all(x == 202 for x in statuses[:-1]), (len(statuses), statuses[-3:]))
    st, _, _ = call(base, "POST", "api/Telemetry/batch",
                    {"app": 1, "pageViews": [{"route": "/smoke-final-r3", "at": "2026-11-28T00:00:00Z"}], "errors": []}, token=mdo)
    check("telemetry (R3): staff batches are not limited (202)", st == 202, st)
    sql('delete from "AppPageViews" where "Route" = \'/smoke-final-r3\'')

    st, _, _ = call(base, "DELETE", "api/Farmer/profile", token=again["token"])
    check("delete: a farmer without a profile can delete too (204)", st == 204, st)
    check("delete: that account is gone as well", sql(f'select count(*) from "AppUsers" where "UserName" = {q("f" + phone)}') == "0")
    sql(f'delete from "SasFarmers" where "Mobile" = {q(phone)} and "Name" = \'[smoke-final] SAS\'')


# ---------------------------------------------------------------- Group R: recheck fixes (local DB only)

def file_status(base, path, token=None):
    """GET of a served file: the status code only (the body is binary)."""
    req = urllib.request.Request(base.rstrip("/") + "/" + path, method="GET")
    req.add_header("X-Forwarded-For", RUN_IP)
    if token:
        req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=40) as resp:
            resp.read()
            return resp.status
    except urllib.error.HTTPError as e:
        return e.code


def ins(query):
    """Runs an INSERT ... RETURNING "Id" and gives back the id (psql prints only the SELECT of the CTE)."""
    return sql(f'with x as ({query} returning "Id") select "Id" from x')


def group_r(base):
    print("Group R: recheck fixes (B1 shared SAS collection, B2 file subtrees)")
    phone = throwaway_phone()
    login = sign_in(base, phone)
    tok = login["token"]
    uid = sql(f'select "Id" from "AppUsers" where "UserName" = {q("f" + phone)}')
    mdo = staff_token(base, "qa.mdo", qa_password())
    other_phone = throwaway_phone()

    # ---- B1: one collection with the caller's sample and another farmer's sample (the other one first).
    mine = ins(f'insert into "SasFarmers" ("Name","Mobile","UserId","Village","SurveyNumber","CreatedAt","UpdatedAt","IsDeleted") '
               f'values (\'[smoke-recheck] A\', {q(phone)}, {q(uid)}, \'A village\', \'A-1\', now(), now(), false)')
    other = ins(f'insert into "SasFarmers" ("Name","Mobile","Village","SurveyNumber","Latitude","Longitude","CreatedAt","UpdatedAt","IsDeleted") '
                f'values (\'[smoke-recheck] B\', {q(other_phone)}, \'B village\', \'B-7\', 11.1, 78.1, now(), now(), false)')
    col = ins('insert into "SampleCollections" ("Code","CollectionDate","CollectedByUserId","CollectedByName","PaymentType","Status","TotalAmount","CreatedAt","UpdatedAt","IsDeleted") '
              "values ('[smoke-recheck] SMP', now(), 'smoke', 'Smoke MDO', 0, 9, 0, now(), now(), false)")
    item_b = ins(f'insert into "SampleItems" ("CollectionId","Code","FarmerId","SampleType","NoOfTests","Amount","CreatedAt","IsDeleted","AnalysisStatus") '
                 f"values ({col}, '[smoke-recheck] SMP-1', {other}, 1, 1, 0, now(), false, 2)")
    item_a = ins(f'insert into "SampleItems" ("CollectionId","Code","FarmerId","SampleType","NoOfTests","Amount","CreatedAt","IsDeleted","AnalysisStatus") '
                 f"values ({col}, '[smoke-recheck] SMP-2', {mine}, 0, 1, 0, now(), false, 2)")
    for item, value in ((item_b, "8.9"), (item_a, "6.5")):
        sql(f'insert into "SampleLabResults" ("SampleItemId","Parameter","EnteredValue","Status","SortOrder","EnteredAt") values ({item}, \'pH\', {q(value)}, 0, 0, now())')
    try:
        st, d, _ = call(base, "GET", f"api/Sas/collections/{col}", token=tok)
        ids = [i.get("id") for i in d.get("items", [])] if st == 200 else None
        check("B1: the farmer opens the shared collection and sees only their own sample", st == 200 and ids == [int(item_a)], (st, ids))
        check("B1: no other farmer's mobile or village anywhere in the answer",
              st == 200 and other_phone not in json.dumps(d) and "B village" not in json.dumps(d), st)
        check("B1: the summary farmer name and count are the caller's own",
              st == 200 and d.get("farmerName") == "[smoke-recheck] A" and d.get("sampleCount") == 1,
              (d.get("farmerName"), d.get("sampleCount")) if isinstance(d, dict) else d)
        st, _, _ = call(base, "GET", f"api/Sas/samples/{item_b}/results", token=tok)
        check("B1: another farmer's results in the same collection -> 404", st == 404, st)
        st, r, _ = call(base, "GET", f"api/Sas/samples/{item_a}/results", token=tok)
        check("B1: the farmer's own results still answer", st == 200 and isinstance(r, list) and len(r) == 1, (st, r))
        st, page, _ = call(base, "GET", "api/Sas/collections?q=" + other_phone, token=tok)
        check("B1: a farmer cannot find the collection by the other farmer's mobile",
              st == 200 and page.get("total") == 0, (st, page.get("total") if isinstance(page, dict) else page))
        st, d, _ = call(base, "GET", f"api/Sas/collections/{col}", token=mdo)
        check("B1: staff still see both samples", st == 200 and len(d.get("items", [])) == 2,
              (st, len(d.get("items", [])) if isinstance(d, dict) else d))
    finally:
        sql(f'delete from "SampleLabResults" where "SampleItemId" in ({item_a}, {item_b})')
        sql(f'delete from "SampleItems" where "CollectionId" = {col}')
        sql(f'delete from "SampleCollections" where "Id" = {col}')
        sql(f'delete from "SasFarmers" where "Id" in ({mine}, {other})')

    # ---- B2: the shared file routes give a Farmer token only the Community / Library subtrees.
    uploads = os.path.join(ROOT, "SpicAPI", "Uploads")
    probes = {
        "DealerRegistration/999999/PAN_20261128000000.pdf": b"%PDF-1.4 smoke",
        "GuestHouseBookingDocuments/999999/idproof_1.jpg": b"\xff\xd8\xff smoke",
        "Library/999999/smoke-cover.png": TINY_PNG,
    }
    for rel, data in probes.items():
        full = os.path.join(uploads, *rel.split("/"))
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "wb") as f:
            f.write(data)
    try:
        for route in ("Community", "Library"):
            st = file_status(base, f"api/{route}/file/DealerRegistration/999999/PAN_20261128000000.pdf", token=tok)
            check(f"B2: farmer {route}/file on dealer KYC -> 404", st == 404, st)
            st = file_status(base, f"api/{route}/file/GuestHouseBookingDocuments/999999/idproof_1.jpg", token=tok)
            check(f"B2: farmer {route}/file on a guest-house ID proof -> 404", st == 404, st)
            st = file_status(base, f"api/{route}/file/DealerRegistration/999999/PAN_20261128000000.pdf", token=mdo)
            check(f"B2: staff {route}/file unchanged (pre-existing, owner item) -> 200", st == 200, st)
        st = file_status(base, "api/Library/file/Library/999999/smoke-cover.png", token=tok)
        check("B2: farmer still gets a library file -> 200", st == 200, st)
        st = file_status(base, "api/Community/file/Library/999999/smoke-cover.png", token=tok)
        check("B2: farmer Community/file on a library file -> 404 (Community subtree only)", st == 404, st)
        st, p, _ = call(base, "POST", "api/Community/discussions", {"title": "[smoke-recheck] photo", "body": "Leaf photo."}, token=tok)
        post_id = p.get("id") if st == 200 and isinstance(p, dict) else 0
        multipart(base, f"api/Community/discussions/{post_id}/attachments", tok, [("leaf.png", TINY_PNG, "image/png")])
        stored = sql(f'select coalesce(min("StoredPath"), \'\') from "CommunityPostAttachments" where "PostId" = {post_id or 0}')
        st = file_status(base, f"api/Community/file/{stored}", token=tok)
        check("B2: farmer still gets a Community attachment -> 200", bool(stored) and st == 200, (stored, st))
        st = file_status(base, f"api/Community/file/{stored}?access_token={tok.removeprefix('Bearer ')}")
        check("B2: the same through ?access_token= (how <img> loads it) -> 200", bool(stored) and st == 200, st)
    finally:
        for rel in probes:
            full = os.path.join(uploads, *rel.split("/"))
            if os.path.exists(full):
                os.remove(full)
            try:
                os.rmdir(os.path.dirname(full))
            except OSError:
                pass
        st, _, _ = call(base, "DELETE", "api/Farmer/profile", token=tok)
        check("recheck: the throw-away farmer is deleted again (204)", st == 204, st)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--group", default="A,C,R")
    ap.add_argument("--leaves", default="", help="folder written by make_leaves.py (adds a leaf check to the deletion test)")
    args = ap.parse_args()
    if (urlparse(args.base).hostname or "") not in ("localhost", "127.0.0.1"):
        sys.exit("Refusing a non-local API: this smoke test runs against the LOCAL API only.")
    groups = set(args.group.upper().split(","))
    if "A" in groups:
        group_a(args.base, args.phone)
    if "C" in groups:
        group_c(args.base, args.leaves)
    if "R" in groups:
        group_r(args.base)
    print()
    print("All checks passed." if not failures else f"{len(failures)} check(s) FAILED: {failures}")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
