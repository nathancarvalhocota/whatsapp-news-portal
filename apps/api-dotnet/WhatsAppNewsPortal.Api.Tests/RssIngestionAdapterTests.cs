using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class RssIngestionAdapterTests
{
    // -----------------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------------

    private const string Rss2Fixture = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>WhatsApp Blog</title>
            <link>https://blog.whatsapp.com</link>
            <item>
              <title>New Privacy Features in WhatsApp</title>
              <link>https://blog.whatsapp.com/new-privacy-features</link>
              <description>We are introducing new privacy controls.</description>
              <pubDate>Mon, 24 Mar 2026 12:00:00 +0000</pubDate>
            </item>
            <item>
              <title>WhatsApp Communities Update</title>
              <link>https://blog.whatsapp.com/communities-update</link>
              <description>Communities are getting new tools.</description>
              <pubDate>Tue, 25 Mar 2026 14:00:00 +0000</pubDate>
            </item>
          </channel>
        </rss>
        """;

    private const string AtomFixture = """
        <?xml version="1.0" encoding="UTF-8"?>
        <feed xmlns="http://www.w3.org/2005/Atom">
          <title>WABetaInfo</title>
          <entry>
            <title>WhatsApp is testing a new status feature</title>
            <link href="https://wabetainfo.com/whatsapp-testing-status"/>
            <published>2026-03-25T10:00:00Z</published>
            <summary>WhatsApp is developing a new way to interact with status.</summary>
          </entry>
          <entry>
            <title>WhatsApp beta gets redesigned chat header</title>
            <link href="https://wabetainfo.com/whatsapp-beta-chat-header"/>
            <published>2026-03-26T08:00:00Z</published>
            <summary>The latest beta introduces a redesigned chat header.</summary>
          </entry>
        </feed>
        """;

    private const string Rss2WithDuplicatesFixture = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <item>
              <title>Item A</title>
              <link>https://blog.whatsapp.com/item-a</link>
            </item>
            <item>
              <title>Item A Duplicate</title>
              <link>https://blog.whatsapp.com/item-a</link>
            </item>
            <item>
              <title>Item B</title>
              <link>https://blog.whatsapp.com/item-b</link>
            </item>
          </channel>
        </rss>
        """;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Source MakeSource(string? feedUrl = "https://blog.whatsapp.com/rss") => new()
    {
        Id = Guid.NewGuid(),
        Name = "WhatsApp Blog",
        Type = SourceType.Official,
        BaseUrl = "https://blog.whatsapp.com",
        FeedUrl = feedUrl
    };

    private static RssIngestionAdapter MakeAdapter(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new StaticHttpMessageHandler(responseBody, statusCode);
        var httpClient = new HttpClient(handler);
        return new RssIngestionAdapter(httpClient, NullLogger<RssIngestionAdapter>.Instance);
    }

    private static RssIngestionAdapter MakeThrowingAdapter()
    {
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        return new RssIngestionAdapter(httpClient, NullLogger<RssIngestionAdapter>.Instance);
    }

    // -----------------------------------------------------------------------
    // RSS 2.0 parsing
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseFeed_Rss2_ExtractsCorrectNumberOfItems()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource();

        var items = adapter.ParseFeed(Rss2Fixture, source);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void ParseFeed_Rss2_ExtractsTitleLinkAndDate()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource();

        var items = adapter.ParseFeed(Rss2Fixture, source);
        var first = items[0];

        Assert.Equal("New Privacy Features in WhatsApp", first.Title);
        Assert.Equal("https://blog.whatsapp.com/new-privacy-features", first.OriginalUrl);
        Assert.NotNull(first.PublishedAt);
        Assert.Equal(source.Id, first.SourceId);
    }

    [Fact]
    public void ParseFeed_Rss2_ExtractsDescription()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource();

        var items = adapter.ParseFeed(Rss2Fixture, source);

        Assert.Equal("We are introducing new privacy controls.", items[0].RawContent);
    }

    [Fact]
    public void ParseFeed_Rss2_PublishedAtIsUtc()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource();

        var items = adapter.ParseFeed(Rss2Fixture, source);

        Assert.Equal(DateTimeKind.Utc, items[0].PublishedAt!.Value.Kind);
    }

    // -----------------------------------------------------------------------
    // Atom parsing
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseFeed_Atom_ExtractsCorrectNumberOfItems()
    {
        var adapter = MakeAdapter(AtomFixture);
        var source = MakeSource("https://wabetainfo.com/feed/");

        var items = adapter.ParseFeed(AtomFixture, source);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void ParseFeed_Atom_ExtractsTitleLinkAndDate()
    {
        var adapter = MakeAdapter(AtomFixture);
        var source = MakeSource("https://wabetainfo.com/feed/");

        var items = adapter.ParseFeed(AtomFixture, source);
        var first = items[0];

        Assert.Equal("WhatsApp is testing a new status feature", first.Title);
        Assert.Equal("https://wabetainfo.com/whatsapp-testing-status", first.OriginalUrl);
        Assert.NotNull(first.PublishedAt);
    }

    [Fact]
    public void ParseFeed_Atom_ExtractsSummary()
    {
        var adapter = MakeAdapter(AtomFixture);
        var source = MakeSource("https://wabetainfo.com/feed/");

        var items = adapter.ParseFeed(AtomFixture, source);

        Assert.Equal("WhatsApp is developing a new way to interact with status.", items[0].RawContent);
    }

    // -----------------------------------------------------------------------
    // Deduplication within batch
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseFeed_Rss2_DeduplicatesDuplicateUrlsWithinBatch()
    {
        var adapter = MakeAdapter(Rss2WithDuplicatesFixture);
        var source = MakeSource();

        var items = adapter.ParseFeed(Rss2WithDuplicatesFixture, source);

        // 3 items in feed, 2 unique URLs — should return 2
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.NotEmpty(item.OriginalUrl));
    }

    // -----------------------------------------------------------------------
    // URL normalization
    // -----------------------------------------------------------------------

    [Fact]
    public void NormalizeUrl_LowercasesSchemeAndHost()
    {
        var result = RssIngestionAdapter.NormalizeUrl("HTTPS://BLOG.WhatsApp.COM/some-Post");
        Assert.StartsWith("https://blog.whatsapp.com/", result);
    }

    [Fact]
    public void NormalizeUrl_TrimsWhitespace()
    {
        var result = RssIngestionAdapter.NormalizeUrl("  https://blog.whatsapp.com/post  ");
        Assert.Equal("https://blog.whatsapp.com/post", result);
    }

    [Fact]
    public void NormalizeUrl_ReturnsLowercasedInputForInvalidUri()
    {
        var result = RssIngestionAdapter.NormalizeUrl("not-a-url");
        Assert.Equal("not-a-url", result);
    }

    // -----------------------------------------------------------------------
    // Controlled failure: HTTP errors
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyOnHttpRequestException()
    {
        var adapter = MakeThrowingAdapter();
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyOnNonSuccessStatusCode()
    {
        var adapter = MakeAdapter(string.Empty, HttpStatusCode.InternalServerError);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    // -----------------------------------------------------------------------
    // Controlled failure: invalid XML
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyOnInvalidXml()
    {
        var adapter = MakeAdapter("<this is not valid xml<<<");
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    // -----------------------------------------------------------------------
    // XML sanitization: feeds com & não escapado
    // -----------------------------------------------------------------------

    [Fact]
    public void SanitizeXml_EscapesUnescapedAmpersands()
    {
        var input = "<item><title>A &amp; B</title><link>https://x.com?a=1&b=2</link></item>";
        var result = RssIngestionAdapter.SanitizeXml(input);

        // &amp; já escapado deve permanecer; & solto na URL deve virar &amp;
        Assert.Contains("A &amp; B", result);
        Assert.Contains("a=1&amp;b=2", result);
    }

    [Fact]
    public void SanitizeXml_PreservesValidEntities()
    {
        var input = "<title>A &amp; B &lt;test&gt; &#169; &#x00A9;</title>";
        var result = RssIngestionAdapter.SanitizeXml(input);

        // Nenhuma entidade válida deve ser alterada
        Assert.Equal(input, result);
    }

    [Fact]
    public void ParseFeed_WithUnescapedAmpersand_ParsesSuccessfully()
    {
        // Simula o problema real do WhatsApp Blog: & solto no conteúdo
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
              <channel>
                <title>WhatsApp Blog</title>
                <item>
                  <title>Privacy &amp; Security Update</title>
                  <link>https://blog.whatsapp.com/post?a=1&b=2</link>
                  <description>New privacy &amp; security features.</description>
                  <pubDate>Mon, 28 Mar 2026 12:00:00 +0000</pubDate>
                </item>
              </channel>
            </rss>
            """;

        var adapter = MakeAdapter(xml);
        var source = MakeSource();

        var items = adapter.ParseFeed(xml, source);

        Assert.Single(items);
        Assert.Contains("blog.whatsapp.com/post", items[0].OriginalUrl);
    }

    // -----------------------------------------------------------------------
    // Source without FeedUrl
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyWhenSourceHasNoFeedUrl()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource(feedUrl: null);

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyWhenSourceHasEmptyFeedUrl()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource(feedUrl: "  ");

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    // -----------------------------------------------------------------------
    // IsDemoItem defaults to false
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseFeed_Rss2_IsDemoItemDefaultsFalse()
    {
        var adapter = MakeAdapter(Rss2Fixture);
        var source = MakeSource();

        var items = adapter.ParseFeed(Rss2Fixture, source);

        Assert.All(items, item => Assert.False(item.IsDemoItem));
    }
}

// ---------------------------------------------------------------------------
// Helpers for mocking HttpMessageHandler
// ---------------------------------------------------------------------------

internal class StaticHttpMessageHandler : HttpMessageHandler
{
    private readonly string _body;
    private readonly HttpStatusCode _statusCode;

    public StaticHttpMessageHandler(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _body = body;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/xml")
        };
        return Task.FromResult(response);
    }
}

internal class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new HttpRequestException("Simulated network failure");
}
