namespace WhatsAppNewsPortal.Api.Articles.Application;

/// <summary>
/// Transitions a draft article to Published status.
/// Publication is idempotent: publishing an already-published article returns its existing data.
/// </summary>
public interface IArticlePublisher
{
    /// <exception cref="InvalidOperationException">Thrown when the article does not exist or is not in a publishable state.</exception>
    Task<PublishArticleResultDto> PublishAsync(Guid articleId, CancellationToken ct = default);
}
