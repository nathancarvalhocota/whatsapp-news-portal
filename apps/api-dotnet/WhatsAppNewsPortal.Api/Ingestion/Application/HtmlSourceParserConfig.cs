namespace WhatsAppNewsPortal.Api.Ingestion.Application;

/// <summary>
/// Per-source CSS selector configuration for HTML parsing.
/// </summary>
public class HtmlSourceParserConfig
{
    /// <summary>CSS selector for article links on the listing page.</summary>
    public string ArticleLinkSelector { get; init; } = "a[href]";

    /// <summary>Regex pattern to filter discovered links (null = accept all).</summary>
    public string? ArticleLinkPattern { get; init; }

    /// <summary>CSS selector for the article title.</summary>
    public string TitleSelector { get; init; } = "h1";

    /// <summary>CSS selector for the main content area.</summary>
    public string ContentSelector { get; init; } = "article, main, .content";

    /// <summary>CSS selector for publication date (optional).</summary>
    public string? DateSelector { get; init; }

    /// <summary>Minimum title character length to accept a link (0 = no minimum).</summary>
    public int MinTitleLength { get; init; } = 15;
}
