namespace WhatsAppNewsPortal.Api.Pipeline.Application;

/// <summary>
/// Configurações do background job do pipeline.
/// Variáveis facilmente modificáveis para controle do agendamento e filtragem de conteúdo.
/// </summary>
public class PipelineJobSettings
{
    // ============================================================
    // VARIÁVEIS DE CONFIGURAÇÃO DO JOB
    // ============================================================

    /// <summary>
    /// Intervalo entre execuções do pipeline em minutos.
    /// Desenvolvimento: 5 (a cada 5 minutos para testes)
    /// Produção: 720 (a cada 12 horas)
    /// Env var: PIPELINE_INTERVAL_MINUTES
    /// </summary>
    public int IntervalMinutes { get; set; } = 720;

    /// <summary>
    /// Se true, executa o pipeline uma vez assim que a API inicia.
    /// Env var: PIPELINE_RUN_ON_STARTUP
    /// </summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Data mínima de publicação dos posts a serem buscados (UTC).
    /// Posts com PublishedAt anterior a esta data são ignorados.
    /// Posts sem data de publicação são processados normalmente.
    /// Formato: yyyy-MM-dd
    /// Env var: PIPELINE_MIN_DATE
    /// </summary>
    public DateTime MinPublishedDate { get; set; } = new(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Se true, publica automaticamente os drafts gerados pelo job.
    /// Env var: PIPELINE_AUTO_PUBLISH
    /// </summary>
    public bool AutoPublishDrafts { get; set; } = true;
}
