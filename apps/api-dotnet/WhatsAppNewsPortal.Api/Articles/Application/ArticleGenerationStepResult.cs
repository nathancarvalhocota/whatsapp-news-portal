namespace WhatsAppNewsPortal.Api.Articles.Application;

/// <summary>
/// Result of the article generation pipeline step.
/// On success, contains the draft Article ID and slug.
/// </summary>
public class ArticleGenerationStepResult
{
    public bool Success { get; init; }
    public Guid? ArticleId { get; init; }
    public string? Slug { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> ValidationErrors { get; init; } = [];

    public static ArticleGenerationStepResult Created(Guid articleId, string slug) =>
        new() { Success = true, ArticleId = articleId, Slug = slug };

    public static ArticleGenerationStepResult Failed(string error, List<string>? validationErrors = null) =>
        new() { Success = false, ErrorMessage = error, ValidationErrors = validationErrors ?? [] };
}
