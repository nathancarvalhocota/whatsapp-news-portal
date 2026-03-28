using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Sources.Application;

public interface ISourceRepository
{
    Task<List<Source>> GetActiveSourcesAsync(CancellationToken ct = default);
    Task<Source?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
