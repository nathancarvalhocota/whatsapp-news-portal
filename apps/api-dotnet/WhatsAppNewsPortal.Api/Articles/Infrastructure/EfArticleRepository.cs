using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Infrastructure.Data;

namespace WhatsAppNewsPortal.Api.Articles.Infrastructure;

public class EfArticleRepository : IArticleRepository
{
    private readonly AppDbContext _db;

    public EfArticleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Articles
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Articles
            .Include(a => a.SourceReferences)
            .FirstOrDefaultAsync(a => a.Slug == slug, ct);
    }

    public async Task<List<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Articles
            .Where(a => a.Status == PipelineStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<Article>> GetByCategoryAsync(string category, int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Articles
            .Where(a => a.Status == PipelineStatus.Published && a.Category == category)
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsBySourceItemIdAsync(Guid sourceItemId, CancellationToken ct = default)
    {
        return await _db.Articles
            .AnyAsync(a => a.SourceItemId == sourceItemId, ct);
    }

    public async Task AddAsync(Article article, CancellationToken ct = default)
    {
        _db.Articles.Add(article);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Article article, CancellationToken ct = default)
    {
        _db.Articles.Update(article);
        await _db.SaveChangesAsync(ct);
    }
}
