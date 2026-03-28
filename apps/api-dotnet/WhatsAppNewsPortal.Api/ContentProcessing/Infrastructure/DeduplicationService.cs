using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Sources.Application;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;

public class DeduplicationService : IDeduplicationService
{
    private readonly ISourceItemRepository _sourceItemRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly ILogger<DeduplicationService> _logger;

    public DeduplicationService(
        ISourceItemRepository sourceItemRepository,
        IArticleRepository articleRepository,
        ILogger<DeduplicationService> logger)
    {
        _sourceItemRepository = sourceItemRepository;
        _articleRepository = articleRepository;
        _logger = logger;
    }

    public async Task<DeduplicationResult> CheckSourceItemAsync(
        string? canonicalUrl,
        string? contentHash,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(canonicalUrl))
        {
            var urlExists = await _sourceItemRepository.ExistsByCanonicalUrlAsync(canonicalUrl, ct);
            if (urlExists)
            {
                _logger.LogDebug("Duplicate source item detected by canonicalUrl: {Url}", canonicalUrl);
                return new DeduplicationResult(true, $"SourceItem with canonicalUrl '{canonicalUrl}' already exists");
            }
        }

        if (!string.IsNullOrWhiteSpace(contentHash))
        {
            var hashExists = await _sourceItemRepository.ExistsByContentHashAsync(contentHash, ct);
            if (hashExists)
            {
                _logger.LogDebug("Duplicate source item detected by contentHash: {Hash}", contentHash);
                return new DeduplicationResult(true, $"SourceItem with contentHash '{contentHash}' already exists");
            }
        }

        return new DeduplicationResult(false, null);
    }

    public async Task<bool> ArticleExistsForSourceItemAsync(Guid sourceItemId, CancellationToken ct = default)
    {
        var exists = await _articleRepository.ExistsBySourceItemIdAsync(sourceItemId, ct);
        if (exists)
            _logger.LogDebug("Article already exists for SourceItemId: {SourceItemId}", sourceItemId);
        return exists;
    }
}
