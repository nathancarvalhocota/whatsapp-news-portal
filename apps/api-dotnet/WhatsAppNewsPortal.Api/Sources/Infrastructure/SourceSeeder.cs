using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Sources.Infrastructure;

public static class SourceSeeder
{
    private static readonly Source[] KnownSources =
    [
        new Source
        {
            Id = new Guid("11111111-0000-0000-0000-000000000001"),
            Name = "WhatsApp Blog",
            Type = SourceType.Official,
            BaseUrl = "https://blog.whatsapp.com",
            FeedUrl = "https://blog.whatsapp.com/rss",
            IsActive = true
        },
        new Source
        {
            Id = new Guid("11111111-0000-0000-0000-000000000002"),
            Name = "WhatsApp Business Blog",
            Type = SourceType.Official,
            BaseUrl = "https://business.whatsapp.com/blog",
            FeedUrl = null,
            IsActive = true
        },
        new Source
        {
            Id = new Guid("11111111-0000-0000-0000-000000000003"),
            Name = "WhatsApp API Documentation",
            Type = SourceType.Official,
            BaseUrl = "https://developers.facebook.com/blog",
            FeedUrl = null,
            IsActive = true
        },
        new Source
        {
            Id = new Guid("11111111-0000-0000-0000-000000000004"),
            Name = "WABetaInfo",
            Type = SourceType.BetaSpecialized,
            BaseUrl = "https://wabetainfo.com",
            FeedUrl = "https://wabetainfo.com/feed/",
            IsActive = true
        }
    ];

    /// <summary>
    /// Insere as fontes padrão do sistema caso ainda não existam (operação idempotente).
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;

        foreach (var source in KnownSources)
        {
            var exists = await context.Sources.AnyAsync(s => s.BaseUrl == source.BaseUrl);
            if (!exists)
            {
                context.Sources.Add(new Source
                {
                    Id = source.Id,
                    Name = source.Name,
                    Type = source.Type,
                    BaseUrl = source.BaseUrl,
                    FeedUrl = source.FeedUrl,
                    IsActive = source.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
