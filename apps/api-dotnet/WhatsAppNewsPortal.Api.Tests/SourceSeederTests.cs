using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Sources.Infrastructure;

namespace WhatsAppNewsPortal.Api.Tests;

public class SourceSeederTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Seed_Creates_Four_Sources()
    {
        await using var context = CreateContext();

        await SourceSeeder.SeedAsync(context);

        var count = await context.Sources.CountAsync();
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Seed_Is_Idempotent()
    {
        await using var context = CreateContext();

        await SourceSeeder.SeedAsync(context);
        await SourceSeeder.SeedAsync(context);
        await SourceSeeder.SeedAsync(context);

        var count = await context.Sources.CountAsync();
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Seed_WABetaInfo_Is_BetaSpecialized()
    {
        await using var context = CreateContext();

        await SourceSeeder.SeedAsync(context);

        var wabetainfo = await context.Sources
            .FirstOrDefaultAsync(s => s.BaseUrl == "https://wabetainfo.com");

        Assert.NotNull(wabetainfo);
        Assert.Equal("WABetaInfo", wabetainfo.Name);
        Assert.Equal(SourceType.BetaSpecialized, wabetainfo.Type);
        Assert.True(wabetainfo.IsActive);
    }

    [Fact]
    public async Task Seed_Official_Sources_Are_Official()
    {
        await using var context = CreateContext();

        await SourceSeeder.SeedAsync(context);

        var officialSources = await context.Sources
            .Where(s => s.Type == SourceType.Official)
            .ToListAsync();

        Assert.Equal(3, officialSources.Count);
        Assert.All(officialSources, s => Assert.Equal(SourceType.Official, s.Type));
    }

    [Fact]
    public async Task Seed_WhatsAppBlog_Has_FeedUrl()
    {
        await using var context = CreateContext();

        await SourceSeeder.SeedAsync(context);

        var blog = await context.Sources
            .FirstOrDefaultAsync(s => s.BaseUrl == "https://blog.whatsapp.com");

        Assert.NotNull(blog);
        Assert.NotNull(blog.FeedUrl);
        Assert.Equal(SourceType.Official, blog.Type);
    }

    [Fact]
    public async Task Seed_All_Sources_Are_Active()
    {
        await using var context = CreateContext();

        await SourceSeeder.SeedAsync(context);

        var inactive = await context.Sources
            .Where(s => !s.IsActive)
            .CountAsync();

        Assert.Equal(0, inactive);
    }
}
