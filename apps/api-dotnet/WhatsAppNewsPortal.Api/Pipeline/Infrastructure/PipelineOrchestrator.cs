using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;
using WhatsAppNewsPortal.Api.Pipeline.Application;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Pipeline.Infrastructure;

public class PipelineOrchestrator(
    ISourceRepository sourceRepository,
    IIngestionAdapter rssAdapter,
    HtmlIngestionAdapter htmlAdapter,
    ISourceItemRepository sourceItemRepository,
    IContentProcessor contentProcessor,
    IClassificationStep classificationStep,
    IArticleGenerationStep articleGenerationStep,
    IProcessingLogRepository processingLogRepository,
    PipelineJobSettings jobSettings,
    ILogger<PipelineOrchestrator> logger) : IPipelineOrchestrator
{
    private readonly DateTime _minPublishedDate = jobSettings.MinPublishedDate;
    public async Task<PipelineRunResultDto> RunAsync(CancellationToken ct = default)
    {
        var result = new PipelineRunResultDto { StartedAt = DateTime.UtcNow };
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        using var scope = logger.BeginScope("CorrelationId={CorrelationId} Stage={PipelineStage}", correlationId, "orchestrator");

        logger.LogInformation("[Pipeline] Iniciando execução do pipeline correlationId={CorrelationId}, minDate={MinDate:yyyy-MM-dd}",
            correlationId, _minPublishedDate);

        // Step 1: Get active sources
        var sources = await sourceRepository.GetActiveSourcesAsync(ct);
        logger.LogInformation("[Pipeline] {Count} fonte(s) ativa(s) encontrada(s)", sources.Count);

        foreach (var source in sources)
        {
            result.SourcesProcessed++;

            try
            {
                await ProcessSourceAsync(source, result, ct);
            }
            catch (Exception ex)
            {
                var error = $"[{source.Name}] Erro fatal ao processar fonte: {ex.Message}";
                logger.LogError(ex, "[Pipeline] {Error}", error);
                result.Errors.Add(error);
            }
        }

        result.FinishedAt = DateTime.UtcNow;
        logger.LogInformation(
            "[Pipeline] Concluído — Fontes: {Sources}, Descobertos: {Discovered}, Normalizados: {Normalized}, " +
            "Dedup: {Dedup}, Classificados: {Classified}, Drafts: {Drafts}, Erros: {Errors}",
            result.SourcesProcessed, result.ItemsDiscovered, result.ItemsNormalized,
            result.ItemsDeduplicated, result.ItemsClassified, result.DraftsGenerated, result.Errors.Count);

        return result;
    }

    private async Task ProcessSourceAsync(Source source, PipelineRunResultDto result, CancellationToken ct)
    {
        logger.LogInformation("[Pipeline][{Source}] Buscando novidades...", source.Name);

        // Step 1: Fetch items using appropriate adapter
        List<DiscoveredItemDto> discovered;
        try
        {
            discovered = source.FeedUrl != null
                ? await rssAdapter.FetchItemsAsync(source, ct)
                : await htmlAdapter.FetchItemsAsync(source, ct);
        }
        catch (Exception ex)
        {
            var error = $"[{source.Name}] Erro na ingestão: {ex.Message}";
            logger.LogWarning(ex, "[Pipeline] {Error}", error);
            result.Errors.Add(error);
            return;
        }

        logger.LogInformation("[Pipeline][{Source}] {Count} item(ns) descoberto(s)", source.Name, discovered.Count);
        result.ItemsDiscovered += discovered.Count;

        foreach (var item in discovered)
        {
            await ProcessItemAsync(source, item, result, ct);
        }
    }

    private async Task ProcessItemAsync(
        Source source, DiscoveredItemDto discovered, PipelineRunResultDto result, CancellationToken ct)
    {
        var itemSummary = new PipelineItemSummary
        {
            SourceName = source.Name,
            Title = discovered.Title
        };
        result.Items.Add(itemSummary);

        try
        {
            // Step 2a: Filtro por data mínima de publicação.
            // Posts com PublishedAt anterior à data limite são ignorados.
            // Posts sem data (ex: HTML adapter) são processados normalmente.
            if (discovered.PublishedAt.HasValue && discovered.PublishedAt.Value < _minPublishedDate)
            {
                logger.LogInformation("[Pipeline][{Source}] Item ignorado (anterior a {MinDate:yyyy-MM-dd}): {Title} ({Date:yyyy-MM-dd})",
                    source.Name, _minPublishedDate, discovered.Title, discovered.PublishedAt.Value);
                itemSummary.Status = "skipped_before_min_date";
                return;
            }

            // Step 2b: Dedup por URL original (pré-persistência).
            // Limitação conhecida: não cobre duplicatas por canonical URL ou content hash.
            // Após normalização o item já está no DB, e IDeduplicationService encontra o próprio
            // item como "duplicado". Para resolver seria necessário extrair a canonicalização/hash
            // para métodos estáticos e checar antes de persistir (ver PROGRESS.md, Tarefa 17).
            if (await sourceItemRepository.ExistsByUrlAsync(discovered.OriginalUrl, ct))
            {
                logger.LogInformation("[Pipeline][{Source}] Item já existente (URL): {Url}",
                    source.Name, discovered.OriginalUrl);
                itemSummary.Status = "skipped_duplicate_url";
                result.ItemsDeduplicated++;
                return;
            }

            // Step 3: Persist as SourceItem (Discovered)
            var sourceItem = new SourceItem
            {
                Id = Guid.NewGuid(),
                SourceId = source.Id,
                OriginalUrl = discovered.OriginalUrl,
                Title = discovered.Title,
                RawContent = discovered.RawContent,
                PublishedAt = discovered.PublishedAt,
                Status = PipelineStatus.Discovered
            };
            await sourceItemRepository.AddAsync(sourceItem, ct);
            await LogStepAsync(sourceItem.Id, "ingestion", "success", $"Item persistido: {discovered.OriginalUrl}", ct);

            // Step 4: Normalize
            logger.LogInformation("[Pipeline][{Source}] Normalizando: {Title}", source.Name, discovered.Title);
            NormalizedItemDto normalized;
            try
            {
                normalized = await contentProcessor.NormalizeAsync(sourceItem, ct);
            }
            catch (Exception ex)
            {
                await FailItemAsync(sourceItem, "normalization", ex.Message, itemSummary, result, ct);
                return;
            }
            result.ItemsNormalized++;
            await LogStepAsync(sourceItem.Id, "normalization", "success", null, ct);
            await LogStepAsync(sourceItem.Id, "deduplication", "passed", "Dedup por URL feito pré-persistência", ct);

            // Step 5: Classify
            logger.LogInformation("[Pipeline][{Source}] Classificando: {Title}", source.Name, discovered.Title);
            var classResult = await classificationStep.ExecuteAsync(normalized, ct);
            if (!classResult.Success)
            {
                await FailItemAsync(sourceItem, "classification",
                    classResult.ErrorMessage ?? "Falha na classificação", itemSummary, result, ct);
                return;
            }
            if (!classResult.IsRelevant)
            {
                logger.LogInformation("[Pipeline][{Source}] Item descartado: {Reason}",
                    source.Name, classResult.DiscardReason);
                itemSummary.Status = "discarded";
                itemSummary.Error = classResult.DiscardReason;
                return;
            }
            result.ItemsClassified++;

            // Step 6: Generate draft article
            logger.LogInformation("[Pipeline][{Source}] Gerando artigo: {Title}", source.Name, discovered.Title);
            var genResult = await articleGenerationStep.ExecuteAsync(normalized, classResult.Classification!, ct);
            if (!genResult.Success)
            {
                await FailItemAsync(sourceItem, "article_generation",
                    genResult.ErrorMessage ?? "Falha na geração", itemSummary, result, ct);
                return;
            }

            result.DraftsGenerated++;
            itemSummary.Status = "draft";
            itemSummary.ArticleId = genResult.ArticleId;
            itemSummary.Slug = genResult.Slug;

            logger.LogInformation("[Pipeline][{Source}] Draft gerado: {Slug} (ID: {Id})",
                source.Name, genResult.Slug, genResult.ArticleId);
        }
        catch (Exception ex)
        {
            var error = $"[{source.Name}] Erro ao processar item '{discovered.Title}': {ex.Message}";
            logger.LogError(ex, "[Pipeline] {Error}", error);
            result.Errors.Add(error);
            itemSummary.Status = "error";
            itemSummary.Error = ex.Message;
        }
    }

    private async Task FailItemAsync(
        SourceItem sourceItem, string step, string error,
        PipelineItemSummary summary, PipelineRunResultDto result, CancellationToken ct)
    {
        logger.LogWarning("[Pipeline] Falha em {Step} para item {Id}: {Error}", step, sourceItem.Id, error);
        sourceItem.Status = PipelineStatus.Failed;
        sourceItem.ErrorMessage = $"[{step}] {error}";
        await sourceItemRepository.UpdateAsync(sourceItem, ct);
        await LogStepAsync(sourceItem.Id, step, "failed", error, ct);
        summary.Status = $"failed_{step}";
        summary.Error = error;
        result.Errors.Add($"[{sourceItem.Title}] {step}: {error}");
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
