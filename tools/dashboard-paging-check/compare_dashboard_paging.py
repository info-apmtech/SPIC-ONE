#!/usr/bin/env python3
"""Prove the server-side Dashboard paging returns exactly what the old client-side
pipeline produced.

/Dashboard used to download every submitted dealer registration from
``GET api/DealerRegistration/submitted`` and then filter, sort, count and page in the
browser (``ApplyRoleFilter``, ``ApplyFilters``, ``GetApprovalStatus``, ``IsPendingForMe``
and the KPI counters in ``Shared/Pages/Dashboard.razor``). That work now happens in SQL
behind two endpoints:

    GET api/DealerRegistration/dashboard/page?page=&pageSize=&search=&company=
        &registrationType=&status=&regionId=&workflowStatus=&sort=
    GET api/DealerRegistration/dashboard/summary?<same filter query>

This script re-implements the OLD client pipeline in Python, feeds it the full
``submitted`` list, and asserts that:

  * every summary number equals the KPI tile / status counter the page used to compute, and
  * the first page (and a later page) of ``dashboard/page`` is the same ordered slice
    the page used to render.

It does this for several users (whose role decides both the location scope and the
"pending for me first" ordering) and several filter combinations.

LOCAL USE ONLY. Point ``--base`` at a locally running API backed by the local
``spicone_dev`` database. Never run it against the live API.

Usage::

    python tools/dashboard-paging-check/compare_dashboard_paging.py \
        --base http://localhost:5122 --users qa.admin qa.rm qa.smm

The QA accounts live only in the local database; the password defaults to the local QA
password and can be overridden with --password or SPICONE_TEST_PASS.

Test data
---------
The checks only mean something when the local database holds enough submitted
registrations to fill several pages. Seed them directly with SQL and give them a
recognisable firm-name prefix so they can be deleted afterwards. A useful spread is:

  * a few hundred rows across several StateId / Region / HQ combinations, so the
    role-scoped users (qa.rm by region, qa.smm by state, qa.mdo by HQ) each see a
    strict subset;
  * every workflow state — IsSubmittedForReview null/false (Draft), true with
    RMApproved null (In RM), true/true/null (In SMM), true/true/true (In AVP and
    Approved) and the three rejection shapes (RMApproved false, SMApproved false,
    AVPApproved false);
  * a mix of Status 0/1/2, InSpic/InGreenStar and IsNewDealerRegistration;
  * a handful of rows that qualify as "submitted" through the Terminated or
    Department branches instead of PinCode (PinCode = '');
  * DISTINCT UpdatedAt and FirmName values, otherwise sort ties make the expected
    page order ambiguous and the comparison reports false differences.

The qa.rm and qa.smm accounts also need an "Employeelogins" row, since that is where
the login endpoint reads StateId / RegionId / HeadquartersId / ZoneId for the JWT.
"""

from __future__ import annotations

import argparse
import base64
import json
import os
import sys
from datetime import datetime
from typing import Any

import requests

DEFAULT_PASSWORD = os.environ.get("SPICONE_TEST_PASS", "QaPass@2026#")

# ── AppRole (SPIC.Core/Entities/Userinfo.cs) ────────────────────────────────────
STATE_ROLES = {"SMD", "SMM"}
REGION_ROLES = {"RM", "RMD"}
HQ_ROLES = {"MO", "MDO", "JMDO"}
ADMIN_ROLES = {"Admin", "CorporateAdmin", "Director", "AVP"}


# ── LoginState (Shared/Services/LoginState.cs) ──────────────────────────────────
class LoginState:
    def __init__(self, token: str) -> None:
        payload = token.split(".")[1]
        payload += "=" * (-len(payload) % 4)
        claims = json.loads(base64.urlsafe_b64decode(payload))
        self.claims = claims
        self.role = claims.get("role") or claims.get(
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
        self.state_id = int(claims.get("spic:state_id") or 0)
        self.region_id = int(claims.get("spic:region_id") or 0)
        self.hq_id = int(claims.get("spic:hq_id") or 0)
        self.assigned_state_ids = _csv_ids(claims.get("spic:assigned_state_ids"))
        self.user_id = (
            claims.get("sub")
            or claims.get("nameid")
            or claims.get("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            or claims.get("spic:user_id")
        )

    @property
    def is_special_admin_with_assignments(self) -> bool:
        return self.role == "SpecialAdmin" and len(self.assigned_state_ids) > 0

    @property
    def is_state_role(self) -> bool:
        return self.role in STATE_ROLES

    @property
    def is_region_role(self) -> bool:
        return self.role in REGION_ROLES

    @property
    def is_hq_role(self) -> bool:
        return self.role in HQ_ROLES

    @property
    def is_admin(self) -> bool:
        return self.role in ADMIN_ROLES


def _csv_ids(value: str | None) -> list[int]:
    if not value:
        return []
    out: list[int] = []
    for part in value.split(","):
        part = part.strip()
        if part.isdigit() and int(part) > 0 and int(part) not in out:
            out.append(int(part))
    return out


# ── The old client pipeline ─────────────────────────────────────────────────────
def apply_role_filter(rows: list[dict], me: LoginState) -> list[dict]:
    """Dashboard.razor ApplyRoleFilter."""
    if me.is_special_admin_with_assignments:
        return [d for d in rows if d["stateId"] in me.assigned_state_ids]
    if me.is_state_role:
        return [d for d in rows if d["stateId"] == me.state_id]
    if me.is_region_role:
        return [d for d in rows if d["region"] == me.region_id]
    if me.is_hq_role:
        return [d for d in rows if d["hq"] == me.hq_id]
    return list(rows)


def approval_status(d: dict) -> str:
    """Dashboard.razor GetApprovalStatus."""
    if d.get("isSubmittedForReview") is not True:
        return "Draft"
    if d.get("rmApproved") is False:
        return "Rejected by RM"
    if d.get("smApproved") is False:
        return "Rejected by SMM"
    if d.get("avpApproved") is False:
        return "Rejected by AVP"
    if d.get("avpApproved") is True:
        return "Approved"
    if d.get("rmApproved") is None:
        return "In RM"
    if d.get("rmApproved") is True and d.get("smApproved") is None:
        return "In SMM"
    if (d.get("rmApproved") is True and d.get("smApproved") is True
            and d.get("avpApproved") is None):
        return "In AVP"
    return "Submitted"


def owner_level(d: dict) -> str:
    """Dashboard.razor GetCurrentOwnerLevel."""
    if d.get("isSubmittedForReview") is not True:
        return "MO"
    if d.get("avpApproved") is True:
        return "None"
    if d.get("rmApproved") is False:
        return "MO"
    if d.get("smApproved") is False:
        return "RM"
    if d.get("avpApproved") is False:
        return "SMM"
    if d.get("rmApproved") is None:
        return "RM"
    if d.get("rmApproved") is True and d.get("smApproved") is None:
        return "SMM"
    if (d.get("rmApproved") is True and d.get("smApproved") is True
            and d.get("avpApproved") is None):
        return "AVP"
    return "None"


def is_creator_level_user(d: dict, me: LoginState) -> bool:
    if me.user_id and me.user_id == d.get("createdBy"):
        return True
    return me.role in HQ_ROLES


def is_pending_for_me(d: dict, me: LoginState) -> bool:
    """Dashboard.razor IsPendingForMe."""
    level = owner_level(d)
    if level == "MO":
        return is_creator_level_user(d, me)
    if level == "RM":
        return me.is_region_role
    if level == "SMM":
        return me.is_state_role
    if level == "AVP":
        return me.is_admin
    return False


def parse_dt(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def apply_filters(rows: list[dict], f: dict, me: LoginState) -> list[dict]:
    """Dashboard.razor ApplyFilters (filter + sort, no paging)."""
    q = list(rows)

    search = (f.get("search") or "").strip().lower()
    if search:
        q = [d for d in q
             if (d.get("dealerCode") or "").lower().find(search) >= 0
             or (d.get("firmName") or "").lower().find(search) >= 0]

    company = f.get("company") or ""
    if company == "SPIC":
        q = [d for d in q if d["inSpic"]]
    elif company == "GreenStar":
        q = [d for d in q if d["inGreenStar"]]
    elif company == "Both":
        q = [d for d in q if d["inSpic"] and d["inGreenStar"]]

    reg_type = f.get("registrationType") or ""
    if reg_type == "new":
        q = [d for d in q if d["isNewDealerRegistration"]]
    elif reg_type == "existing":
        q = [d for d in q if not d["isNewDealerRegistration"]]

    status = int(f.get("status", -1))
    if status >= 0:
        q = [d for d in q if d["status"] == status]

    region_id = int(f.get("regionId", 0))
    if region_id > 0:
        q = [d for d in q if d["region"] == region_id]

    wf = f.get("workflowStatus") or "All"
    if wf != "All":
        wanted = {
            "Pending Draft": lambda d: approval_status(d) == "Draft",
            "Pending RM": lambda d: approval_status(d) == "In RM",
            "Pending SMM": lambda d: approval_status(d) == "In SMM",
            "Pending AVP": lambda d: approval_status(d) == "In AVP",
            "Approved": lambda d: approval_status(d) == "Approved",
            "Rejected/Returned": lambda d: approval_status(d).startswith("Rejected"),
        }.get(wf)
        if wanted:
            q = [d for d in q if wanted(d)]

    # Python's sort is stable, so applying the keys back to front reproduces
    # OrderByDescending(pending).ThenBy(...).ThenBy(Id).
    sort = f.get("sort") or "newest"
    q.sort(key=lambda d: d["id"])
    if sort in ("oldest",):
        q.sort(key=lambda d: parse_dt(d["updatedAt"]))
    elif sort == "name":
        # The database collation (English_United States.1252) and .NET's culture
        # compare both order case-insensitively at the primary level, so the
        # reference key has to as well — a plain ordinal sort would not match either.
        q.sort(key=lambda d: (d.get("firmName") or "").casefold())
    else:
        q.sort(key=lambda d: parse_dt(d["updatedAt"]), reverse=True)
    q.sort(key=lambda d: not is_pending_for_me(d, me))  # OrderByDescending(pending)
    return q


def expected_summary(rows: list[dict]) -> dict:
    """The KPI tiles and status counters, straight off Dashboard.razor."""
    def st(d):
        return approval_status(d)

    return {
        "totalRegistrations": len(rows),
        "draft": sum(1 for d in rows if d["status"] != 2 and st(d) == "Draft"),
        "inRM": sum(1 for d in rows if st(d) in ("In RM", "Rejected by SMM")),
        "inSMM": sum(1 for d in rows if st(d) in ("In SMM", "Rejected by AVP")),
        "inAVP": sum(1 for d in rows if st(d) == "In AVP"),
        "approved": sum(1 for d in rows if d.get("avpApproved") is True),
        "rejected": sum(1 for d in rows
                        if d.get("rmApproved") is False
                        or d.get("smApproved") is False
                        or d.get("avpApproved") is False),
        "rejectedByRM": sum(1 for d in rows if d.get("rmApproved") is False),
        "rejectedBySMM": sum(1 for d in rows if d.get("smApproved") is False),
        "rejectedByAVP": sum(1 for d in rows if d.get("avpApproved") is False),
        "statusActive": sum(1 for d in rows if d["status"] == 0),
        "statusInactive": sum(1 for d in rows if d["status"] == 1),
        "statusTerminated": sum(1 for d in rows if d["status"] == 2),
    }


# ── HTTP ────────────────────────────────────────────────────────────────────────
def login(base: str, user: str, password: str) -> str:
    r = requests.post(f"{base}/api/Authentication/login",
                      json={"userName": user, "password": password}, timeout=60)
    r.raise_for_status()
    token = r.json()["token"]
    return token[len("Bearer "):] if token.lower().startswith("bearer ") else token


def get(base: str, token: str, path: str, params: dict | None = None) -> Any:
    r = requests.get(f"{base}{path}", params=params,
                     headers={"Authorization": f"Bearer {token}"}, timeout=300)
    r.raise_for_status()
    return r.json()


FILTER_SETS = [
    ("default", {}),
    ("greenstar + active + oldest",
     {"company": "GreenStar", "status": 0, "sort": "oldest"}),
    ("new dealers + pending RM + name",
     {"registrationType": "new", "workflowStatus": "Pending RM", "sort": "name"}),
    ("search 'qa-page a' + both companies",
     {"search": "QA-PAGE A", "company": "Both"}),
    ("rejected/returned, region 1",
     {"workflowStatus": "Rejected/Returned", "regionId": 1}),
    ("existing dealers + approved",
     {"registrationType": "existing", "workflowStatus": "Approved"}),
]

PAGE_SIZE = 30


def run_user(base: str, user: str, password: str, verbose: bool) -> tuple[int, int]:
    token = login(base, user, password)
    me = LoginState(token)
    full = get(base, token, "/api/DealerRegistration/submitted")
    scoped = apply_role_filter(full, me)

    print(f"\n=== {user}  (role={me.role} state={me.state_id} region={me.region_id} "
          f"hq={me.hq_id})")
    print(f"    submitted rows: {len(full)}  after client ApplyRoleFilter: {len(scoped)}")

    passed = failed = 0
    for label, f in FILTER_SETS:
        expected_rows = apply_filters(scoped, f, me)
        exp = expected_summary(expected_rows)

        params = {
            "page": 1, "pageSize": PAGE_SIZE,
            "search": f.get("search", ""),
            "company": f.get("company", ""),
            "registrationType": f.get("registrationType", ""),
            "status": f.get("status", -1),
            "regionId": f.get("regionId", 0),
            "workflowStatus": f.get("workflowStatus", "All"),
            "sort": f.get("sort", "newest"),
        }

        actual = get(base, token, "/api/DealerRegistration/dashboard/summary", params)
        problems = [f"{k}: client={v} server={actual.get(k)}"
                    for k, v in exp.items() if actual.get(k) != v]

        page1 = get(base, token, "/api/DealerRegistration/dashboard/page", params)
        if page1["total"] != len(expected_rows):
            problems.append(f"total: client={len(expected_rows)} server={page1['total']}")
        exp_ids = [d["id"] for d in expected_rows[:PAGE_SIZE]]
        got_ids = [d["id"] for d in page1["items"]]
        if exp_ids != got_ids:
            problems.append(f"page 1 order differs\n        client={exp_ids}\n        server={got_ids}")

        # a later page too, so paging boundaries are covered
        if len(expected_rows) > PAGE_SIZE:
            params2 = dict(params, page=2)
            page2 = get(base, token, "/api/DealerRegistration/dashboard/page", params2)
            exp_ids2 = [d["id"] for d in expected_rows[PAGE_SIZE:2 * PAGE_SIZE]]
            got_ids2 = [d["id"] for d in page2["items"]]
            if exp_ids2 != got_ids2:
                problems.append(f"page 2 order differs\n        client={exp_ids2}\n        server={got_ids2}")

        if problems:
            failed += 1
            print(f"  FAIL  {label}  (client rows={len(expected_rows)})")
            for p in problems:
                print(f"        {p}")
        else:
            passed += 1
            print(f"  ok    {label}  (rows={len(expected_rows)}, "
                  f"total={page1['total']}, page1={len(got_ids)})")
            if verbose:
                print(f"        summary={exp}")

    return passed, failed


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base", default="http://localhost:5122",
                    help="Local API base URL (default http://localhost:5122)")
    ap.add_argument("--users", nargs="+", default=["qa.admin", "qa.rm", "qa.smm"],
                    help="QA user names to check")
    ap.add_argument("--password", default=DEFAULT_PASSWORD)
    ap.add_argument("-v", "--verbose", action="store_true")
    args = ap.parse_args()

    if "localhost" not in args.base and "127.0.0.1" not in args.base:
        print("Refusing to run against a non-local API.", file=sys.stderr)
        return 2

    total_pass = total_fail = 0
    for user in args.users:
        p, f = run_user(args.base, user, args.password, args.verbose)
        total_pass += p
        total_fail += f

    print(f"\n{total_pass} passed, {total_fail} failed")
    return 1 if total_fail else 0


if __name__ == "__main__":
    raise SystemExit(main())
