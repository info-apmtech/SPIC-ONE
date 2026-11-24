namespace SPIC.Core.Billing;

/// <summary>
/// The single Guest House bill payable / round-off / balance calculation, shared by the Generate Bill
/// page and the API (the API result is the authoritative one).
///
///   amount after discount = gross (subtotal + CGST + SGST) - discount
///   rounded payable       = nearest rupee of that amount, 50 paise and above rounds UP
///   round off             = rounded payable - amount after discount   (signed)
///   balance payable       = rounded payable - verified advance
///
/// Advance never takes part in the rounding. Decimal arithmetic, explicit midpoint rule.
/// </summary>
public static class GuestHouseBillMath
{
	public readonly record struct Result(decimal Gross, decimal Discount, decimal AmountAfterDiscount, decimal RoundOff, decimal RoundedPayable, decimal Advance, decimal Balance);

	public static decimal RoundToRupee(decimal amount) => Math.Round(amount, 0, MidpointRounding.AwayFromZero);

	public static Result Compute(decimal gross, decimal discount, decimal advance)
	{
		var afterDiscount = Math.Round(gross - discount, 2, MidpointRounding.AwayFromZero);
		var rounded = RoundToRupee(afterDiscount);
		var roundOff = rounded - afterDiscount;
		return new Result(gross, discount, afterDiscount, roundOff, rounded, advance, rounded - advance);
	}
}
