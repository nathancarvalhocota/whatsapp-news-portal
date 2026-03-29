using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Pipeline.Application;
using Microsoft.EntityFrameworkCore;

namespace WhatsAppNewsPortal.Api.Pipeline.Infrastructure;

/// <summary>
/// Background job que executa o pipeline de descoberta e geração de artigos periodicamente.
/// Roda uma vez no startup (se configurado) e depois no intervalo definido em PipelineJobSettings.
/// O singleton PipelineJobSettings é compartilhado com os endpoints /api/settings/pipeline,
/// permitindo alteração em runtime.
/// </summary>
public class ContentPipelineJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PipelineJobSettings _settings;
    private readonly ILogger<ContentPipelineJob> _logger;

    public ContentPipelineJob(
        IServiceProvider serviceProvider,
        PipelineJobSettings settings,
        ILogger<ContentPipelineJob> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[PipelineJob] Iniciado — Intervalo: {Interval}min, RunOnStartup: {RunOnStartup}, " +
            "MinDate: {MinDate:yyyy-MM-dd}, AutoPublish: {AutoPublish}",
            _settings.IntervalMinutes, _settings.RunOnStartup,
            _settings.MinPublishedDate, _settings.AutoPublishDrafts);

        if (_settings.RunOnStartup)
        {
            // Aguarda 5 segundos para migrations/seed completarem
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await RunPipelineSafeAsync(stoppingToken);
        }

        var interval = TimeSpan.FromMinutes(_settings.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[PipelineJob] Próxima execução em {Interval} minutos", _settings.IntervalMinutes);
            await Task.Delay(interval, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await RunPipelineSafeAsync(stoppingToken);
            }
        }
    }

    private async Task RunPipelineSafeAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[PipelineJob] Iniciando execução do pipeline...");

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IPipelineOrchestrator>();

            var result = await orchestrator.RunAsync(ct);

            _logger.LogInformation(
                "[PipelineJob] Pipeline concluído — Fontes: {Sources}, Descobertos: {Discovered}, " +
                "Drafts: {Drafts}, Erros: {Errors}",
                result.SourcesProcessed, result.ItemsDiscovered,
                result.DraftsGenerated, result.Errors.Count);

            // Auto-publicar drafts gerados
            if (_settings.AutoPublishDrafts)
            {
                await AutoPublishDraftsAsync(scope.ServiceProvider, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[PipelineJob] Execução cancelada (shutdown)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PipelineJob] Erro fatal na execução do pipeline: {Message}", ex.Message);
        }
    }

    private async Task AutoPublishDraftsAsync(IServiceProvider scopedProvider, CancellationToken ct)
    {
        try
        {
            var db = scopedProvider.GetRequiredService<AppDbContext>();
            var publisher = scopedProvider.GetRequiredService<IArticlePublisher>();

            var drafts = await db.Articles
                .Where(a => a.Status == PipelineStatus.Draft)
                .Select(a => a.Id)
                .ToListAsync(ct);

            if (drafts.Count == 0)
            {
                _logger.LogInformation("[PipelineJob] Nenhum draft pendente para publicar");
                return;
            }

            _logger.LogInformation("[PipelineJob] Publicando {Count} draft(s)...", drafts.Count);

            var published = 0;
            foreach (var articleId in drafts)
            {
                try
                {
                    await publisher.PublishAsync(articleId, ct);
                    published++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[PipelineJob] Falha ao publicar draft {ArticleId}: {Message}",
                        articleId, ex.Message);
                }
            }

            _logger.LogInformation("[PipelineJob] {Published}/{Total} draft(s) publicado(s)", published, drafts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PipelineJob] Erro ao auto-publicar drafts: {Message}", ex.Message);
        }
    }
}
