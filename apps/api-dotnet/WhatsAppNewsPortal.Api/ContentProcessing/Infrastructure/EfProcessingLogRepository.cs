using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Infrastructure.Data;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;

public class EfProcessingLogRepository : IProcessingLogRepository
{
    private readonly AppDbContext _db;

    public EfProcessingLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ProcessingLog log, CancellationToken ct = default)
    {
        _db.ProcessingLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
