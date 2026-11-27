using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spic.Infrastructure.Services.Assistant;

/// <summary>One message of an Anthropic Messages API request: a role and content blocks
/// (<see cref="AnthropicContent.Text"/>, <see cref="AnthropicContent.ImageBase64"/>).</summary>
public sealed record AnthropicMessage(string Role, IReadOnlyList<object> Content)
{
    public static AnthropicMessage Text(string role, string text) => new(role, new object[] { AnthropicContent.Text(text) });
}

/// <summary>Content blocks in the Messages API wire shape.</summary>
public static class AnthropicContent
{
    public static object Text(string text) => new Dictionary<string, object?> { ["type"] = "text", ["text"] = text };

    /// <summary>An image block (base64, e.g. "image/jpeg"). For the farmer portal leaf-photo check (Task 6).</summary>
    public static object ImageBase64(string mediaType, string base64Data) => new Dictionary<string, object?>
    {
        ["type"] = "image",
        ["source"] = new Dictionary<string, object?> { ["type"] = "base64", ["media_type"] = mediaType, ["data"] = base64Data }
    };
}

/// <summary>A Messages API request. <see cref="Model"/> null = Assistant:Model.</summary>
public sealed record AnthropicRequest(string System, IReadOnlyList<AnthropicMessage> Messages, int MaxTokens, string? Model = null, int TimeoutSeconds = 60);

/// <summary>The text of a reply (thinking blocks are skipped) or why there is none.</summary>
public sealed record AnthropicResult(bool Ok, string? Text, string? StopReason, string? Error)
{
    public static AnthropicResult Fail(string error, string? stopReason = null) => new(false, null, stopReason, error);
}

/// <summary>
/// The one place SPIC ONE calls the Anthropic Messages API (raw HTTP, as the library assistant always has):
/// the SPIC AI library and farmer assistants and, next, the leaf-photo check. Reads Assistant:AnthropicApiKey
/// and Assistant:Model from <see cref="AssistantOptions"/>; never logs the key or the prompt. Never throws for
/// an API problem: the result says what went wrong and callers fall back.
/// </summary>
public class AnthropicMessagesClient
{
    public const string Endpoint = "https://api.anthropic.com/v1/messages";
    public const string AnthropicVersion = "2023-06-01";
    public const string DefaultModel = "claude-sonnet-5";

    private static readonly JsonSerializerOptions WireJson = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AssistantOptions _options;
    private readonly ILogger<AnthropicMessagesClient> _logger;

    public AnthropicMessagesClient(IHttpClientFactory httpClientFactory, IOptions<AssistantOptions> options, ILogger<AnthropicMessagesClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.HasAnthropicKey;

    public string Model => string.IsNullOrWhiteSpace(_options.Model) ? DefaultModel : _options.Model.Trim();

    private string Url => string.IsNullOrWhiteSpace(_options.Endpoint) ? Endpoint : _options.Endpoint.Trim();

    /// <summary>The JSON body for a request (public for the harness).</summary>
    public static string Serialize(AnthropicRequest request, string model)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = string.IsNullOrWhiteSpace(request.Model) ? model : request.Model,
            ["max_tokens"] = request.MaxTokens <= 0 ? 800 : request.MaxTokens,
            ["system"] = request.System,
            ["messages"] = request.Messages.Select(m => new Dictionary<string, object?> { ["role"] = m.Role, ["content"] = m.Content }).ToList()
        };
        return JsonSerializer.Serialize(body, WireJson);
    }

    public async Task<AnthropicResult> SendAsync(AnthropicRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return AnthropicResult.Fail("not configured");

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = new StringContent(Serialize(request, Model), Encoding.UTF8, "application/json")
            };
            message.Headers.TryAddWithoutValidation("x-api-key", _options.AnthropicApiKey);
            message.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds <= 0 ? 60 : request.TimeoutSeconds);

            using var response = await client.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic call failed with {Status}: {Body}", (int)response.StatusCode, Trim(body, 500));
                return AnthropicResult.Fail($"HTTP {(int)response.StatusCode}");
            }

            var (text, stopReason) = Parse(body);
            if (stopReason == "refusal")
            {
                _logger.LogWarning("Anthropic declined the request (stop_reason refusal).");
                return AnthropicResult.Fail("refusal", stopReason);
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Anthropic returned no text content (stop_reason {StopReason}).", stopReason);
                return AnthropicResult.Fail("no text", stopReason);
            }
            if (stopReason == "max_tokens")
                _logger.LogInformation("Anthropic answer reached max_tokens; the text is used as it is.");

            return new AnthropicResult(true, text, stopReason, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic call failed.");
            return AnthropicResult.Fail(ex.GetType().Name);
        }
    }

    /// <summary>The text blocks of a Messages API response joined, and its stop_reason.</summary>
    public static (string? Text, string? StopReason) Parse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var stop = doc.RootElement.TryGetProperty("stop_reason", out var sr) && sr.ValueKind == JsonValueKind.String ? sr.GetString() : null;
        if (!doc.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return (null, stop);

        var sb = new StringBuilder();
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) && type.GetString() == "text" && block.TryGetProperty("text", out var text))
                sb.Append(text.GetString());
        }
        return (sb.Length == 0 ? null : sb.ToString().Trim(), stop);
    }

    private static string Trim(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
}
