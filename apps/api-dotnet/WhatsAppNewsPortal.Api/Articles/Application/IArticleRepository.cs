using WhatsAppNewsPortal.Api.Articles.Domain;

namespace WhatsAppNewsPortal.Api.Articles.Application;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsBySourceItemIdAsync(Guid sourceItemId, CancellationToken ct = default);
    Task AddAsync(Article article, CancellationToken ct = default);
    Task UpdateAsync(Article article, CancellationToken ct = default);
}
