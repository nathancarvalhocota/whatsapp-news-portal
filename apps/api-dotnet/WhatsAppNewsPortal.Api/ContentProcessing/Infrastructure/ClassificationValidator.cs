using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;

/// <summary>
/// Validates a ClassificationResultDto to ensure all required fields are present
/// and well-formed before proceeding to article generation.
/// </summary>
public static class ClassificationValidator
{
    public static List<string> Validate(ClassificationResultDto result)
    {
        var errors = new List<string>();

        if (result.IsRelevant)
        {
            if (string.IsNullOrWhiteSpace(result.SuggestedTitle))
                errors.Add("SuggestedTitle is required for relevant items");

            if (string.IsNullOrWhiteSpace(result.Slug))
                errors.Add("Slug is required for relevant items");

            if (string.IsNullOrWhiteSpace(result.MetaTitle))
                errors.Add("MetaTitle is required for relevant items");

            if (string.IsNullOrWhiteSpace(result.MetaDescription))
                errors.Add("MetaDescription is required for relevant items");

            if (string.IsNullOrWhiteSpace(result.Excerpt))
                errors.Add("Excerpt is required for relevant items");

            if (result.Tags.Length == 0)
                errors.Add("At least one tag is required for relevant items");

            if (result.EditorialType == EditorialType.BetaNews &&
                string.IsNullOrWhiteSpace(result.EditorialNote))
                errors.Add("EditorialNote is required for BetaNews items");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(result.DiscardReason))
                errors.Add("DiscardReason is required when IsRelevant is false");
        }

        return errors;
    }

    /// <summary>
    /// Sanitizes common issues in the classification output before validation.
    /// Normalizes the slug and trims whitespace from text fields.
    /// </summary>
    public static void Sanitize(ClassificationResultDto result)
    {
        result.SuggestedTitle = result.SuggestedTitle?.Trim() ?? string.Empty;
        result.MetaTitle = result.MetaTitle?.Trim() ?? string.Empty;
        result.MetaDescription = result.MetaDescription?.Trim() ?? string.Empty;
        result.Excerpt = result.Excerpt?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(result.Slug))
            result.Slug = SanitizeSlug(result.Slug);

        result.Tags = result.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();
    }

    internal static string SanitizeSlug(string slug)
    {
        slug = slug.Trim().ToLowerInvariant();
        slug = slug.Replace(' ', '-');
        // Collapse consecutive hyphens
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
