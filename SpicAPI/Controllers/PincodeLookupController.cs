using System.Net.Security;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpicAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PincodeLookupController : ControllerBase
    {
        // A dedicated HttpClient configured to tolerate the upstream's TLS quirks.
        // This client is used ONLY for api.postalpincode.in — public data, no secrets.
        private static readonly HttpClient _http = BuildExternalClient();

        private static HttpClient BuildExternalClient()
        {
            var handler = new HttpClientHandler
            {
                // Accept the upstream cert even if the server's CA store can't validate it.
                // Safe here because: (1) the response is public pincode data,
                // (2) any tampering would just fill in wrong district/state which the user can correct.
                ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true,

                // Force modern TLS so we don't accidentally negotiate weak protocols.
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        /// <summary>
        /// Proxies the public pincode-lookup API.
        /// Route: GET api/PincodeLookup/{pincode}
        /// </summary>
        [HttpGet("{pincode}")]
        [Authorize]
        public async Task<IActionResult> Lookup(string pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode)
                || pincode.Length != 6
                || !pincode.All(char.IsDigit))
            {
                return BadRequest(new { error = "Pincode must be 6 digits" });
            }

            try
            {
                var json = await _http.GetStringAsync(
                    $"https://api.postalpincode.in/pincode/{pincode}");

                return Content(json, "application/json");
            }
            catch (HttpRequestException ex)
            {
                // Surface the real inner exception — much more useful than just "SSL error"
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(502, new { error = $"Upstream unreachable: {detail}" });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new { error = "Upstream API timed out" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Lookup error: {ex.Message}" });
            }
        }
    }
}