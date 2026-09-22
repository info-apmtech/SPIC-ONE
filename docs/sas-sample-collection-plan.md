# SAS Portal, version 1: Soil / Water Sample Collection

Owner: product owner (deploys) and the coordinating agent (builds). Started 2026-09-21 from ten
Figma screens supplied as screenshots (list, new collection, payment, sample summary drawer,
upload consignment, consignment submitted, consignment photos, consignment history, sample
collection details, consignment details).

## 1. Product decisions (from the product owner)

- **Free sample**: the Farmer / NGO category is hidden and the collection is marked Free; the
  flow goes Summary -> Upload Consignment. The payment page never appears for a free sample.
- **Paid sample**: category Farmer or NGO drives the price per sample; Summary -> Payment
  (UPI QR + transaction details + proof) -> Upload Consignment -> Submitted.
- **Who collects**: JMDO and MDO (Admin / CorporateAdmin as well). Other staff roles with the
  page granted can view. **Dealer and Farmer** see status and results only (read-only list and
  details; a Farmer sees only collections that carry their own farmer record).
- **Details page**: one page (the "Sample Collection Details" design with Result Comparison).
  The result table appears once the lab enters results; until then the section reads "Awaiting
  lab results" and the timeline shows where the sample is. This is a superset of the plain
  consignment-check design, so one page serves every role.
- **Payment review and lab results** are entered by Admin / CorporateAdmin for now (no lab
  portal yet): approve/reject a payment from the details page, enter results per sample.

## 2. Principles (same as the Community / Library work)

No live data (local PostgreSQL `spicone_dev` only); additive schema in one migration
(`V5n_SasSampleCollection`); the product owner runs `migrate.ps1` then `deploy.ps1`; contracts
fixed first (`SPIC.Core/DTOs/SasDtos.cs`, `SPIC.Core/Entities/SampleCollection.cs`); existing
API conventions; files under `Uploads/Sas/...` served by `GET api/Sas/file/{*path}` (on the
`access_token` allowlist); phone <= 767.98px, tablet, desktop unchanged elsewhere.

## 3. Architecture

| Layer | Content |
|---|---|
| Entities | `SasFarmer`, `SampleCollection`, `SampleItem`, `SamplePayment`, `SampleConsignment`, `ConsignmentPhoto`, `SasStatusEvent`, `SampleLabResult`, `SasSampleCharge` (seeded), `SasCourier` (seeded) |
| Permissions | `PagePermission.SampleCollection` (list, new, payment, consignment, submitted, view) and `PagePermission.ConsignmentHistory` (history, view), both appended at the end of the enum |
| API | `SasController` (`api/Sas/...`, routes in the DTO header); config `Sas:UpiId`, `Sas:MerchantName` |
| Client | `Shared/Services/SasApi.cs` + view models; pages under `Shared/Pages/Sas/` |
| Routes | `/SampleCollection` (list) · `/SampleCollection/new` (+ `?id=` for drafts) · `/SampleCollection/payment/{id}` · `/SampleCollection/consignment/{id}` · `/SampleCollection/submitted/{consignmentId}` · `/SampleCollection/view/{id}` · `/ConsignmentHistory` · `/ConsignmentHistory/view/{id}` |
| Navigation | sidebar links (gated by the page keys), tiles on the My Activities hub for field staff, More sheet entries |

Status flow of a collection: Draft -> (submit) -> Free: ReadyForConsignment / Paid:
PendingPayment -> (payment) PendingApproval -> (approve) ReadyForConsignment -> (consignment)
Dispatched -> InTransit -> DeliveredToLab -> TestInProgress -> Completed. Rejected payment
returns to PendingPayment. Every transition writes a `SasStatusEvent` (timeline).

QR code: generated on the client from `upi://pay?pa={UpiId}&pn={MerchantName}&am={amount}&cu=INR&tn={code}`
with a vendored MIT QR script (`Shared/wwwroot/js/qrcode.min.js`) so it works offline in MAUI.

## 4. Phases and agents

| Phase | Agent (Opus) | Owns |
|---|---|---|
| 0 | coordinator | this plan, entities, DTOs, DbContext, migration, permission keys, allowlist, config, navigation, hub tiles, docs |
| 1a | API-Sas | `SpicAPI/Controllers/SasController.cs` (+ optional `Spic.Infrastructure/Services/SasService.cs`) |
| 1b | Client-Collect | `Shared/Services/SasApi.cs`, `Shared/Services/SasModels.cs`, `Shared/Pages/Sas/SampleCollectionList.razor`, `SampleCollectionNew.razor`, `SampleSummaryDrawer.razor`, `SamplePayment.razor`, `NewFarmerDialog.razor` (+css), `Shared/wwwroot/js/qrcode.min.js` |
| 1c | Client-Consign | `Shared/Pages/Sas/ConsignmentUpload.razor`, `ConsignmentSubmitted.razor`, `ConsignmentHistory.razor`, `ConsignmentPhotosDialog.razor`, `ConsignmentDetails.razor` (+css); may add `Shared/Components/Sas/*` |
| 1d | Client-Details | `Shared/Pages/Sas/SampleCollectionDetails.razor` (+css), `Shared/Components/Sas/StatusTimeline.razor`, `Shared/Components/Sas/LabResultsTable.razor` (+css; results entry for review roles) |
| 2 | coordinator | local end-to-end run (MDO creates free and paid collections, admin approves, consignment, results, dealer read-only), role + device sweeps, docs, commit, push |
| 3 | product owner | `migrate.ps1`, `deploy.ps1`, grant Sample Collection / Consignment History to the MDO / JMDO / Dealer designations, set `Sas__UpiId` / `Sas__MerchantName` |

Shared client contract between 1b, 1c and 1d: `SasApi` (1b) is the only HTTP surface; 1c and 1d
call it and may add methods they need in a clearly marked "consignment" / "details" region at the
end of the file (append only, never edit existing members). The coordinator resolves overlaps.

## 4a. Live-site safety of the migrations (checked 2026-09-21)

The three pending migrations (`V5l_DigitalLibraryAndCommunity`, `V5m_CommunityReplyAttachments`,
`V5n_SasSampleCollection`) only create new tables, add one nullable column to a table created by
V5l, create indexes and insert seed rows. No existing table, column, row or index is altered or
dropped, so live data and the running API are untouched while they apply. Each migration runs in
its own transaction: a failure rolls back completely.

The one thing that could have failed was the page-catalogue seed: the three new rows carried fixed
ids 71-73, and a page registered on the live site through Page Management since 12 September
would already hold id 71. Those inserts are now idempotent SQL: fixed id when free, otherwise a
generated id, never touching existing rows, and the identity sequence is moved past MAX(Id).
Verified on a fresh database migrated to the live level (V5k), with a page inserted at id 71,
then the three migrations applied: they succeeded, the new keys landed at 72-74, a re-run was a
no-op. A key that lands on a different id than the model snapshot expects is harmless (all lookups
are by Key); the migration files must not be edited again after they have been applied to prod.

Deploy order stays: `migrate.ps1` first (old API keeps running, it ignores the new tables), then
`deploy.ps1` for API + web together.

## 5. Acceptance

- MDO: new collection with two farmers (one new farmer created inline), free path lands on Upload
  Consignment without a payment page; paid path shows the QR with the right amount, payment saved
  with proof, admin approves, consignment uploaded with label/package photos, submitted page shows
  the AWB, history lists it with the expandable sample lines, details page shows the timeline.
- Admin enters results for a sample; the collection becomes Completed; Dealer and Farmer roles
  open the details page read-only and see status and results; they cannot see New / Pay / Consign.
- All pages pass the phone checks (392 px, no overflow, 44 px targets), sweeps clean per role.
