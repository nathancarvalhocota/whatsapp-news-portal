namespace WhatsAppNewsPortal.Api.Pipeline.Application;

/// <summary>
/// Configurações do background job do pipeline.
/// Registrado como singleton mutável — alterações via /api/settings/pipeline
/// são captadas pelo ContentPipelineJob no próximo ciclo.
/// </summary>
public class PipelineJobSettings
{
    /// <summary>
    /// Intervalo entre execuções do pipeline em minutos.
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
    /// Formato: yyyy-MM-dd
    /// Env var: PIPELINE_MIN_DATE
    /// </summary>
    public DateTime MinPublishedDate { get; set; } = new(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Se true, publica automaticamente os drafts gerados pelo job.
    /// Env var: PIPELINE_AUTO_PUBLISH
    /// </summary>
    public bool AutoPublishDrafts { get; set; } = true;

    // ============================================================
    // Sinalização para interromper o delay do ContentPipelineJob
    // quando settings são alteradas via admin em runtime.
    // ============================================================
    private CancellationTokenSource _delayCts = new();

    /// <summary>Token que o job usa no Task.Delay — é cancelado por InterruptDelay().</summary>
    public CancellationToken DelayCancellationToken => _delayCts.Token;

    /// <summary>
    /// Cancela o delay em andamento do job para que ele reinicie o ciclo
    /// com os novos valores de configuração.
    /// </summary>
    public void InterruptDelay()
    {
        var old = Interlocked.Exchange(ref _delayCts, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}
