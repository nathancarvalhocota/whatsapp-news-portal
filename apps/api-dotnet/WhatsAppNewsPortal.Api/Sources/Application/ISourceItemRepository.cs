using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Sources.Application;

public interface ISourceItemRepository
{
    Task<SourceItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByUrlAsync(string originalUrl, CancellationToken ct = default);
    Task<bool> ExistsByCanonicalUrlAsync(string canonicalUrl, CancellationToken ct = default);
    Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken ct = default);
    Task AddAsync(SourceItem item, CancellationToken ct = default);
    Task UpdateAsync(SourceItem item, CancellationToken ct = default);
    Task DeleteAsync(SourceItem item, CancellationToken ct = default);
}
