namespace SPIC.RegressionHarness;

internal static class Program
{
	private static int Main()
	{
		Console.WriteLine("SPIC regression harness");
		Console.WriteLine("========================");
		Console.WriteLine();

		// Every importer routes its master matching through these two types, so they
		// are the highest-leverage checks in the repository: a regression here silently
		// reappears in all eleven bulk upload flows at once.
		MasterNormalizerTests.Run();
		Console.WriteLine();
		MissingMasterCollectorTests.Run();
		Console.WriteLine();

		// Role -> authorization model, the bypass sets and the OpenAccess list: pure
		// data, so the Designation-based access rules can be asserted without a server.
		PageAuthorizationTests.Run();
		Console.WriteLine();

		// Guest House single and multi-record inventory availability, holding windows,
		// serialization detection, cancellations, check-in, check-out and allocations.
		GuestHouseInventoryTests.Run();

		return Check.Report("Total");
	}
}
