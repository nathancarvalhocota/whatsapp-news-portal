using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.Ingestion.Application;

namespace WhatsAppNewsPortal.Api.Ingestion.Infrastructure;

/// <summary>
/// Fetches raw HTML via HttpClient with explicit timeout and user-agent.
/// Returns null on any failure so a broken source never crashes the pipeline.
/// </summary>
public class HtmlFetcher : IHtmlFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HtmlFetcher> _logger;

    public HtmlFetcher(HttpClient httpClient, ILogger<HtmlFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch HTML from {Url}", url);
            return null;
        }
    }
}
