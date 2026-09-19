using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Downloads a file from the API with the signed-in user's bearer token and hands
    /// it to the host to save or open. On the Web host that is a browser download; on
    /// MAUI the same JS name is bridged to the native viewer (see maui-interop.js).
    ///
    /// Replaces the undefined "spicDownload.post" JS helper that the report pages used
    /// to call, and the token-in-URL navigations for file endpoints.
    /// </summary>
    public sealed class FileDownloadService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public FileDownloadService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        /// <summary>POSTs <paramref name="body"/> as JSON and saves the file the endpoint returns.</summary>
        public async Task PostAsync(string url, object? body, string fileName, CancellationToken cancellationToken = default)
        {
            using var response = await _http.PostAsJsonAsync(url, body, cancellationToken);
            await SaveResponseAsync(response, fileName, cancellationToken);
        }

        /// <summary>GETs the endpoint and saves the file it returns.</summary>
        public async Task GetAsync(string url, string fileName, CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            await SaveResponseAsync(response, fileName, cancellationToken);
        }

        /// <summary>Saves bytes that are already in memory.</summary>
        public async Task SaveAsync(string fileName, string contentType, byte[] bytes)
        {
            await _js.InvokeVoidAsync("downloadFileFromBytes", fileName, contentType, Convert.ToBase64String(bytes));
        }

        private async Task SaveResponseAsync(HttpResponseMessage response, string fileName, CancellationToken cancellationToken)
        {
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            // Prefer the server's file name when it sends one.
            var serverName = response.Content.Headers.ContentDisposition?.FileNameStar
                             ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');

            await SaveAsync(string.IsNullOrWhiteSpace(fileName) ? (serverName ?? "download") : fileName, contentType, bytes);
        }
    }
}
