namespace SPIC.RegressionHarness;

internal static class Program
{
	private static int Main()
	{
		Console.WriteLine("SPIC bulk-import regression harness");
		Console.WriteLine("===================================");
		Console.WriteLine();

		// Every importer routes its master matching through these two types, so they
		// are the highest-leverage checks in the repository: a regression here silently
		// reappears in all eleven bulk upload flows at once.
		MasterNormalizerTests.Run();
		Console.WriteLine();
		MissingMasterCollectorTests.Run();

		return Check.Report("Total");
	}
}
