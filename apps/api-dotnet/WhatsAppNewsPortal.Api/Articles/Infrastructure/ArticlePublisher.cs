using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.Articles.Infrastructure;

public class ArticlePublisher : IArticlePublisher
{
    private readonly IArticleRepository _articleRepo;

    public ArticlePublisher(IArticleRepository articleRepo)
    {
        _articleRepo = articleRepo;
    }

    public async Task<PublishArticleResultDto> PublishAsync(Guid articleId, CancellationToken ct = default)
    {
        var article = await _articleRepo.GetByIdAsync(articleId, ct)
            ?? throw new InvalidOperationException($"Article {articleId} not found.");

        // Idempotent: already published → return existing data
        if (article.Status == PipelineStatus.Published)
        {
            return new PublishArticleResultDto
            {
                ArticleId = article.Id,
                Slug = article.Slug,
                PublishedAt = article.PublishedAt!.Value
            };
        }

        if (article.Status != PipelineStatus.Draft)
            throw new InvalidOperationException($"Article {articleId} is not a draft (status: {article.Status}).");

        var errors = ValidateDraft(article);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Draft {articleId} is not valid for publication: {string.Join("; ", errors)}");

        article.Status = PipelineStatus.Published;
        article.PublishedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;

        await _articleRepo.UpdateAsync(article, ct);

        return new PublishArticleResultDto
        {
            ArticleId = article.Id,
            Slug = article.Slug,
            PublishedAt = article.PublishedAt.Value
        };
    }

    private static List<string> ValidateDraft(Article article)
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

        if (string.IsNullOrWhiteSpace(article.Slug))
            errors.Add("Slug is required");

        if (string.IsNullOrWhiteSpace(article.MetaTitle))
            errors.Add("MetaTitle is required");

        if (string.IsNullOrWhiteSpace(article.MetaDescription))
            errors.Add("MetaDescription is required");

        return errors;
    }
}
