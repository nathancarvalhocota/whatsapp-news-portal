using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

public interface IProcessingLogRepository
{
    Task AddAsync(ProcessingLog log, CancellationToken ct = default);
}
