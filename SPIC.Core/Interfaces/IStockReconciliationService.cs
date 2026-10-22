using System;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	/// <summary>
	/// The State-wise Global Stock Reconciliation pipeline.
	///
	/// ONE calculation, one result: BuildAsync reads the uploaded report files,
	/// validates them, normalizes the state names and aggregates every source
	/// into a single state-wise table. The screen, the Excel export and the
	/// emailed workbook are all written from that one StockReconciliationResult,
	/// so they can never show different numbers.
	/// </summary>
	public interface IStockReconciliationService
	{
		/// <summary>
		/// Parses, validates and aggregates the five report files.
		/// Throws <see cref="StockReconciliationValidationException"/> when the
		/// files cannot be reconciled - nothing partial is ever returned.
		/// </summary>
		Task<StockReconciliationResult> BuildAsync(
			StockReconciliationInput input,
			DateTime asOnDate,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Calculates the state-wise reconciliation from already-imported database records.
		/// </summary>
		Task<StockReconciliationResult> BuildFromDatabaseAsync(
			StockReconciliationDatabaseQueryRequest request,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Fetches filter masters (companies, plants, products, available dates) from the database.
		/// </summary>
		Task<StockReconciliationFilterOptions> GetFilterOptionsAsync(
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Writes the reconciliation as a workbook. It only formats the values it
		/// is given; it never recalculates them.
		/// </summary>
		byte[] BuildExcel(StockReconciliationResult result);

		/// <summary>The workbook file name for a run, e.g. State_Wise_Stock_Reconciliation_20261021.xlsx.</summary>
		string BuildFileName(DateTime asOnDate);

		/// <summary>
		/// Builds the combined daily stock email package (subject, HTML body with 2 tables,
		/// 2-sheet Excel workbook, and missing-source validation).
		/// Reused by both manual send now and future automated scheduler.
		/// </summary>
		Task<DailyCombinedStockMailResult> BuildDailyStockMailAsync(
			DateTime currentBusinessDate,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Calculates the GFL state-wise product stock matrix from WholesalerStockAsOnToday records.
		/// </summary>
		Task<GflStockTableDto> BuildGflStockMatrixFromDatabaseAsync(
			DateTime asOnDate,
			CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// The uploaded files could not be reconciled. Every problem found is
	/// reported, not just the first one, so the user can fix the files in one go.
	/// </summary>
	public sealed class StockReconciliationValidationException : Exception
	{
		public StockReconciliationValidationException(string message, IReadOnlyList<string> errors)
			: base(message)
		{
			Errors = errors;
		}

		public IReadOnlyList<string> Errors { get; }
	}
}
