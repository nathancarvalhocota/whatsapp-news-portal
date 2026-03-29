using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.Articles.Infrastructure;

/// <summary>
/// Validates a GeneratedArticleDto before persisting as a draft Article.
/// </summary>
public static class ArticleValidator
{
    public static List<string> Validate(GeneratedArticleDto article, EditorialType editorialType)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(article.Title))
            errors.Add("Title is required");

        if (string.IsNullOrWhiteSpace(article.Excerpt))
            errors.Add("Excerpt is required");

        if (string.IsNullOrWhiteSpace(article.ContentHtml))
            errors.Add("ContentHtml is required");
        else if (!article.ContentHtml.Contains('<'))
            errors.Add("ContentHtml must contain HTML markup");

        if (editorialType == EditorialType.BetaNews &&
            string.IsNullOrWhiteSpace(article.BetaDisclaimer))
            errors.Add("BetaDisclaimer is required for BetaNews articles");

        return errors;
    }

    /// <summary>
    /// Trims whitespace from text fields.
    /// </summary>
    public static void Sanitize(GeneratedArticleDto article)
    {
        article.Title = article.Title?.Trim() ?? string.Empty;
        article.Excerpt = article.Excerpt?.Trim() ?? string.Empty;
        article.ContentHtml = article.ContentHtml?.Trim() ?? string.Empty;
        article.BetaDisclaimer = article.BetaDisclaimer?.Trim();

        if (string.IsNullOrWhiteSpace(article.BetaDisclaimer))
            article.BetaDisclaimer = null;
    }
}
