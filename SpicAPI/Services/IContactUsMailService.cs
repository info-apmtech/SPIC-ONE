using System.Threading;
using System.Threading.Tasks;

namespace SpicAPI.Services
{
	public interface IContactUsMailService
	{
		Task SendAsync(
			string replyToEmail,
			string replyToName,
			string subject,
			string htmlBody,
			CancellationToken cancellationToken);
	}
}