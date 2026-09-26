# SAS Lab API check

`lab_api_check.py` walks the whole SAS Lab portal flow (`api/Lab/...`, `SpicAPI/Controllers/LabController.cs`)
against a running **local** API and asserts every KPI, status, analysis day, activity kind, document
upload / delete and export. It refuses any base URL that is not localhost.

## What it does

1. Signs in as the QA accounts (`qa.mdo`, `qa.admin`, `qa.labcoord`, `qa.analyst`, `qa.analyst2`,
   `qa.farmer`, `qa.dealer`, `qa.finance`) and checks `api/Lab/me`, the 403s for non-lab roles,
   languages and the analyst list.
2. Builds fresh v1 data as `qa.mdo`: four farmers, a free collection (soil + water), a paid one
   (soil + soil & water; payment recorded, approved by `qa.admin`) and a second free one, then three
   consignments. Every name, remark and tracking number starts with `QA-LAB`.
3. As `qa.labcoord`: receives the consignments, creates batch one from two consignments assigned to
   `qa.analyst` (High) and batch two unassigned, then assigns it to `qa.analyst2`.
4. As `qa.analyst`: preview (auto result, recommendations, crop note), draft, submit for every
   sample (Texture as text, Organic Matter derived, a SoilAndWater sample), reopen and resubmit.
5. Documents (upload, served by `api/Sas/file`, filters, soft delete), exports via `?access_token=`,
   completion of batch one, and the v1 collection / consignment records (TestInProgress, Completed,
   results, timeline).
6. Reports agree with the lab pages: display ids (`SAS-SOIL-001`) equal between `batches/{id}/samples`,
   the report list and the report detail; the report's overall status, recommendations and crop note
   equal the entry page's result (one result engine); `reports/stats.BatchGroups` counts batches with
   reports; completing a completed batch returns 409 "Batch is already completed and its reports
   exist", and after its reports are deleted in the local database (skip with `--no-db`) it
   regenerates them and returns 200.
7. Payments: the admin's `VerifiedAmount` and Finance's `FinanceVerifiedAmount` / `FinanceReceivedDate`
   stay apart; a short verify is an Amount Mismatch, the mismatch route a Failed payment (no Finance
   amount); `ApprovalPending` and the `approvalPending` filter count admin-approved payments not yet
   processed by Finance.
8. `qa.analyst2` saves a draft (Continue Test Entry) and batch two is back-dated 7 days in the local
   database to check Analysis Days and Delayed (skip with `--no-db`).

KPIs are asserted as deltas against a baseline taken at the start, so the script can be re-run; each
run adds a new set of QA-LAB rows and leaves them in place for the client workstreams.

## Run

```
# API on the local database (from the repository root)
set ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234
dotnet run --project SpicAPI --urls http://localhost:5121

# in another shell
pip install requests psycopg2-binary
python tools/lab-api-check/lab_api_check.py --base http://localhost:5121
```

Options: `--password` (QA password, default `QaPass@2026#`), `--db` (psycopg2 DSN for the delayed
step, default the local `spicone_dev`), `--no-db`. Exit code 0 when every check passes; failures are
listed at the end.
