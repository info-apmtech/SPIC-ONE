# Lab report check

`lab_report_check.py` exercises the SAS Lab report routes (`api/Lab/reports*`,
`api/Lab/batches/{id}/report*`) for one batch against a **local** API and prints PASS / FAIL per
check. Standard library only (Python 3.9+). Never point it at the live API: it downloads every
report (which counts downloads) and marks one report Printed.

## What it checks

1. Generates the reports of the batch by calling the generation route twice (default
   `PATCH api/Lab/batches/{id}/status?status=Completed`, the LabController route that calls
   `ILabReportService.GenerateForBatchAsync`) and asserts the second call creates nothing and
   answers 409 "Batch is already completed and its reports exist".
   Also: `reports/stats.BatchGroups` = batches with at least one report; the report list's and the
   detail's `SampleId` (the PDF's Lab Number) equal `batches/{id}/samples`; no recommendation line
   repeats its parameter ("Sodium is high ... because sodium is ..."); PDF file names carry the
   requested language, or `-en` together with `X-Report-Language-Fallback`; the CORS response
   exposes that header.
2. One report per sample, two for Soil & Water samples; no pending reports in the batch summary.
3. Codes `RPT-SAS-yyyy-nnn` (unique), batch code `REP-SAS-yyyy-nnn`, `FinancialYearStart`
   = April-March year of `GeneratedAt`.
4. `reports/stats`, `reports/batches`, `batches/{id}/report`, `reports/{id}`.
5. For every report: PDF in each language the detail offers (`application/pdf`, `%PDF`; a
   `X-Report-Language-Fallback` header is printed when no font covers the language) and XLSX
   (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`); the download count
   goes up and the status becomes Downloaded.
6. `?access_token=` on the PDF route, batch download as one PDF and as a zip named by report code.
7. `POST reports/{id}/printed` sets Printed and a later download keeps it.
8. With `--farmer-user`: the farmer sees only their own reports, gets 403 on another farmer's
   report and on batch routes, can download Tamil, is refused Telugu (400) and cannot mark printed.

## Usage

```powershell
$env:LAB_CHECK_PASSWORD = '<local QA password>'
python tools/lab-report-check/lab_report_check.py --base http://localhost:5122 --batch-id 3 `
    --user qa.labcoord --farmer-user qa.farmer --out $env:TEMP\lab-reports
```

Options: `--generate-route` / `--generate-method` to use another generation route,
`--no-generate` when the reports already exist, `--out` to keep the downloaded files.
Exit code 0 when every check passes.

## Fonts

Tamil / Telugu / Marathi PDFs need fonts in `SpicAPI/Fonts/` (any `.ttf` / `.otf` / `.ttc`; the
family is the file name without the weight suffix, and a family whose name contains `Tamil`,
`Telugu` or `Devanagari` covers that script, e.g. `NotoSansTamil-Regular.ttf` +
`NotoSansTamil-Bold.ttf`). `Sas:Lab:ReportFonts:{lang}` overrides the keyword and
`Sas:Lab:FontsPath` the folder. Without a covering font the PDF is rendered in English and the
response carries `X-Report-Language-Fallback: <lang>`. The API logs the registered fonts and the
language coverage at startup (`LabFonts:`).
