using Spic.Infrastructure.Services.MasterData;

namespace SPIC.RegressionHarness;

/// <summary>
/// Pins the behaviour every importer relies on when a file references master data that
/// does not exist: report all of it at once, with row numbers, and write nothing.
/// </summary>
internal static class MissingMasterCollectorTests
{
	public static void Run()
	{
		Console.WriteLine("MissingMasterCollector");

		var collector = new MissingMasterCollector();
		Check.That("a fresh collector is empty", !collector.HasAny);

		collector.Add("State", "Tamil Nadu", 4);
		collector.Add("State", "tamil  nadu", 9);
		collector.Add("District", "Chennai", 12);

		Check.That("entries were recorded", collector.HasAny);
		Check.Equal("each distinct master is one entry", collector.Entries.Count, 2);

		// The whole point of the collector: one user sees every bad row, not just the
		// first failure, so a master is never created and re-uploaded repeatedly.
		var state = collector.Entries.Single(e => e.Kind == "State");
		Check.Equal(
			"repeats of the same value collapse into one entry",
			string.Join(",", state.RowNumbers),
			"4,9");
		Check.Equal("the original casing is preserved for display", state.Value, "Tamil Nadu");

		var (message, lines) = collector.Build("location master");

		Check.Equal("one line per distinct master", lines.Count, 2);
		Check.That(
			"the message states that nothing was created",
			message.Contains("No location master records were created", StringComparison.Ordinal));
		Check.That(
			"the message states that no master data was added",
			message.Contains("no master data was added", StringComparison.Ordinal));
		Check.That(
			"the line names the master and its rows",
			lines.Any(l => l.Contains("Missing State: Tamil Nadu", StringComparison.Ordinal) &&
				l.Contains("4, 9", StringComparison.Ordinal)));
		Check.That(
			"a single row is phrased in the singular",
			lines.Any(l => l.Contains("— row 12", StringComparison.Ordinal)));
		Check.That(
			"lines are ordered by kind then value",
			string.Join("|", lines).IndexOf("District", StringComparison.Ordinal) <
			string.Join("|", lines).IndexOf("State", StringComparison.Ordinal));

		// A blank cell carries nothing to create, so it must not produce a phantom
		// "Missing State: " entry.
		var blanks = new MissingMasterCollector();
		blanks.Add("State", "   ", 3);
		blanks.Add("State", null, 4);
		blanks.Add("Region", string.Empty, 5);
		Check.That("blank references are ignored", !blanks.HasAny);

		// A blank-but-required column is still reported by the importers themselves, by
		// passing an explicit marker rather than an empty string.
		var marked = new MissingMasterCollector();
		marked.Add("Zone", "<blank>", 7);
		Check.That("an explicit blank marker is reported", marked.HasAny);
		Check.Equal("the marker carries its row", marked.Entries[0].RowNumbers[0], 7);
	}
}
