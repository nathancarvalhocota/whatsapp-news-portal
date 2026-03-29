using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Ingestion.Infrastructure;

/// <summary>
/// Discovers items from sources that require direct HTML page reading (no RSS feed).
/// Uses configurable CSS selectors per source to extract article links and content.
/// Returns an empty list on any failure so a broken source never crashes the pipeline.
/// </summary>
public class HtmlIngestionAdapter : IIngestionAdapter
{
    private readonly IHtmlFetcher _fetcher;
    private readonly ILogger<HtmlIngestionAdapter> _logger;

    private static readonly Dictionary<string, HtmlSourceParserConfig> SourceConfigs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["business.whatsapp.com"] = new HtmlSourceParserConfig
        {
            ArticleLinkSelector = "a[href*='/blog/']",
            ArticleLinkPattern = @"/blog/.+",
            TitleSelector = "h1, .post-title",
            ContentSelector = "article, .blog-post-content, .post-body, main",
            DateSelector = "time[datetime], .post-date"
        },
        ["developers.facebook.com"] = new HtmlSourceParserConfig
        {
            ArticleLinkSelector = "a[href*='/docs/whatsapp']",
            ArticleLinkPattern = @"/docs/whatsapp/.+",
            TitleSelector = "h1",
            ContentSelector = "article, .content, main"
        }
    };

    private static readonly HtmlSourceParserConfig DefaultConfig = new();

    public HtmlIngestionAdapter(IHtmlFetcher fetcher, ILogger<HtmlIngestionAdapter> logger)
    {
        _fetcher = fetcher;
        _logger = logger;
    }

    public async Task<List<DiscoveredItemDto>> FetchItemsAsync(Source source, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(source.FeedUrl))
        {
            _logger.LogDebug("Source {SourceId} ({SourceName}) has FeedUrl — skipping HTML ingestion",
                source.Id, source.Name);
            return [];
        }

        var html = await _fetcher.FetchHtmlAsync(source.BaseUrl, ct);
        if (html is null)
        {
            _logger.LogWarning("Failed to fetch listing page for source {SourceId} ({SourceName}) at {BaseUrl}",
                source.Id, source.Name, source.BaseUrl);
            return [];
        }

        try
        {
            var config = GetConfigForSource(source);
            return ParseListingPage(html, source, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse HTML for source {SourceId} ({SourceName})",
                source.Id, source.Name);
            return [];
        }
    }

    internal HtmlSourceParserConfig GetConfigForSource(Source source)
    {
        if (Uri.TryCreate(source.BaseUrl, UriKind.Absolute, out var uri)
            && SourceConfigs.TryGetValue(uri.Host, out var config))
        {
            return config;
        }
        return DefaultConfig;
    }

    /// <summary>
    /// Returns the parser config matching the host of the given URL, or the default config.
    /// Used by the demo pipeline to extract content from a specific URL.
    /// </summary>
    internal static HtmlSourceParserConfig GetParserConfigForHost(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && SourceConfigs.TryGetValue(uri.Host, out var config))
        {
            return config;
        }
        return DefaultConfig;
    }

    internal List<DiscoveredItemDto> ParseListingPage(string html, Source source, HtmlSourceParserConfig config)
    {
        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);

        var links = document.QuerySelectorAll(config.ArticleLinkSelector);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<DiscoveredItemDto>();

        Uri.TryCreate(source.BaseUrl, UriKind.Absolute, out var baseUri);
        Regex? linkPattern = config.ArticleLinkPattern is not null
            ? new Regex(config.ArticleLinkPattern, RegexOptions.IgnoreCase)
            : null;

        foreach (var link in links)
        {
            var href = link.GetAttribute("href")?.Trim();
            if (string.IsNullOrEmpty(href) || href.StartsWith('#')) continue;

            var resolvedUrl = ResolveUrl(href, baseUri);
            if (resolvedUrl is null) continue;

            if (linkPattern is not null && !linkPattern.IsMatch(resolvedUrl))
                continue;

            var normalized = RssIngestionAdapter.NormalizeUrl(resolvedUrl);
            if (!seen.Add(normalized)) continue;

            var title = link.TextContent?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title)) continue;

            result.Add(new DiscoveredItemDto
            {
                SourceId = source.Id,
                OriginalUrl = resolvedUrl,
                Title = title,
                RawContent = null,
                PublishedAt = null
            });
        }

        return result;
    }

    /// <summary>
    /// Extracts the main text content from an HTML page using CSS selectors.
    /// Tries each selector in order, falls back to body text.
    /// </summary>
    internal static string? ExtractContent(string html, HtmlSourceParserConfig config)
    {
        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);

        foreach (var selector in config.ContentSelector.Split(',', StringSplitOptions.TrimEntries))
        {
            var element = document.QuerySelector(selector);
            if (element is not null)
            {
                var text = element.TextContent?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        return document.Body?.TextContent?.Trim();
    }

    /// <summary>
    /// Extracts the title from an HTML page using CSS selectors.
    /// Tries each selector in order, falls back to page title.
    /// </summary>
    internal static string? ExtractTitle(string html, HtmlSourceParserConfig config)
    {
        var parser = new HtmlParser();
        using var document = parser.ParseDocument(html);

        foreach (var selector in config.TitleSelector.Split(',', StringSplitOptions.TrimEntries))
        {
            var element = document.QuerySelector(selector);
            if (element is not null)
            {
                var text = element.TextContent?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        return document.Title?.Trim();
    }

    internal static string? ResolveUrl(string href, Uri? baseUri)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (baseUri is not null && Uri.TryCreate(baseUri, href, out var resolved))
            return resolved.ToString();

        return null;
    }
}
