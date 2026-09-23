using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5p_SeedGuestHouseCancellationPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seeds the standard 3-tier cancellation policy - taken verbatim from the
            // wording already shown on the CancelBooking page ("Free cancellation up to
            // 24 hours before check-in" / "50% refund within 24-48 hours" / "no refund
            // within 24 hours") - for every active Guest House that has no cancellation
            // policy configured yet. Safe to run repeatedly: a guest house that already
            // has any policy rows is skipped.
            migrationBuilder.Sql(@"
                INSERT INTO ""GuestHouseCancellationPolicy""
                    (""GuestHouseId"", ""PolicyName"", ""Description"", ""HoursBeforeCheckIn"", ""RefundPercentage"", ""CancellationChargePercentage"", ""IsActive"", ""CreatedBy"", ""CreatedAt"", ""UpdatedBy"", ""UpdatedAt"")
                SELECT gh.""Id"", tier.""PolicyName"", tier.""Description"", tier.""HoursBeforeCheckIn"", tier.""RefundPercentage"", tier.""CancellationChargePercentage"", TRUE, 'System', NOW(), 'System', NOW()
                FROM ""GuestHouses"" gh
                CROSS JOIN (VALUES
                    ('Standard Cancellation Policy', 'Full refund when cancelled 48 hours or more before check-in', 48, 100.0, 0.0),
                    ('Standard Cancellation Policy', '50% refund when cancelled between 24 and 48 hours before check-in', 24, 50.0, 50.0),
                    ('Standard Cancellation Policy', 'No refund when cancelled within 24 hours of check-in', 0, 0.0, 100.0)
                ) AS tier(""PolicyName"", ""Description"", ""HoursBeforeCheckIn"", ""RefundPercentage"", ""CancellationChargePercentage"")
                WHERE gh.""IsActive"" = TRUE
                  AND NOT EXISTS (SELECT 1 FROM ""GuestHouseCancellationPolicy"" p WHERE p.""GuestHouseId"" = gh.""Id"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""GuestHouseCancellationPolicy""
                WHERE ""PolicyName"" = 'Standard Cancellation Policy' AND ""CreatedBy"" = 'System';
            ");
        }
    }
}
