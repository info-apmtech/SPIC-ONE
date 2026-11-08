# Publicity Material Tracking

Built 2026-11-08 from the RMDO and Admin Figma screenshots. One module, one set of pages that adapt
by role. Other roles' screens (RM / SMD / SMM detail variants, DGM, vendor) extend these files;
they do not get copies.

## Workflow

```
RMDO draft -> submit -> RM -> SMD -> SMM -> ADMIN-MD CS -> DGM-MD CS (final + note approval)
  -> ADMIN-MD CS dispatch (vendor, note approval document required) -> in transit -> delivered
  -> RMDO acknowledgement (received qty, condition, damaged qty, photos)
       no exception                  -> Completed
       shortage / damage             -> Exception Raised -> ADMIN-MD CS resolves:
            re-initiate from vendor  -> new dispatch -> acknowledgement
            stock transfer from HQ   -> transfer order to the source HQ RMDO (notification)
                                        -> source RMDO uploads courier proof (OCR fills AWB etc.)
                                        -> admin confirms (Transfer Created) -> delivered
                                        -> destination RMDO acknowledgement -> Completed
       excess                        -> Completed; the excess is offered as a transfer source
Any approver can Edit the quantity or Return (back to the RMDO, editable, resubmit).
```

Stage / Status after final approval are derived from the dispatch and acknowledgement rows
(`PublicityController.RecomputeAsync`); shortage, excess, usable and outstanding quantities are
computed, never stored.

## Roles (AppRole)

| Role | Does |
|---|---|
| RMD (RMDO) | creates, acknowledges, submits transfer proof for its HQs (claim HQ + RMDHQMapping) |
| RM / SMD / SMM | approve in order; scoped to region / state; consolidation page |
| MDAdmin (ADMIN-MD CS) | approves, dispatches, marks transit / delivered, resolves exceptions |
| MDHOD (DGM-MD CS) | final approval; may dispatch |
| Admin / SuperAdmin | everything (testing) |

## Role views (same pages, role switches)

| Screen | RMDO | RM | SMD / SMM | ADMIN-MD CS / DGM-MD CS |
|---|---|---|---|---|
| List | My Request, 8 KPIs | My Request, 5 KPIs (pending for me, approved by RM, dispatch, completed, returned), HQ filter | Submitted Requests, review banner, 6 KPIs, region + HQ filters | Submitted Requests, 6 KPIs, all filters |
| Details | view, edit & resubmit when returned | Additional Remarks, Edit, Return / Request Changes, Approve & Forward to SMD | same, forward to SMM / ADMIN-MD CS; read-only "Request Details" when not their turn | Create Dispatch, Resolve Exception, dispatch status |
| Edit | `/PublicityRequest/edit/{id}` | `/PublicityReview/edit/{id}` (full form, stays on the stage, changes logged) | same | same |
| Consolidation | - | Region Consolidation | State Consolidation; Approve & Submit enabled once every region reached me | All India |

## Pieces

| Layer | Files |
|---|---|
| Entities | `SPIC.Core/Entities/Publicity.cs` (request, dispatch, acknowledgement, file, event, notification, material master) |
| DTOs | `SPIC.Core/DTOs/PublicityDtos.cs` (route list in its header) |
| Migration | `V31_PublicityTracking` (7 new tables, 8 materials, page keys `PublicityRequest`, `PublicityReview` inserted by Key) |
| API | `SpicAPI/Controllers/PublicityController.cs`; files under `Uploads/Publicity/{requestId}`, served by `GET api/Publicity/file/{*path}` (access_token allowed) |
| Client | `Services/ApiClientBase.cs` (reusable plumbing) + `Services/PublicityApi.cs` (+ `PubText` labels) |
| Components | `Components/Publicity/*` (stepper, pill, info tile, header strip, overview, dispatch card, ack card, summary, docs, drop zone) |
| Pages | `Pages/Publicity/*`: List (`/PublicityRequest`, `/PublicityReview`), Form, Details, Dispatch (admin dispatch + RMDO transfer proof), Done, Consolidation, Notifications |
| Styles | `wwwroot/css/Publicity.css` (one global sheet, `.pub-` prefix), linked in both hosts |
| Check | `tools/publicity-api-check/publicity_api_check.py` (local DB only) |

## Deploy (product owner)

1. Local dry run first (done 2026-11-08: V31 applied cleanly on spicone_dev after the pending teammate migrations).
2. Backup job, `migrate.ps1` (V31), `deploy.ps1 -Quick`.
3. Grant `PublicityRequest` to the RMDO designation and `PublicityReview` to RM, SMD, SMM, ADMIN-MD CS and DGM-MD CS designations.
4. Users need the right AppRole (RMD, RM, SMD, SMM, MDAdmin, MDHOD) and an Employeelogin with state / region / HQ.
