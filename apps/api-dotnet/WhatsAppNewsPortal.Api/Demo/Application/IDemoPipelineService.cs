namespace WhatsAppNewsPortal.Api.Demo.Application;

public interface IDemoPipelineService
{
    Task RunDemoPipelineAsync(CancellationToken ct = default);
}
