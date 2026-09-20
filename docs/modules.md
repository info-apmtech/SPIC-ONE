# SPIC ONE: module and feature status

Source: module sheet shared by the product owner on 2026-09-19 (Google Sheet "modules list").
"Documentation" = functional documentation; "Design" = Figma design
(https://www.figma.com/design/HkdquY0LylyLn9Yo7m3xkR/SPIC). Blank means not started.

Implementation priority stated on 2026-09-19: **SAS Portal** and **Digital Library**
(Chatbot, AI Videos, Product Information / product browser, Product Brochure); design all
their pages first from Figma, then implement functionality one by one. The Figma file is too
heavy for browser automation; page designs come as screenshots from the product owner.

| Module | Feature | Documentation | Design |
|---|---|---|---|
| Subsidy Management System | Pending Acknowledgement | Completed | Completed |
| Subsidy Management System | Stock Aging | Completed | Completed |
| Subsidy Management System | Notifications | Not started | |
| Subsidy Management System | Customized Reports | In progress | In progress |
| SDWA | Room Bookings | Completed | Completed |
| SDWA | Welfare Scheme | Completed | Completed |
| SDWA | Dealer Registration | Completed | Completed |
| SDWA | Reports | Completed | Completed |
| Digital Library | Chatbot | Completed | Completed |
| Digital Library | AI Videos | Completed | Completed |
| Digital Library | Product Information | Completed | Completed |
| Digital Library | Product Brochure | Completed | Completed |
| SAS | Soil Sample Collection and Analysis | Completed | Completed |
| SAS | Budgeting Activities | In progress | |
| SAS | MSTL Van Tracking | In progress | |
| SAS | Field Programs Tracking | In progress | |
| SAS | Farm Activities Tracking | Completed | In progress |
| SAS | SPC Renewal Reminders | In progress | |
| SAS | Reports | In progress | |
| SAS | SAS Report in 6 Languages | In progress | |
| MD Portal | Budgeting, CSR-1 and CSR-2 | Completed | Completed |
| MD Portal | MD Report | Completed | Completed |
| MD Portal | Sale Audit | Completed | Completed |
| MD Portal | Demo Documentation | Completed | Completed |
| Publicity | Indent creation, Approval, Dispatch, Tracking, Acknowledgement | In progress | |
| Dealer Portal | Dealer Enrolment | Completed | Completed |
| Dealer Portal | Dealer Outstanding Notification | | |
| Dealer Portal | Reports: SOA / Collections Since | | |
| Dealer Portal | Order Placement | | |
| Compliance | Dealer License | | |
| Compliance | State License | | |
| Compliance | AV Van License | | |
| Specialty Products | Indent Creation | Completed | Completed |
| Specialty Products | Trade Terms | Completed | Completed |
| Farmer Portal | Farmer Chatbot | | |
| Farmer Portal | Farmer Community Connect | | Completed |
| Farmer Portal | Predictive Analysis based on AI | | |
| Service Portal | Farmer Scheme | Completed | Completed |
| Service Portal | Drone Seva | Completed | Completed |
| Service Portal | Dealer Scheme | Completed | Completed |
| Service Portal | MO / MDO incentive | | |
| Service Portal | AV Van Tracking | | |
| Service Portal | Evaluation: JMDO / MO / MDO | | |
| Dashboard | SDWA, SAS, MD and CS, Sales Portal, Subsidy, Dealer Portal, Farmer Portal, Customized Main Dashboard | | |
| Sales Portal | Indent creation | Completed | Completed |
| Sales Portal | SO Generation and Approval | Completed | Completed |
| Sales Portal | STO Creation and Approval | Completed | Completed |
| Sales Portal | Automated POs for Third Party | Completed | Completed |
| Sales Portal | SAP Integration | In progress | In progress |
| Sales Portal | Customized Reports | Completed | Completed |
| Imports | Customized reports, Automated reminders | | |
| Industrial Products | Customized reports, Automated reminders | | |

## Mapping to the existing app

Already implemented in `SPIC.MauiBlazorApp.Shared/Pages`: Subsidy (Pending Acknowledgement,
Stock Aging, Liquidation, Company Sales, Ageing), SDWA (dealer registration, welfare schemes,
guest house bookings, lucky draw, gallery), MD Portal (budget, CSR-1, CSR-2, final report),
Dealer enrolment and credit limits, logistics, settings.

Not yet in the app: Digital Library (4 features), SAS (8 features), Specialty Products,
Service Portal, Sales Portal, Farmer Portal, Publicity, Compliance, Imports, Industrial
Products, the cross-module dashboards.

## Digital Library implementation log

- 2026-09-20: landing page `/DigitalLibrary` built from the Figma frame (hero with assistant
  search and suggestion chips, AI Videos / Product Information / Product Brochures cards,
  featured strip, content grid with publish/edit/delete/archive). Sample data only; the
  content API comes next. Sub-sections (`/DigitalLibrary/videos|products|brochures|chat|add`)
  are placeholder screens until their frames are implemented. Menu entry is admin-only until
  `PagePermission` gains a `DigitalLibrary` key (API + permission catalogue change).
- Figma automation: the file loads in the browser pane but frames render as low-resolution
  tiles, so designs are taken from full-size screenshots supplied by the product owner.
