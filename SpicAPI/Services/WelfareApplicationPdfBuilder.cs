using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
    internal static class WelfareApplicationPdfBuilder
    {
        static WelfareApplicationPdfBuilder()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] Build(WelfareApplication app) =>
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                    page.Header().Element(e => ComposeHeader(e, app));
                    page.Content().Element(e => ComposeContent(e, app));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();

        private static void ComposeHeader(IContainer header, WelfareApplication app)
        {
            header.BorderBottom(1).BorderColor("#D8D8D8").PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("SPIC").FontSize(20).Bold().FontColor("#0B4A82");
                    col.Item().Text("Welfare Scheme Application Form").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                row.AutoItem().Column(col =>
                {
                    col.Item().AlignRight().Text(string.IsNullOrWhiteSpace(app.ApplicationNumber) ? $"APP-{app.Id:000000}" : app.ApplicationNumber)
                        .FontSize(13).Bold().FontColor("#0B4A82");
                    col.Item().AlignRight().PaddingTop(3).Element(e => StatusChip(e, app.Status));
                });
            });
        }

        private static IContainer StatusChip(IContainer c, WelfareApplicationStatus status)
        {
            var (bg, fg, label) = status switch
            {
                WelfareApplicationStatus.Approved => ("#DCFCE7", "#166534", "Approved"),
                WelfareApplicationStatus.Rejected => ("#FEE2E2", "#991B1B", "Rejected"),
                WelfareApplicationStatus.Cancelled => ("#E0E7FF", "#3730A3", "Cancelled"),
                WelfareApplicationStatus.Draft => ("#F3F4F6", "#374151", "Draft"),
                WelfareApplicationStatus.Submitted => ("#DBEAFE", "#1E40AF", "Submitted"),
                _ => ("#FEF3C7", "#92400E", SpicAPI.Controllers.SDWAWelfareApplicationController.GetStatusDisplayName(status))
            };

            c.Background(bg).PaddingHorizontal(8).PaddingVertical(3)
                .Text(label).FontSize(9).SemiBold().FontColor(fg);
            return c;
        }

        private static void ComposeFooter(IContainer footer)
        {
            footer.PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                row.AutoItem().Text(t =>
                {
                    t.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void ComposeContent(IContainer content, WelfareApplication app)
        {
            content.Column(col =>
            {
                col.Spacing(14);

                col.Item().Element(e => Section(e, "Application Summary"));
                col.Item().Element(e => InfoColumns(e, new List<(string, string)>
                {
                    ("Scheme", SpicAPI.Controllers.SDWAWelfareApplicationController.GetSchemeDisplayName(app.SchemeName)),
                    ("Applied On", app.ApplicationDate.ToString("dd MMM yyyy")),
                    ("Current Status", SpicAPI.Controllers.SDWAWelfareApplicationController.GetStatusDisplayName(app.Status)),
                    ("Beneficiary Group", app.BeneficiaryGroup),
                    ("Sub Dealer", app.SubDealerName),
                    ("Employee", app.EmployeeName)
                }));

                col.Item().Element(e => Section(e, "Dealer Details"));
                col.Item().Element(e => InfoColumns(e, new List<(string, string)>
                {
                    ("Dealer Code", app.DealerCode),
                    ("Firm / Dealer Name", app.DealerName),
                    ("Nature of Dealership", app.DealershipNature),
                    ("Mobile Number", app.MobileNumber),
                    ("Region", app.Region),
                    ("District", app.District),
                    ("Quantity Lifted (Cases)", app.QuantityLifted?.ToString()),
                    ("Avg Qty Lifted 3Y (MT)", app.AverageQuantityLifted3Years?.ToString("0.##")),
                    ("Last Year Qty (MT)", app.LastYearQuantityLifted?.ToString("0.##"))
                }));

                col.Item().Element(e => Section(e, "Beneficiary Details"));
                col.Item().Element(e => InfoColumns(e, new List<(string, string)>
                {
                    ("Beneficiary Name", app.BeneficiaryName),
                    ("Relationship", app.Relationship),
                    ("Date of Birth", FmtDate(app.BeneficiaryDateOfBirth)),
                    ("Nominee Name", app.NomineeName),
                    ("Nominee Relationship", app.NomineeRelationship),
                    ("Name as in Cheque", app.BeneficiaryNameAsInCheque),
                    ("Name Source", app.LeafOrBankPassbook)
                }));

                var schemeFields = SchemeSpecificFields(app);
                if (schemeFields.Count > 0)
                {
                    col.Item().Element(e => Section(e, "Scheme Specific Details"));
                    col.Item().Element(e => InfoColumns(e, schemeFields));
                }

                col.Item().Element(e => Section(e, "Uploaded Documents"));
                col.Item().Element(e => DocumentsTable(e, app));

                col.Item().Element(e => Section(e, "Approval Trail"));
                col.Item().Element(e => ApprovalsTable(e, app));

                col.Item().Element(e => Section(e, "Declaration"));
                col.Item().Text("I hereby declare that the particulars furnished in this application are true and correct to the best of my knowledge, and I undertake to produce the original documents for verification whenever required.")
                    .Italic().FontSize(9).LineHeight(1.4f);
                col.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Confirmed by dealer: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    t.Span(app.IsDeclarationConfirmed ? "Yes" : "No").FontSize(9).SemiBold();
                });
            });
        }

        private static List<(string, string)> SchemeSpecificFields(WelfareApplication app) => app.SchemeName switch
        {
            WelfareSchemeType.MedicalAssistance => new List<(string, string)>
            {
                ("Treatment Type", app.MedicalTreatmentType)
            },
            WelfareSchemeType.Wedding => new List<(string, string)>
            {
                ("Date of Marriage", FmtDate(app.MarriageDate))
            },
            WelfareSchemeType.Grahapravesam => new List<(string, string)>
            {
                ("Event Date", FmtDate(app.EventDate)),
                ("House Ownership", app.OwnershipType),
                ("Event Venue", app.EventVenue)
            },
            WelfareSchemeType.EducationalAssistance => new List<(string, string)>
            {
                ("Course", app.Course),
                ("Year of Study", app.EduYear?.ToString()),
                ("College / Institution", app.CollegeName),
                ("Course Duration (Years)", app.TotalNumberOfCourses?.ToString()),
                ("First Application", app.IsFirstApplication == null ? "" : app.IsFirstApplication.Value ? "Yes" : "No (Renewal)")
            },
            WelfareSchemeType.Sathabhishekam => new List<(string, string)>
            {
                ("Event Date", FmtDate(app.EventDate))
            },
            WelfareSchemeType.DeathRelief => new List<(string, string)>
            {
                ("Date of Death", FmtDate(app.DateOfDeath)),
                ("Legal Heir", app.LegalHeirName),
                ("Cause of Death", app.DeathCause)
            },
            WelfareSchemeType.MeritAward => new List<(string, string)>
            {
                ("Candidate Name", app.MeritCandidateName),
                ("Father's Name", app.MeritFatherName),
                ("Examination Appeared", app.ExaminationAppeared),
                ("Board", app.BoardName),
                ("Maximum Marks", app.MaximumMarks?.ToString()),
                ("Marks Obtained", app.MarksObtained?.ToString()),
                ("Percentage", app.MeritPercentage == null ? "" : $"{app.MeritPercentage:0.00}%")
            },
            WelfareSchemeType.DistinctionAward => new List<(string, string)>
            {
                ("Candidate Name", app.DistinctionCandidateName),
                ("Father's Name", app.DistinctionFatherName),
                ("Professional Course", app.ProfessionalCourseName),
                ("Completion Year", app.CourseCompletionYear),
                ("University / Institution", app.UniversityName),
                ("Maximum Marks", app.DistinctionMaximumMarks?.ToString()),
                ("Marks Obtained", app.DistinctionMarksObtained?.ToString()),
                ("Aggregate Percentage", app.DistinctionAggregatePercentage == null ? "" : $"{app.DistinctionAggregatePercentage:0.00}%"),
                ("Arrears", app.HasArrears == null ? "" : app.HasArrears.Value ? "Yes" : "No"),
                ("Wholesale Dealer Employee", app.IsWholesaleDealerEmployee == null ? "" : app.IsWholesaleDealerEmployee.Value ? "Yes" : "No")
            },
            _ => new List<(string, string)>()
        };

        private static void InfoColumns(IContainer c, List<(string Label, string Value)> items)
        {
            var filled = items.Where(i => !string.IsNullOrWhiteSpace(i.Value)).ToList();
            if (filled.Count == 0)
            {
                c.Text("Not provided.").Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            var half = (filled.Count + 1) / 2;
            c.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Spacing(5);
                    foreach (var (label, value) in filled.Take(half))
                        col.Item().Element(e => Kv(e, label, value));
                });
                row.RelativeItem().PaddingLeft(18).Column(col =>
                {
                    col.Spacing(5);
                    foreach (var (label, value) in filled.Skip(half))
                        col.Item().Element(e => Kv(e, label, value));
                });
            });
        }

        private static void Kv(IContainer c, string label, string value)
        {
            c.Row(row =>
            {
                row.ConstantItem(115).Text(label).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().Text(value).FontSize(10);
            });
        }

        private static IContainer Section(IContainer c, string title)
        {
            c.PaddingTop(4).PaddingBottom(6).BorderBottom(1).BorderColor("#D8D8D8")
                .Text(title).FontSize(11).Bold().FontColor("#0B4A82");
            return c;
        }

        private static void DocumentsTable(IContainer c, WelfareApplication app)
        {
            var docs = app.Documents.OrderBy(d => d.UploadedAt).ToList();
            if (docs.Count == 0)
            {
                c.Text("No documents were uploaded with this application.").Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(55);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeadCell).Text("Category").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Document").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("File").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Size").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Verified").FontColor(Colors.White).FontSize(9).SemiBold();
                });

                foreach (var d in docs)
                {
                    table.Cell().Element(BodyCell).Text(d.DocumentType ?? "-");
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(d.DocumentName) ? d.FileName ?? "-" : d.DocumentName);
                    table.Cell().Element(BodyCell).Text(d.FileName ?? "-");
                    table.Cell().Element(BodyCell).Text(FormatFileSize(d.FileSize));
                    table.Cell().Element(BodyCell).Text(d.IsVerified ? "Yes" : "Pending");
                }
            });
        }

        private static void ApprovalsTable(IContainer c, WelfareApplication app)
        {
            var approvals = app.Approvals.OrderBy(a => a.CreatedAt).ToList();
            if (approvals.Count == 0)
            {
                c.Text("No approval activity recorded yet.").Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            c.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(60);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(90);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeadCell).Text("Level").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Status").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Actioned By").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Date").FontColor(Colors.White).FontSize(9).SemiBold();
                    header.Cell().Element(HeadCell).Text("Remarks").FontColor(Colors.White).FontSize(9).SemiBold();
                });

                foreach (var a in approvals)
                {
                    table.Cell().Element(BodyCell).Text(ApprovalLevelLabel(a.ApprovalLevel));
                    table.Cell().Element(BodyCell).Text(a.ApprovalStatus.ToString());
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(a.ApprovedBy) ? "-" : a.ApprovedBy);
                    table.Cell().Element(BodyCell).Text(a.ApprovedAt?.ToString("dd MMM yyyy hh:mm tt") ?? "-");
                    table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(a.Remarks) ? "-" : a.Remarks);
                }
            });
        }

        private static IContainer HeadCell(IContainer c) =>
            c.Background("#0B4A82").PaddingVertical(5).PaddingHorizontal(6);

        private static IContainer BodyCell(IContainer c) =>
            c.BorderBottom(0.5f).BorderColor("#E5E7EB").PaddingVertical(4).PaddingHorizontal(6);

        private static string ApprovalLevelLabel(AppRole role) => role switch
        {
            AppRole.MO => "Marketing Officer",
            AppRole.RM => "Regional Manager",
            AppRole.SMM => "Senior Manager",
            _ => role.ToString()
        };

        private static string FmtDate(DateTime? date) => date?.ToString("dd MMM yyyy") ?? "";

        private static string FormatFileSize(long? size)
        {
            if (size == null) return "-";
            double s = size.Value;
            return s >= 1048576 ? $"{s / 1048576:0.#} MB" : s >= 1024 ? $"{s / 1024:0.#} KB" : $"{s} B";
        }
    }
}
