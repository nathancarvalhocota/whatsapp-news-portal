using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Result of the Gemini Flash-Lite classification step.
/// Contains all editorial metadata needed to generate or discard the article.
/// Items from beta_specialized sources must always yield EditorialType = BetaNews.
/// </summary>
public class ClassificationResultDto
{
    /// <summary>Whether the item is relevant enough to generate an article.</summary>
    public bool IsRelevant { get; set; }

    /// <summary>Reason for discarding, when IsRelevant is false.</summary>
    public string? DiscardReason { get; set; }

    /// <summary>Editorial type derived from source and content context.</summary>
    public EditorialType EditorialType { get; set; }

    /// <summary>Suggested article title in PT-BR.</summary>
    public string SuggestedTitle { get; set; } = string.Empty;

    /// <summary>URL slug derived from the title.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>SEO meta title (may differ from article title).</summary>
    public string MetaTitle { get; set; } = string.Empty;

    /// <summary>SEO meta description (up to 160 chars recommended).</summary>
    public string MetaDescription { get; set; } = string.Empty;

    /// <summary>Short excerpt summarizing the article content in PT-BR.</summary>
    public string Excerpt { get; set; } = string.Empty;

    /// <summary>Content tags for categorization.</summary>
    public string[] Tags { get; set; } = [];

    /// <summary>Brief editorial observation for the article writer (e.g. beta caveats).</summary>
    public string? EditorialNote { get; set; }
}
