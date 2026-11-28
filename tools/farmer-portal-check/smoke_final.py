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
import os
import sys
from urllib.parse import urlparse

sys.path.insert(0, os.path.dirname(__file__))
from smoke_task3 import call, check, failures, farmer_token, staff_token, qa_password  # noqa: E402

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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--group", default="A")
    args = ap.parse_args()
    if (urlparse(args.base).hostname or "") not in ("localhost", "127.0.0.1"):
        sys.exit("Refusing a non-local API: this smoke test runs against the LOCAL API only.")
    groups = set(args.group.upper().split(","))
    if "A" in groups:
        group_a(args.base, args.phone)
    print()
    print("All checks passed." if not failures else f"{len(failures)} check(s) FAILED: {failures}")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
