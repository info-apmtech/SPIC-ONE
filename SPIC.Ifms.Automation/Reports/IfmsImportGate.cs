using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Reports;

/// <summary>
/// Serialises IFMS import work so two runs can never post the same report at the
/// same time.
///
/// The scheduled trigger, the manual trigger worker, the <c>run-now</c> command and
/// the <c>upload-saved</c> command all funnel into the same upload endpoint. The
/// endpoint upserts most tables by business key, but several report tables append,
/// so two overlapping runs of the same report date can duplicate rows. Nothing in
/// the database prevented that: the <c>run-now</c> command checked for a pending or
/// running row, but that check and the insert were not atomic, and the command-line
/// tools shared no state with the hosted workers at all.
///
/// This gate closes both gaps without a schema change:
///   * a <see cref="SemaphoreSlim"/> serialises callers inside one process;
///   * an exclusively-opened lock file serialises separate processes (a second copy
///     of the service, or an operator running <c>upload-saved</c> by hand while the
///     service is up).
///
/// The lock file records the owning process id, so a file left behind by a process
/// that was killed is recognised as stale instead of blocking imports forever.
/// </summary>
public sealed class IfmsImportGate
{
	private readonly SemaphoreSlim _inProcess = new(1, 1);
	private readonly ILogger<IfmsImportGate> _logger;
	private readonly string _lockFilePath;
	private readonly TimeSpan _staleAfter;

	public IfmsImportGate(
		ILogger<IfmsImportGate> logger,
		IOptions<IfmsOptions> options)
	{
		var ifmsOptions = options.Value;
		_logger = logger;
		_lockFilePath = Path.Combine(
			Path.GetFullPath(string.IsNullOrWhiteSpace(ifmsOptions.DownloadRoot)
				? "downloads"
				: ifmsOptions.DownloadRoot),
			"ifms-import.lock");

		// A nightly run is long. Anything older than this is treated as abandoned,
		// which is only reachable if the owning process died without releasing.
		_staleAfter = TimeSpan.FromHours(12);
	}

	/// <summary>
	/// Takes the import lock, or throws <see cref="InvalidOperationException"/> when
	/// another run already holds it. Callers must dispose the returned lease.
	/// </summary>
	public async Task<IAsyncDisposable> AcquireAsync(
		string operation,
		CancellationToken cancellationToken)
	{
		if (!await _inProcess.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
		{
			throw new InvalidOperationException(
				$"Another IFMS {operation} is already running in this process. " +
				$"'{operation}' was not started, because two overlapping runs of the same " +
				"report date can duplicate rows.");
		}

		FileStream? lockFile = null;
		try
		{
			lockFile = TryOpenLockFile();
			_logger.LogInformation(
				"IFMS import lock acquired for {Operation}.", operation);

			return new Lease(_inProcess, lockFile, _logger, operation);
		}
		catch
		{
			// The lock file could not be taken, so release the in-process slot too,
			// otherwise the gate would deadlock for the lifetime of the process.
			_inProcess.Release();
			throw;
		}
	}

	private FileStream? TryOpenLockFile()
	{
		var directory = Path.GetDirectoryName(_lockFilePath);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		for (var attempt = 0; attempt < 2; attempt++)
		{
			try
			{
				var stream = new FileStream(
					_lockFilePath,
					FileMode.OpenOrCreate,
					FileAccess.ReadWrite,
					FileShare.None);

				using var writer = new StreamWriter(stream, leaveOpen: true);
				writer.WriteLine($"pid={Environment.ProcessId}");
				writer.WriteLine($"since={DateTimeOffset.UtcNow:O}");
				writer.Flush();
				stream.Flush(true);

				return stream;
			}
			catch (IOException) when (attempt == 0 && IsStaleLockFile())
			{
				_logger.LogWarning(
					"Removing stale IFMS import lock file {LockFile}; its owner is no longer running.",
					_lockFilePath);

				try
				{
					File.Delete(_lockFilePath);
				}
				catch (IOException)
				{
					// Another process got there first and may now hold a live lock.
					throw new InvalidOperationException(
						"Another IFMS import is already running (lock file could not be " +
						$"reclaimed: {_lockFilePath}).");
				}
			}
		}

		throw new InvalidOperationException(
			"Another IFMS import is already running (lock file held by another process: " +
			$"{_lockFilePath}). Wait for it to finish, or stop that process first.");
	}

	/// <summary>
	/// True when the lock file exists but its owning process is gone, or it is simply
	/// too old to be a live run.
	/// </summary>
	private bool IsStaleLockFile()
	{
		try
		{
			var info = new FileInfo(_lockFilePath);
			if (!info.Exists)
				return false;

			if (DateTime.UtcNow - info.LastWriteTimeUtc > _staleAfter)
				return true;

			var pid = ReadPid();
			if (pid is null)
				return false;

			// A pid we can see is a live owner, so the lock is genuinely held.
			return !IsProcessAlive(pid.Value);
		}
		catch (IOException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
	}

	private int? ReadPid()
	{
		try
		{
			using var stream = new FileStream(
				_lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = new StreamReader(stream);

			while (reader.ReadLine() is { } line)
			{
				if (line.StartsWith("pid=", StringComparison.OrdinalIgnoreCase) &&
					int.TryParse(line.AsSpan(4), out var parsed))
				{
					return parsed;
				}
			}
		}
		catch (IOException)
		{
			// Unreadable lock file: assume it is held rather than stealing it.
		}
		catch (UnauthorizedAccessException)
		{
		}

		return null;
	}

	private static bool IsProcessAlive(int pid)
	{
		try
		{
			using var process = Process.GetProcessById(pid);
			return !process.HasExited;
		}
		catch (ArgumentException)
		{
			// No such process: the owner is gone.
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private sealed class Lease : IAsyncDisposable
	{
		private readonly SemaphoreSlim _inProcess;
		private readonly FileStream? _lockFile;
		private readonly ILogger _logger;
		private readonly string _operation;
		private bool _released;

		public Lease(
			SemaphoreSlim inProcess,
			FileStream? lockFile,
			ILogger logger,
			string operation)
		{
			_inProcess = inProcess;
			_lockFile = lockFile;
			_logger = logger;
			_operation = operation;
		}

		public async ValueTask DisposeAsync()
		{
			if (_released)
				return;

			_released = true;

			if (_lockFile is not null)
			{
				try
				{
					// Close before deleting: on Windows the file cannot be removed while
					// this handle is open.
					_lockFile.Dispose();
					File.Delete(_lockFile.Name);
				}
				catch (IOException ex)
				{
					_logger.LogWarning(
						ex,
						"Could not remove the IFMS import lock file {LockFile}; it will be " +
						"reclaimed as stale on the next run.",
						_lockFile.Name);
				}
				catch (UnauthorizedAccessException ex)
				{
					_logger.LogWarning(
						ex,
						"Could not remove the IFMS import lock file {LockFile}.",
						_lockFile.Name);
				}
			}

			_inProcess.Release();
			_logger.LogInformation("IFMS import lock released for {Operation}.", _operation);
		}
	}
}
