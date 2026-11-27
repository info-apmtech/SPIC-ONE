"""
Farmer portal Task 1 smoke test: OTP request -> verify -> profile, against a LOCAL API only.

    python tools/farmer-portal-check/smoke_task1.py [--base http://localhost:5120] [--staff-phone 98xxxxxxxx]

Needs the API running in the Development environment with Sms:Provider = Log (the default), so
otp/request returns devCode, and the local spicone_dev database (never the live server):

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120   (ASPNETCORE_ENVIRONMENT=Development)

Each run signs up a NEW random test farmer (6xxxxxxxxx) in the local database.
--staff-phone: a mobile that belongs to a staff/dealer account in the local DB, to check the 409.
"""
import argparse
import json
import random
import sys
import urllib.error
import urllib.parse
import urllib.request

failures = []


def call(base, method, path, body=None, token=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request(base.rstrip("/") + "/" + path.lstrip("/"), data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            raw = resp.read().decode()
            return resp.status, (json.loads(raw) if raw.strip().startswith(("{", "[", '"')) else raw)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw


def check(name, ok, detail=""):
    print(("  PASS  " if ok else "  FAIL  ") + name + ("" if ok else "  -- " + str(detail)))
    if not ok:
        failures.append(name)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--staff-phone")
    args = ap.parse_args()

    host = urllib.parse.urlparse(args.base).hostname
    if host not in ("localhost", "127.0.0.1", "::1"):
        sys.exit("Refusing to run against a non-local API: " + args.base)

    base = args.base
    phone = "6" + "".join(random.choice("0123456789") for _ in range(9))
    print(f"Farmer portal Task 1 smoke test on {base} (test phone {phone[:2]}xxxxxx{phone[-2:]})")

    s, b = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": "12345"})
    check("bad phone -> 400 with a message", s == 400 and "10-digit" in b.get("message", ""), (s, b))

    s, b = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": "+91 " + phone[:5] + " " + phone[5:]})
    check("request -> 200 (phone with +91 and spaces)", s == 200 and b.get("success") is True, (s, b))
    check("request -> expires 300 / resend 30", b.get("expiresInSeconds") == 300 and b.get("resendInSeconds") == 30, b)
    code = b.get("devCode")
    check("request -> devCode in Development", isinstance(code, str) and len(code) == 6, b)
    if not code:
        sys.exit("No devCode: is the API in Development with Sms:Provider = Log?")

    s, b = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": phone})
    check("immediate resend -> 429 with retryAfterSeconds", s == 429 and 0 < (b.get("retryAfterSeconds") or 0) <= 30, (s, b))

    wrong = "000000" if code != "000000" else "111111"
    s, b = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": phone, "code": wrong})
    check("wrong code -> 400, 4 tries left", s == 400 and "4 tries left" in b.get("message", ""), (s, b))

    s, b = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": phone, "code": "12"})
    check("short code -> 400", s == 400, (s, b))

    s, b = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": "0" + phone, "code": code})
    check("right code -> 200 (phone with leading 0)", s == 200, (s, b))
    token = b.get("token", "")
    refresh = b.get("refreshToken")
    check("verify -> Bearer token + refresh token", token.startswith("Bearer ") and bool(refresh), b)
    check("verify -> new user, profile incomplete", b.get("isNewUser") is True and b.get("profileComplete") is False, b)
    user = b.get("user") or {}
    check("verify -> Farmer role, f+phone user name", user.get("role") == "Farmer" and user.get("userName") == "f" + phone, user)

    s, b = call(base, "POST", "api/FarmerAuth/otp/verify", {"phone": phone, "code": code})
    check("same code again -> 400 (single use)", s == 400, (s, b))

    s, b = call(base, "GET", "api/Farmer/profile", token=token)
    check("GET profile before onboarding -> 404 profileComplete false", s == 404 and b.get("profileComplete") is False, (s, b))
    check("GET profile 404 carries the phone", b.get("phone") == phone, b)

    s, b = call(base, "GET", "api/Farmer/profile")
    check("GET profile without a token -> 401", s == 401, s)

    s, states = call(base, "GET", "api/State", token=token)
    state = next((x for x in states if isinstance(x, dict)), None) if s == 200 else None
    districts, other_district = [], None
    if state:
        s, districts = call(base, "GET", f"api/District/byState/{state['id']}", token=token)
        for other in states[1:]:
            s2, od = call(base, "GET", f"api/District/byState/{other['id']}", token=token)
            if s2 == 200 and od:
                other_district = od[0]
                break
    if not state or not districts:
        print("  SKIP  no states/districts in the local DB; profile PUT checks skipped")
    else:
        district = districts[0]
        profile = {
            "name": "Smoke Test Farmer", "email": "", "stateId": state["id"], "districtId": district["id"],
            "village": "Test Village", "taluk": "Test Taluk", "pinCode": "62850",
            "primaryCrops": ["Paddy", "Banana", "paddy"], "landAcres": 2.5, "irrigationType": 2, "preferredLanguage": "ta",
            "latitude": 9.17, "longitude": 77.87
        }
        s, b = call(base, "PUT", "api/Farmer/profile", profile, token)
        check("PUT bad pincode -> 400 errors.pinCode", s == 400 and "pinCode" in (b.get("errors") or {}), (s, b))

        if other_district:
            bad = dict(profile, pinCode="628501", districtId=other_district["id"])
            s, b = call(base, "PUT", "api/Farmer/profile", bad, token)
            check("PUT district of another state -> 400 errors.districtId", s == 400 and "districtId" in (b.get("errors") or {}), (s, b))

        profile["pinCode"] = "628501"
        s, b = call(base, "PUT", "api/Farmer/profile", profile, token)
        check("PUT valid -> 200 profileComplete", s == 200 and b.get("profileComplete") is True, (s, b))
        check("PUT -> crops de-duplicated, phone from the account", b.get("primaryCrops") == ["Paddy", "Banana"] and b.get("phone") == phone, b)
        check("PUT -> state / district names resolved", bool(b.get("stateName")) and bool(b.get("districtName")), b)

        s, b = call(base, "GET", "api/Farmer/profile", token=token)
        check("GET profile after onboarding -> 200", s == 200 and b.get("name") == "Smoke Test Farmer" and b.get("irrigationType") == 2, (s, b))

    # The refresh path was moved into TokenIssuer: the farmer's refresh token must work there too.
    s, b = call(base, "POST", "api/Authentication/refresh", {"refreshToken": refresh})
    check("refresh with the OTP refresh token -> 200 new pair", s == 200 and b.get("token", "").startswith("Bearer ") and b.get("refreshToken") not in (None, refresh), (s, b))
    s, b = call(base, "POST", "api/Authentication/refresh", {"refreshToken": refresh})
    check("refresh token is single use -> 401", s == 401, s)

    # Normal login is unchanged: wrong password still 401 with the same text.
    s, b = call(base, "POST", "api/Authentication/login", {"userName": "no-such-user-" + phone, "password": "x"})
    check("normal login wrong user -> 401 'Invalid username or password'", s == 401 and "Invalid username or password" in str(b), (s, b))

    if args.staff_phone:
        s, b = call(base, "POST", "api/FarmerAuth/otp/request", {"phone": args.staff_phone})
        check("staff number -> 409 normal-login message", s == 409 and "normal login" in b.get("message", ""), (s, b))

    print()
    print(f"{len(failures)} failed" if failures else "All smoke checks passed.")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
