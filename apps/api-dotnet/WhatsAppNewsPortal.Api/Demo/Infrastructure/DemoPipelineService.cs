using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Demo.Application;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Demo.Infrastructure;

public class DemoPipelineService(
    AppDbContext db,
    ISourceRepository sourceRepository,
    ISourceItemRepository sourceItemRepository,
    IHtmlFetcher htmlFetcher,
    IContentProcessor contentProcessor,
    IClassificationStep classificationStep,
    IArticleGenerationStep articleGenerationStep,
    IProcessingLogRepository processingLogRepository,
    ILogger<DemoPipelineService> logger) : IDemoPipelineService
{

    public async Task<DemoPipelineResultDto> RunDemoAsync(DemoPipelineRequest request, CancellationToken ct = default)
    {
        var result = new DemoPipelineResultDto();
        var url = request.Url?.Trim();
        result.Url = string.IsNullOrWhiteSpace(request.Url) ? "" : request.Url.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            result.ErrorMessage = "URL is required. Provide a URL in the request body";
            return result;
        }

        var correlationId = Guid.NewGuid().ToString("N")[..8];
        using var scope = logger.BeginScope("CorrelationId={CorrelationId} Stage={PipelineStage}", correlationId, "demo");

        logger.LogInformation("[Demo] Iniciando demo pipeline correlationId={CorrelationId} url={Url}", correlationId, url);

        try
        {
            // Step 1: Find matching source by domain
            var source = await FindSourceForUrlAsync(url, ct);
            if (source is null)
            {
                result.ErrorMessage = $"No active source found matching URL domain: {url}";
                return result;
            }
            result.SourceName = source.Name;
            result.Steps.Add($"Source matched: {source.Name} ({source.Type})");

            // Step 2: Reset previous demo data if requested
            if (request.Reset)
            {
                var resetCount = await ResetDemoDataForUrlAsync(url, ct);
                result.WasReset = true;
                result.Steps.Add($"Previous demo data reset ({resetCount} item(s) removed)");
            }

            // Step 3: Idempotent check — if item already exists and no reset, return existing data
            if (!request.Reset && await sourceItemRepository.ExistsByUrlAsync(url, ct))
            {
                var existing = await db.SourceItems.FirstOrDefaultAsync(si => si.OriginalUrl == url, ct);
                if (existing is not null)
                {
                    result.SourceItemId = existing.Id;
                    result.Status = existing.Status.ToString().ToLowerInvariant();
                    var existingArticle = await db.Articles.FirstOrDefaultAsync(a => a.SourceItemId == existing.Id, ct);
                    if (existingArticle is not null)
                    {
                        result.ArticleId = existingArticle.Id;
                        result.Slug = existingArticle.Slug;
                    }
                    result.Success = true;
                    result.Steps.Add("Item already exists — returning existing data (use reset=true to reprocess)");
                    return result;
                }
            }

            // Step 4: Fetch HTML content from URL
            logger.LogInformation("[Demo] Fetching content from {Url}", url);
            var html = await htmlFetcher.FetchHtmlAsync(url, ct);
            if (html is null)
            {
                result.ErrorMessage = $"Failed to fetch content from URL: {url}";
                return result;
            }
            result.Steps.Add("Content fetched successfully");

            // Step 5: Extract title and content using HTML parser
            var parserConfig = HtmlIngestionAdapter.GetParserConfigForHost(url);
            var title = HtmlIngestionAdapter.ExtractTitle(html, parserConfig) ?? "Demo Article";
            var rawContent = HtmlIngestionAdapter.ExtractContent(html, parserConfig) ?? html;
            result.Steps.Add($"Title extracted: {title}");

            // Step 6: Persist SourceItem with IsDemoItem = true
            var sourceItem = new SourceItem
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                OriginalUrl = url,
                Title = title,
                RawContent = rawContent,
                Status = PipelineStatus.Discovered,
                IsDemoItem = true
            };
            await sourceItemRepository.AddAsync(sourceItem, ct);
            result.SourceItemId = sourceItem.Id;
            result.Steps.Add("SourceItem created (IsDemoItem=true)");
            await LogStepAsync(sourceItem.Id, "demo_ingestion", "success", $"Demo item created: {url}", ct);

            // Step 7: Normalize (real pipeline step)
            logger.LogInformation("[Demo] Normalizing: {Title}", title);
            NormalizedItemDto normalized;
            try
            {
                normalized = await contentProcessor.NormalizeAsync(sourceItem, ct);
            }
            catch (Exception ex)
            {
                sourceItem.Status = PipelineStatus.Failed;
                sourceItem.ErrorMessage = $"[normalization] {ex.Message}";
                await sourceItemRepository.UpdateAsync(sourceItem, ct);
                result.Status = "failed_normalization";
                result.ErrorMessage = ex.Message;
                return result;
            }
            result.Steps.Add("Content normalized");

            // Step 8: Classify (real pipeline step)
            logger.LogInformation("[Demo] Classifying: {Title}", title);
            var classResult = await classificationStep.ExecuteAsync(normalized, ct);
            if (!classResult.Success)
            {
                result.Status = "failed_classification";
                result.ErrorMessage = classResult.ErrorMessage ?? "Classification failed";
                return result;
            }
            if (!classResult.IsRelevant)
            {
                result.Status = "discarded";
                result.ErrorMessage = $"Item classified as not relevant: {classResult.DiscardReason}";
                return result;
            }
            result.Steps.Add("Content classified as relevant");

            // Step 9: Generate draft article (real pipeline step)
            logger.LogInformation("[Demo] Generating article: {Title}", title);
            var genResult = await articleGenerationStep.ExecuteAsync(normalized, classResult.Classification!, ct);
            if (!genResult.Success)
            {
                result.Status = "failed_generation";
                result.ErrorMessage = genResult.ErrorMessage ?? "Article generation failed";
                return result;
            }

            result.ArticleId = genResult.ArticleId;
            result.Slug = genResult.Slug;
            result.Status = "draft";
            result.Success = true;
            result.Steps.Add($"Article draft generated: {genResult.Slug}");

            logger.LogInformation("[Demo] Demo pipeline completed — Article: {Slug} (ID: {Id})",
                genResult.Slug, genResult.ArticleId);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Demo] Unexpected error in demo pipeline for URL: {Url}", url);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private async Task<Source?> FindSourceForUrlAsync(string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var sources = await sourceRepository.GetActiveSourcesAsync(ct);

        // Match by domain (base URL or feed URL)
        foreach (var source in sources)
        {
            if (Uri.TryCreate(source.BaseUrl, UriKind.Absolute, out var sourceUri) &&
                string.Equals(uri.Host, sourceUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            if (source.FeedUrl is not null &&
                Uri.TryCreate(source.FeedUrl, UriKind.Absolute, out var feedUri) &&
                string.Equals(uri.Host, feedUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }
        }

        return null;
    }

    private async Task<int> ResetDemoDataForUrlAsync(string url, CancellationToken ct)
    {
        var demoItems = await db.SourceItems
            .Where(si => si.OriginalUrl == url && si.IsDemoItem)
            .ToListAsync(ct);

        if (demoItems.Count == 0) return 0;

        var itemIds = demoItems.Select(si => si.Id).ToList();

        // Delete associated articles (source references cascade via FK)
        var articles = await db.Articles
            .Where(a => itemIds.Contains(a.SourceItemId))
            .ToListAsync(ct);
        db.Articles.RemoveRange(articles);

        // Delete processing logs
        var logs = await db.ProcessingLogs
            .Where(l => l.SourceItemId.HasValue && itemIds.Contains(l.SourceItemId.Value))
            .ToListAsync(ct);
        db.ProcessingLogs.RemoveRange(logs);

        // Delete source items
        db.SourceItems.RemoveRange(demoItems);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Demo] Reset {Count} demo item(s) for URL: {Url}", demoItems.Count, url);
        return demoItems.Count;
    }

    private async Task LogStepAsync(Guid sourceItemId, string step, string status, string? message, CancellationToken ct)
    {
        await processingLogRepository.AddAsync(new ProcessingLog
        {
            SourceItemId = sourceItemId,
            StepName = step,
            Status = status,
            Message = message
        }, ct);
    }
}
