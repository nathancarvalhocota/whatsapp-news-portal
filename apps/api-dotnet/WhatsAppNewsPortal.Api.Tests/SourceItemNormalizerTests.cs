using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class SourceItemNormalizerTests
{
    private readonly SourceItemNormalizer _normalizer;
    private readonly FakeSourceItemRepository _repo = new();

    public SourceItemNormalizerTests()
    {
        _normalizer = new SourceItemNormalizer(_repo, NullLogger<SourceItemNormalizer>.Instance);
    }

    // --- URL Canonicalization ---

    [Fact]
    public void CanonicalizeUrl_LowercasesSchemeAndHost()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("HTTPS://WWW.EXAMPLE.COM/Path");
        Assert.Equal("https://www.example.com/Path", result);
    }

    [Fact]
    public void CanonicalizeUrl_RemovesTrailingSlash()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("https://example.com/blog/post/");
        Assert.Equal("https://example.com/blog/post", result);
    }

    [Fact]
    public void CanonicalizeUrl_KeepsRootSlash()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("https://example.com/");
        Assert.Contains("example.com", result);
    }

    [Fact]
    public void CanonicalizeUrl_RemovesFragment()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("https://example.com/page#section");
        Assert.DoesNotContain("#section", result);
    }

    [Fact]
    public void CanonicalizeUrl_RemovesDefaultHttpPort()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("http://example.com:80/page");
        Assert.DoesNotContain(":80", result);
    }

    [Fact]
    public void CanonicalizeUrl_RemovesDefaultHttpsPort()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("https://example.com:443/page");
        Assert.DoesNotContain(":443", result);
    }

    [Fact]
    public void CanonicalizeUrl_KeepsNonDefaultPort()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("https://example.com:8080/page");
        Assert.Contains(":8080", result);
    }

    [Fact]
    public void CanonicalizeUrl_SortsQueryParameters()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("https://example.com/page?z=1&a=2");
        Assert.Contains("a=2&z=1", result);
    }

    [Fact]
    public void CanonicalizeUrl_TrimsWhitespace()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("  https://example.com/page  ");
        Assert.Equal("https://example.com/page", result);
    }

    [Fact]
    public void CanonicalizeUrl_EmptyUrl_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SourceItemNormalizer.CanonicalizeUrl(""));
        Assert.Equal(string.Empty, SourceItemNormalizer.CanonicalizeUrl("   "));
    }

    [Fact]
    public void CanonicalizeUrl_InvalidUri_ReturnsLowercased()
    {
        var result = SourceItemNormalizer.CanonicalizeUrl("not-a-url");
        Assert.Equal("not-a-url", result);
    }

    // --- Content Hash ---

    [Fact]
    public void ComputeHash_ReturnsSha256Hex()
    {
        var hash = SourceItemNormalizer.ComputeHash("hello world");
        // SHA256 of "hello world" is known
        Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", hash);
    }

    [Fact]
    public void ComputeHash_DifferentInputs_DifferentHashes()
    {
        var hash1 = SourceItemNormalizer.ComputeHash("content A");
        var hash2 = SourceItemNormalizer.ComputeHash("content B");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_SameInput_SameHash()
    {
        var hash1 = SourceItemNormalizer.ComputeHash("identical content");
        var hash2 = SourceItemNormalizer.ComputeHash("identical content");
        Assert.Equal(hash1, hash2);
    }

    // --- Text Cleaning ---

    [Fact]
    public void CleanText_RemovesHtmlTags()
    {
        var result = SourceItemNormalizer.CleanText("<p>Hello <b>world</b></p>");
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void CleanText_CollapsesWhitespace()
    {
        var result = SourceItemNormalizer.CleanText("hello    world   test");
        Assert.Equal("hello world test", result);
    }

    [Fact]
    public void CleanText_CollapsesNewlines()
    {
        var result = SourceItemNormalizer.CleanText("hello\n\n\nworld\t\ttab");
        Assert.Equal("hello world tab", result);
    }

    [Fact]
    public void CleanText_DecodesHtmlEntities()
    {
        var result = SourceItemNormalizer.CleanText("A &amp; B &lt; C &gt; D &quot;E&quot; F&#39;s");
        Assert.Equal("A & B < C > D \"E\" F's", result);
    }

    [Fact]
    public void CleanText_TrimsResult()
    {
        var result = SourceItemNormalizer.CleanText("   trimmed   ");
        Assert.Equal("trimmed", result);
    }

    [Fact]
    public void CleanText_NullOrWhitespace_ReturnsNull()
    {
        Assert.Null(SourceItemNormalizer.CleanText(null));
        Assert.Null(SourceItemNormalizer.CleanText(""));
        Assert.Null(SourceItemNormalizer.CleanText("   "));
    }

    [Fact]
    public void CleanText_DecodesNbsp()
    {
        var result = SourceItemNormalizer.CleanText("word1&nbsp;word2");
        Assert.Equal("word1 word2", result);
    }

    // --- NormalizeAsync Integration ---

    [Fact]
    public async Task NormalizeAsync_ValidItem_SetsProcessingStatus()
    {
        var item = CreateValidSourceItem();

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(PipelineStatus.Processing, item.Status);
    }

    [Fact]
    public async Task NormalizeAsync_ValidItem_SetsCanonicalUrl()
    {
        var item = CreateValidSourceItem();
        item.OriginalUrl = "HTTPS://BLOG.WHATSAPP.COM/Article-Title/";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal("https://blog.whatsapp.com/Article-Title", item.CanonicalUrl);
        Assert.Equal(item.CanonicalUrl, dto.CanonicalUrl);
    }

    [Fact]
    public async Task NormalizeAsync_ValidItem_ComputesContentHash()
    {
        var item = CreateValidSourceItem();

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.NotNull(item.ContentHash);
        Assert.NotEmpty(item.ContentHash);
        Assert.Equal(64, item.ContentHash.Length); // SHA256 hex = 64 chars
        Assert.Equal(item.ContentHash, dto.ContentHash);
    }

    [Fact]
    public async Task NormalizeAsync_ValidItem_CleansAndPersistsNormalizedContent()
    {
        var item = CreateValidSourceItem();
        item.RawContent = "<p>Some   <b>raw</b>   content   with   HTML</p>";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal("Some raw content with HTML", item.NormalizedContent);
        Assert.Equal(item.NormalizedContent, dto.NormalizedContent);
    }

    [Fact]
    public async Task NormalizeAsync_ValidItem_PersistsUpdate()
    {
        var item = CreateValidSourceItem();

        await _normalizer.NormalizeAsync(item);

        Assert.Single(_repo.UpdatedItems);
        Assert.Same(item, _repo.UpdatedItems[0]);
    }

    [Fact]
    public async Task NormalizeAsync_ValidItem_ReturnsDtoWithAllFields()
    {
        var item = CreateValidSourceItem();
        item.PublishedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        item.IsDemoItem = true;
        item.Source = new Source { Type = SourceType.BetaSpecialized };

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(item.Id, dto.SourceItemId);
        Assert.Equal(item.SourceId, dto.SourceId);
        Assert.Equal(item.OriginalUrl, dto.OriginalUrl);
        Assert.Equal(item.PublishedAt, dto.PublishedAt);
        Assert.Equal(SourceType.BetaSpecialized, dto.SourceType);
        Assert.True(dto.IsDemoItem);
    }

    [Fact]
    public async Task NormalizeAsync_EmptyContent_SetsFailedStatus()
    {
        var item = CreateValidSourceItem();
        item.RawContent = "";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(PipelineStatus.Failed, item.Status);
        Assert.Contains("Content is empty", item.ErrorMessage);
    }

    [Fact]
    public async Task NormalizeAsync_WhitespaceOnlyContent_SetsFailedStatus()
    {
        var item = CreateValidSourceItem();
        item.RawContent = "     \n\t   ";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(PipelineStatus.Failed, item.Status);
    }

    [Fact]
    public async Task NormalizeAsync_EmptyTitle_SetsFailedStatus()
    {
        var item = CreateValidSourceItem();
        item.Title = "";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(PipelineStatus.Failed, item.Status);
        Assert.Contains("Title is empty", item.ErrorMessage);
    }

    [Fact]
    public async Task NormalizeAsync_TooShortContent_SetsFailedStatus()
    {
        var item = CreateValidSourceItem();
        item.RawContent = "short";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(PipelineStatus.Failed, item.Status);
        Assert.Contains("too short", item.ErrorMessage);
    }

    [Fact]
    public async Task NormalizeAsync_FailedItem_PersistsWithErrorMessage()
    {
        var item = CreateValidSourceItem();
        item.RawContent = null;

        await _normalizer.NormalizeAsync(item);

        Assert.Single(_repo.UpdatedItems);
        Assert.NotNull(item.ErrorMessage);
    }

    [Fact]
    public async Task NormalizeAsync_FailedItem_ReturnsDtoWithEmptyContentAndHash()
    {
        var item = CreateValidSourceItem();
        item.RawContent = null;

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(string.Empty, dto.NormalizedContent);
        Assert.Equal(string.Empty, dto.ContentHash);
    }

    [Fact]
    public async Task NormalizeAsync_HtmlContent_StripsTagsBeforeHashing()
    {
        var item = CreateValidSourceItem();
        item.RawContent = "<div><p>This is the article content for testing purposes.</p></div>";

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.DoesNotContain("<", dto.NormalizedContent);
        Assert.DoesNotContain(">", dto.NormalizedContent);
        Assert.NotEmpty(dto.ContentHash);
    }

    [Fact]
    public async Task NormalizeAsync_SameContent_ProducesSameHash()
    {
        var item1 = CreateValidSourceItem();
        item1.RawContent = "Identical article content with enough characters to pass.";
        var item2 = CreateValidSourceItem();
        item2.RawContent = "Identical article content with enough characters to pass.";

        var dto1 = await _normalizer.NormalizeAsync(item1);
        var dto2 = await _normalizer.NormalizeAsync(item2);

        Assert.Equal(dto1.ContentHash, dto2.ContentHash);
    }

    [Fact]
    public async Task NormalizeAsync_NullSource_DefaultsToOfficialSourceType()
    {
        var item = CreateValidSourceItem();
        item.Source = null!;

        var dto = await _normalizer.NormalizeAsync(item);

        Assert.Equal(SourceType.Official, dto.SourceType);
    }

    // --- Helpers ---

    private static SourceItem CreateValidSourceItem()
    {
        return new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            OriginalUrl = "https://blog.whatsapp.com/some-article",
            Title = "Test Article Title",
            RawContent = "This is the raw content of the article. It has enough characters to pass validation.",
            Status = PipelineStatus.Discovered,
            Source = new Source
            {
                Id = Guid.NewGuid(),
                Name = "WhatsApp Blog",
                Type = SourceType.Official,
                BaseUrl = "https://blog.whatsapp.com"
            }
        };
    }

    /// <summary>
    /// In-memory fake repository for testing without EF Core.
    /// </summary>
    private class FakeSourceItemRepository : ISourceItemRepository
    {
        public List<SourceItem> UpdatedItems { get; } = [];
        public List<SourceItem> AddedItems { get; } = [];

        public Task<SourceItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<SourceItem?>(null);

        public Task<bool> ExistsByUrlAsync(string originalUrl, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> ExistsByCanonicalUrlAsync(string canonicalUrl, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task AddAsync(SourceItem item, CancellationToken ct = default)
        {
            AddedItems.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SourceItem item, CancellationToken ct = default)
        {
            UpdatedItems.Add(item);
            return Task.CompletedTask;
        }
    }
}
