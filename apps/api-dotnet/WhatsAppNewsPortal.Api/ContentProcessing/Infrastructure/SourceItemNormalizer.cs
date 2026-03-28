using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;

public partial class SourceItemNormalizer : IContentProcessor
{
    private readonly ISourceItemRepository _repository;
    private readonly ILogger<SourceItemNormalizer> _logger;

    public SourceItemNormalizer(ISourceItemRepository repository, ILogger<SourceItemNormalizer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<NormalizedItemDto> NormalizeAsync(SourceItem item, CancellationToken ct = default)
    {
        item.Status = PipelineStatus.Processing;
        item.UpdatedAt = DateTime.UtcNow;

        var canonicalUrl = CanonicalizeUrl(item.OriginalUrl);
        item.CanonicalUrl = canonicalUrl;

        var cleanedContent = CleanText(item.RawContent);
        var cleanedTitle = CleanText(item.Title)?.Trim() ?? string.Empty;

        var validationError = Validate(cleanedTitle, cleanedContent, canonicalUrl);
        if (validationError is not null)
        {
            item.Status = PipelineStatus.Failed;
            item.ErrorMessage = validationError;
            await _repository.UpdateAsync(item, ct);
            _logger.LogWarning("SourceItem {Id} failed normalization: {Error}", item.Id, validationError);

            return new NormalizedItemDto
            {
                SourceItemId = item.Id,
                SourceId = item.SourceId,
                OriginalUrl = item.OriginalUrl,
                CanonicalUrl = canonicalUrl,
                Title = cleanedTitle,
                NormalizedContent = string.Empty,
                ContentHash = string.Empty,
                PublishedAt = item.PublishedAt,
                SourceType = item.Source?.Type ?? SourceType.Official,
                IsDemoItem = item.IsDemoItem
            };
        }

        item.NormalizedContent = cleanedContent;
        item.ContentHash = ComputeHash(cleanedContent!);

        await _repository.UpdateAsync(item, ct);
        _logger.LogInformation("SourceItem {Id} normalized successfully (hash={Hash})", item.Id, item.ContentHash);

        return new NormalizedItemDto
        {
            SourceItemId = item.Id,
            SourceId = item.SourceId,
            OriginalUrl = item.OriginalUrl,
            CanonicalUrl = canonicalUrl,
            Title = cleanedTitle,
            NormalizedContent = cleanedContent!,
            ContentHash = item.ContentHash,
            PublishedAt = item.PublishedAt,
            SourceType = item.Source?.Type ?? SourceType.Official,
            IsDemoItem = item.IsDemoItem
        };
    }

    internal static string CanonicalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.ToLowerInvariant();

        // Lowercase scheme and host, remove fragment, remove default ports
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty
        };

        if ((uri.Scheme == "http" && uri.Port == 80) ||
            (uri.Scheme == "https" && uri.Port == 443))
        {
            builder.Port = -1;
        }

        // Remove trailing slash from path (unless path is just "/")
        var path = builder.Path;
        if (path.Length > 1 && path.EndsWith('/'))
            builder.Path = path.TrimEnd('/');

        // Sort query parameters for consistency
        if (!string.IsNullOrEmpty(uri.Query) && uri.Query.Length > 1)
        {
            var queryParams = uri.Query[1..]
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Order()
                .ToArray();
            builder.Query = string.Join("&", queryParams);
        }

        return builder.Uri.AbsoluteUri;
    }

    internal static string? CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Strip HTML tags
        var cleaned = HtmlTagRegex().Replace(text, " ");

        // Decode common HTML entities
        cleaned = cleaned
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&nbsp;", " ");

        // Normalize whitespace: collapse multiple spaces/tabs/newlines into single space
        cleaned = ExcessWhitespaceRegex().Replace(cleaned, " ");

        return cleaned.Trim();
    }

    internal static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }

    private static string? Validate(string? title, string? content, string canonicalUrl)
    {
        if (string.IsNullOrWhiteSpace(canonicalUrl))
            return "URL is empty or invalid";

        if (string.IsNullOrWhiteSpace(title))
            return "Title is empty after cleaning";

        if (string.IsNullOrWhiteSpace(content))
            return "Content is empty after cleaning";

        if (content.Length < 20)
            return $"Content too short ({content.Length} chars) — likely not a real article";

        return null;
    }

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex ExcessWhitespaceRegex();
}
