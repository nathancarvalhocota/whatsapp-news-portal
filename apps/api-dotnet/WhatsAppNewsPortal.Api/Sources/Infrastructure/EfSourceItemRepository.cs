using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Sources.Infrastructure;

public class EfSourceItemRepository : ISourceItemRepository
{
    private readonly AppDbContext _db;

    public EfSourceItemRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SourceItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.SourceItems
            .Include(si => si.Source)
            .FirstOrDefaultAsync(si => si.Id == id, ct);
    }

    public async Task<bool> ExistsByUrlAsync(string originalUrl, CancellationToken ct = default)
    {
        return await _db.SourceItems
            .AnyAsync(si => si.OriginalUrl == originalUrl, ct);
    }

    public async Task<bool> ExistsByCanonicalUrlAsync(string canonicalUrl, CancellationToken ct = default)
    {
        return await _db.SourceItems
            .AnyAsync(si => si.CanonicalUrl == canonicalUrl, ct);
    }

    public async Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken ct = default)
    {
        return await _db.SourceItems
            .AnyAsync(si => si.ContentHash == contentHash, ct);
    }

    public async Task AddAsync(SourceItem item, CancellationToken ct = default)
    {
        _db.SourceItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SourceItem item, CancellationToken ct = default)
    {
        _db.SourceItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }
}
