using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Sources.Infrastructure;

public class EfSourceRepository(AppDbContext db) : ISourceRepository
{
    public async Task<List<Source>> GetActiveSourcesAsync(CancellationToken ct = default)
        => await db.Sources.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<Source?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Sources.FindAsync([id], ct);
}
