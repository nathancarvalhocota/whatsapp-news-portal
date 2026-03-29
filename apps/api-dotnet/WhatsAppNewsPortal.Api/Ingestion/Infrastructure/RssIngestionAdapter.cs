using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Ingestion.Infrastructure;

/// <summary>
/// Fetches items from RSS 2.0 and Atom feeds.
/// Returns an empty list on any failure so a broken source never crashes the pipeline.
/// </summary>
public class RssIngestionAdapter : IIngestionAdapter
{
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";

    private readonly HttpClient _httpClient;
    private readonly ILogger<RssIngestionAdapter> _logger;

    public RssIngestionAdapter(HttpClient httpClient, ILogger<RssIngestionAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DiscoveredItemDto>> FetchItemsAsync(Source source, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(source.FeedUrl))
        {
            _logger.LogWarning("Source {SourceId} ({SourceName}) has no FeedUrl — skipping RSS ingestion",
                source.Id, source.Name);
            return [];
        }

        string xml;
        try
        {
            xml = await _httpClient.GetStringAsync(source.FeedUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch RSS feed for source {SourceId} ({SourceName}) at {FeedUrl}",
                source.Id, source.Name, source.FeedUrl);
            return [];
        }

        try
        {
            return ParseFeed(xml, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse RSS feed for source {SourceId} ({SourceName})",
                source.Id, source.Name);
            return [];
        }
    }

    internal List<DiscoveredItemDto> ParseFeed(string xml, Source source)
    {
        // Alguns feeds RSS retornam '&' não escapado no conteúdo (ex: WhatsApp Blog).
        // XDocument.Parse é strict; substituímos '&' soltos por '&amp;' antes de parsear.
        var sanitized = SanitizeXml(xml);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root;
        if (root is null) return [];

        return root.Name.LocalName switch
        {
            "rss" => ParseRss2(root, source),
            "feed" => ParseAtom(root, source),
            _ => []
        };
    }

    private List<DiscoveredItemDto> ParseRss2(XElement root, Source source)
    {
        var items = root.Element("channel")?.Elements("item") ?? [];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<DiscoveredItemDto>();

        foreach (var item in items)
        {
            var rawLink = item.Element("link")?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(rawLink)) continue;

            var normalized = NormalizeUrl(rawLink);
            if (!seen.Add(normalized)) continue;

            var title = item.Element("title")?.Value?.Trim() ?? string.Empty;
            var description = item.Element("description")?.Value?.Trim();
            var pubDate = ParseDateTime(item.Element("pubDate")?.Value);

            result.Add(new DiscoveredItemDto
            {
                SourceId = source.Id,
                OriginalUrl = rawLink,
                Title = title,
                RawContent = description,
                PublishedAt = pubDate
            });
        }

        return result;
    }

    private List<DiscoveredItemDto> ParseAtom(XElement root, Source source)
    {
        var entries = root.Elements(AtomNs + "entry");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<DiscoveredItemDto>();

        foreach (var entry in entries)
        {
            // Prefer the alternate link; fall back to the first link with href
            var rawLink = entry.Elements(AtomNs + "link")
                .FirstOrDefault(e => e.Attribute("rel")?.Value == "alternate" || e.Attribute("rel") == null)
                ?.Attribute("href")?.Value?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(rawLink)) continue;

            var normalized = NormalizeUrl(rawLink);
            if (!seen.Add(normalized)) continue;

            var title = entry.Element(AtomNs + "title")?.Value?.Trim() ?? string.Empty;
            var summary = entry.Element(AtomNs + "summary")?.Value?.Trim()
                ?? entry.Element(AtomNs + "content")?.Value?.Trim();
            var pubDate = ParseDateTime(
                entry.Element(AtomNs + "published")?.Value
                ?? entry.Element(AtomNs + "updated")?.Value);

            result.Add(new DiscoveredItemDto
            {
                SourceId = source.Id,
                OriginalUrl = rawLink,
                Title = title,
                RawContent = summary,
                PublishedAt = pubDate
            });
        }

        return result;
    }

    /// <summary>
    /// Escapa '&amp;' soltos (não seguidos por entidade XML válida) para tornar o XML parseável.
    /// Cobre o caso comum de feeds com '&amp;' literal no conteúdo/URLs.
    /// </summary>
    internal static string SanitizeXml(string xml)
    {
        // Substitui & não seguido de identificador válido de entidade XML (word chars + ;)
        return System.Text.RegularExpressions.Regex.Replace(xml, @"&(?!(?:[a-zA-Z][a-zA-Z0-9]*|#\d+|#x[0-9a-fA-F]+);)", "&amp;");
    }

    internal static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed.ToLowerInvariant();

        // Lowercase scheme and host; preserve path/query case (significant in most servers)
        return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{uri.PathAndQuery}";
    }

    private static readonly System.Text.RegularExpressions.Regex TzOffsetWithoutColon =
        new(@"([+-]\d{2})(\d{2})$", System.Text.RegularExpressions.RegexOptions.Compiled);

    internal static DateTime? ParseDateTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();

        // 1) Standard parse — handles ISO 8601 (Atom <published>)
        if (DateTimeOffset.TryParse(trimmed, out var dto))
            return dto.UtcDateTime;

        // 2) Normalize +HHMM → +HH:MM (RSS pubDate uses +0000, not +00:00)
        var normalized = TzOffsetWithoutColon.Replace(trimmed, "$1:$2");
        if (DateTimeOffset.TryParse(normalized, out var dto2))
            return dto2.UtcDateTime;

        // 3) RFC 2822: strip optional "Ddd, " prefix — .NET TryParse doesn't handle it
        var stripped = StripDayOfWeekPrefix(normalized);
        if (stripped.Length < normalized.Length && DateTimeOffset.TryParse(stripped, out var dto3))
            return dto3.UtcDateTime;

        return null;
    }

    private static string StripDayOfWeekPrefix(string s)
    {
        // RFC 2822 dates may start with "Mon, " or similar — strip the day-of-week
        var idx = s.IndexOf(", ", StringComparison.Ordinal);
        return idx is >= 1 and <= 4 ? s[(idx + 2)..] : s;
    }
}
