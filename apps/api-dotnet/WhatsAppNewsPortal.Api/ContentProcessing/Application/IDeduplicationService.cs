namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

public record DeduplicationResult(bool IsDuplicate, string? Reason);

public interface IDeduplicationService
{
    /// <summary>
    /// Checks whether a source item with the same canonicalUrl or contentHash already exists.
    /// At least one non-empty value must be provided.
    /// </summary>
    Task<DeduplicationResult> CheckSourceItemAsync(
        string? canonicalUrl,
        string? contentHash,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether an article linked to the given sourceItemId already exists.
    /// </summary>
    Task<bool> ArticleExistsForSourceItemAsync(Guid sourceItemId, CancellationToken ct = default);
}
