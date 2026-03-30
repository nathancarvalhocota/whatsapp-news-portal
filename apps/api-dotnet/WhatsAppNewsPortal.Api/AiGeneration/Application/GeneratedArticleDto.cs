namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Result of the Gemini Flash article generation step.
/// Produced by IAiArticleGenerator and used to create the Article draft.
/// </summary>
public class GeneratedArticleDto
{
    /// <summary>Final article title in PT-BR.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Short excerpt/summary of the article in PT-BR.</summary>
    public string Excerpt { get; set; } = string.Empty;

    /// <summary>Full article body in HTML, with proper H2/H3 heading structure.</summary>
    public string ContentHtml { get; set; } = string.Empty;

    /// <summary>
    /// Disclaimer paragraph for beta content.
    /// Must be present when the item is from a beta_specialized source.
    /// Must be null/empty for official sources.
    /// </summary>
    public string? BetaDisclaimer { get; set; }

    /// <summary>Thematic topics chosen by the AI from the allowed list.</summary>
    public List<string>? Topics { get; set; }
}
