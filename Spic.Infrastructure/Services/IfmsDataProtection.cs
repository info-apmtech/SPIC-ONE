using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services;

/// <summary>
/// A Data Protection provider that belongs to the IFMS credential store alone.
/// </summary>
/// <remarks>
/// SpicAPI's own Data Protection (Identity tokens, cookies) must stay on the
/// API host's default key ring, exactly as production ran before the IFMS
/// work. Registering <c>AddDataProtection()</c> app-wide against the IFMS key
/// table re-pointed Identity at a remote key ring under a new application
/// name and broke every existing login. This type builds a private provider
/// on its own service collection instead: keys live in spiconeifms
/// (<c>DataProtectionKeys</c>) under application name "SPIC.Ifms", shared by
/// SpicAPI and SPIC.Ifms.Automation, and nothing else in either host sees it.
/// </remarks>
public interface IIfmsDataProtection
{
	IDataProtectionProvider Provider { get; }
}

public sealed class IfmsDataProtection : IIfmsDataProtection, IDisposable
{
	private readonly ServiceProvider _services;

	public IfmsDataProtection(string ifmsConnectionString)
	{
		if (string.IsNullOrWhiteSpace(ifmsConnectionString))
			throw new InvalidOperationException("IFMS data protection needs the spiconeifms connection string.");

		var services = new ServiceCollection();
		services.AddDbContext<IfmsDbContext>(o => o.UseNpgsql(ifmsConnectionString));
		services.AddDataProtection()
			.SetApplicationName("SPIC.Ifms")
			.PersistKeysToDbContext<IfmsDbContext>();

		_services = services.BuildServiceProvider();
		Provider = _services.GetRequiredService<IDataProtectionProvider>();
	}

	public IDataProtectionProvider Provider { get; }

	public void Dispose() => _services.Dispose();
}
