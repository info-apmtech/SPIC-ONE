using System.Collections.Generic;
using System.Linq;

namespace SPIC.Core.Entities
{
    /// <summary>
    /// Converts between the legacy 11-named-field expense shape (CSR1's own [NotMapped] properties,
    /// which CSR1SaveRequest/CSR1DetailsDto and the UI still use unchanged) and CSR1Expenses rows
    /// (the actual persisted, normalized shape). This is the one place that encodes which
    /// ExpenseInformation.Id each legacy field name corresponds to - every CSR1/CSR2 controller
    /// method that reads or writes these 11 fields should go through here, not duplicate the mapping.
    /// </summary>
    public static class CSR1ExpenseMapper
    {
        // Matches the live ExpenseInformations catalog (seeded by the "AddExpenseInformation"
        // migration, 2026-10-23). Id 1 does not exist (an early seed row was removed) - that is
        // expected, not a gap to fill here.
        public static readonly IReadOnlyDictionary<string, int> ExpenseIdByFieldName =
            new Dictionary<string, int>
            {
                ["PublicityMaterial"] = 2,
                ["StageArrangements"] = 3,
                ["ServiceChargesLCA"] = 4,
                ["TransportRent"] = 5,
                ["JeepRunningExpenses"] = 6,
                ["Refreshments"] = 7,
                ["Inputs"] = 8,
                ["Photography"] = 9,
                ["Compliments"] = 10,
                ["Others"] = 11,
                ["PrintingAndStationery"] = 12,
            };

        /// <summary>
        /// Populates CSR1's 11 legacy [NotMapped] properties from its CSR1Expenses rows. Call this
        /// after loading a CSR1 (and separately querying its CSR1Expenses) anywhere the old
        /// csr1.PrintingAndStationery-style reads still happen (CSR1 details/review-edit,
        /// CSR2's planned-amount lookups) - without it those properties stay at their default (0),
        /// since EF no longer populates them from the database.
        /// </summary>
        public static void ApplyExpenses(CSR1 csr1, IEnumerable<CSR1Expenses> expenses)
        {
            var byExpenseId = expenses
                .Where(e => e.IsActive && e.ExpenseId.HasValue)
                .ToDictionary(e => e.ExpenseId!.Value, e => e.ExpenseAmount);

            decimal Amount(string fieldName) =>
                byExpenseId.TryGetValue(ExpenseIdByFieldName[fieldName], out var amount) ? amount : 0m;

            csr1.PrintingAndStationery = Amount(nameof(csr1.PrintingAndStationery));
            csr1.PublicityMaterial = Amount(nameof(csr1.PublicityMaterial));
            csr1.StageArrangements = Amount(nameof(csr1.StageArrangements));
            csr1.ServiceChargesLCA = Amount(nameof(csr1.ServiceChargesLCA));
            csr1.TransportRent = Amount(nameof(csr1.TransportRent));
            csr1.JeepRunningExpenses = Amount(nameof(csr1.JeepRunningExpenses));
            csr1.Refreshments = Amount(nameof(csr1.Refreshments));
            csr1.Inputs = Amount(nameof(csr1.Inputs));
            csr1.Photography = Amount(nameof(csr1.Photography));
            csr1.Compliments = Amount(nameof(csr1.Compliments));
            csr1.Others = Amount(nameof(csr1.Others));
        }

        /// <summary>
        /// Converts CSR1's 11 legacy [NotMapped] properties (as submitted by the UI) into the
        /// CSR1Expenses rows to persist for the given CSR1Id. Only non-zero amounts get a row,
        /// matching the sparse shape already live in the database (a CSR1 with only 3 of the 11
        /// expenses filled in has exactly 3 CSR1Expenses rows, not 11 with 8 zeros).
        /// </summary>
        public static List<CSR1Expenses> ToExpenseRows(CSR1 csr1, int csr1Id) =>
            ExpenseIdByFieldName
                .Select(kvp => (FieldName: kvp.Key, ExpenseId: kvp.Value, Amount: AmountFor(csr1, kvp.Key)))
                .Where(x => x.Amount != 0m)
                .Select(x => new CSR1Expenses
                {
                    CSR1Id = csr1Id,
                    ExpenseId = x.ExpenseId,
                    ExpenseAmount = x.Amount,
                    IsActive = true
                })
                .ToList();

        private static decimal AmountFor(CSR1 csr1, string fieldName) => fieldName switch
        {
            nameof(CSR1.PrintingAndStationery) => csr1.PrintingAndStationery,
            nameof(CSR1.PublicityMaterial) => csr1.PublicityMaterial,
            nameof(CSR1.StageArrangements) => csr1.StageArrangements,
            nameof(CSR1.ServiceChargesLCA) => csr1.ServiceChargesLCA,
            nameof(CSR1.TransportRent) => csr1.TransportRent,
            nameof(CSR1.JeepRunningExpenses) => csr1.JeepRunningExpenses,
            nameof(CSR1.Refreshments) => csr1.Refreshments,
            nameof(CSR1.Inputs) => csr1.Inputs,
            nameof(CSR1.Photography) => csr1.Photography,
            nameof(CSR1.Compliments) => csr1.Compliments,
            nameof(CSR1.Others) => csr1.Others,
            _ => 0m
        };
    }
}
