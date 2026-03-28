namespace WhatsAppNewsPortal.Api.Ingestion.Application;

/// <summary>
/// Fetches raw HTML content from a URL.
/// Returns null on any failure (network, timeout, non-success status).
/// </summary>
public interface IHtmlFetcher
{
    Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default);
}
