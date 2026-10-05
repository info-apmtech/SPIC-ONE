using System.Reflection;

namespace SPIC.RegressionHarness;

/// <summary>
/// Minimal assertion runner.
///
/// The machine this repository is built on has no test framework in its offline NuGet
/// cache, and the behaviour that most needed locking down here is pure logic with no
/// database or HTTP dependency. Rather than add a package that cannot be restored, the
/// harness runs its own checks and reports through the exit code.
/// </summary>
internal static class Check
{
	private static readonly List<string> Failures = new();
	private static int _passed;

	public static void That(string name, bool condition, string? detail = null)
	{
		if (condition)
		{
			_passed++;
			Console.WriteLine($"  PASS  {name}");
			return;
		}

		Failures.Add(detail is null ? name : $"{name} — {detail}");
		Console.WriteLine($"  FAIL  {name}{(detail is null ? string.Empty : $" — {detail}")}");
	}

	public static void Equal<T>(string name, T expected, T actual) =>
		That(
			name,
			EqualityComparer<T>.Default.Equals(expected, actual),
			$"expected <{expected}> but was <{actual}>");

	public static void Throws<TException>(string name, Action action)
		where TException : Exception
	{
		try
		{
			action();
			That(name, false, $"expected {typeof(TException).Name} but nothing was thrown");
		}
		catch (TException)
		{
			That(name, true);
		}
		catch (Exception ex)
		{
			That(name, false, $"expected {typeof(TException).Name} but got {ex.GetType().Name}");
		}
	}

	public static int Report(string suite)
	{
		Console.WriteLine();
		Console.WriteLine(
			$"{suite}: {_passed} passed, {Failures.Count} failed.");

		if (Failures.Count == 0)
			return 0;

		Console.WriteLine();
		Console.WriteLine("Failures:");
		foreach (var failure in Failures)
			Console.WriteLine($"  - {failure}");

		return 1;
	}
}
