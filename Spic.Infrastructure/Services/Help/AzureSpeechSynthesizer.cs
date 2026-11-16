using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace Spic.Infrastructure.Services.Help;

/// <summary>
/// Azure AI Speech text-to-speech over REST (no SDK): POST the SSML, get an MP3 back.
/// Only called by <see cref="HelpAudioCache"/> for a clip that has never been generated.
/// </summary>
public sealed class AzureSpeechSynthesizer : ISpeechSynthesizer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HelpSpeechOptions _options;

    public AzureSpeechSynthesizer(IHttpClientFactory httpClientFactory, IOptions<HelpSpeechOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<byte[]> SynthesizeAsync(string ssml, CancellationToken ct)
    {
        if (!_options.Enabled) throw new InvalidOperationException("Help Desk voice is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://{_options.SpeechRegion}.tts.speech.microsoft.com/cognitiveservices/v1")
        {
            Content = new StringContent(ssml, Encoding.UTF8),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ssml+xml");
        request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _options.SpeechKey);
        request.Headers.TryAddWithoutValidation("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");
        request.Headers.TryAddWithoutValidation("User-Agent", "spic-help");

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Azure Speech returned {(int)response.StatusCode}: {detail[..Math.Min(200, detail.Length)]}");
        }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
