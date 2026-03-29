using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class HtmlIngestionAdapterTests
{
    // -----------------------------------------------------------------------
    // Fixtures
    // -----------------------------------------------------------------------

    private const string BusinessBlogListingFixture = """
        <!DOCTYPE html>
        <html lang="en">
        <head><title>WhatsApp Business Blog</title></head>
        <body>
          <header>
            <nav><a href="/">Home</a><a href="/blog">Blog</a></nav>
          </header>
          <main>
            <h1>WhatsApp Business Blog</h1>
            <div class="posts">
              <div class="post-card">
                <a href="/blog/whatsapp-business-platform-updates-2026">
                  WhatsApp Business Platform: New Updates for 2026
                </a>
              </div>
              <div class="post-card">
                <a href="/blog/new-commerce-features">
                  New Commerce Features for Small Businesses
                </a>
              </div>
              <div class="post-card">
                <a href="/blog/messaging-api-improvements">
                  Messaging API: What's New in the Latest Release
                </a>
              </div>
            </div>
          </main>
          <footer>
            <a href="/privacy">Privacy</a>
          </footer>
        </body>
        </html>
        """;

    private const string ArticleFixture = """
        <!DOCTYPE html>
        <html lang="en">
        <head><title>New Updates for 2026</title></head>
        <body>
          <article>
            <h1>WhatsApp Business Platform: New Updates for 2026</h1>
            <time datetime="2026-03-20">March 20, 2026</time>
            <div class="blog-post-content">
              <p>We're excited to announce several new updates.</p>
              <h2>Enhanced Templates</h2>
              <p>Businesses can now create richer templates.</p>
            </div>
          </article>
        </body>
        </html>
        """;

    private const string DuplicateLinksFixture = """
        <!DOCTYPE html>
        <html><body>
          <a href="/blog/post-1">WhatsApp Announces New Feature One</a>
          <a href="/blog/post-1">WhatsApp Announces New Feature One Again</a>
          <a href="/blog/post-2">WhatsApp Announces New Feature Two</a>
        </body></html>
        """;

    private const string DefaultSelectorFixture = """
        <!DOCTYPE html>
        <html><body>
          <a href="https://example.com/article-1">Article One: Full Title</a>
          <a href="https://example.com/article-2">Article Two: Full Title</a>
          <a href="#">Anchor only</a>
          <a href="">Empty href</a>
        </body></html>
        """;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Source MakeSource(
        string baseUrl = "https://business.whatsapp.com/blog",
        string? feedUrl = null,
        SourceType type = SourceType.Official) => new()
    {
        Id = Guid.NewGuid(),
        Name = "WhatsApp Business Blog",
        Type = type,
        BaseUrl = baseUrl,
        FeedUrl = feedUrl
    };

    private static HtmlIngestionAdapter MakeAdapter(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new StaticHttpMessageHandler(responseBody, statusCode);
        var httpClient = new HttpClient(handler);
        var fetcher = new HtmlFetcher(httpClient, NullLogger<HtmlFetcher>.Instance);
        return new HtmlIngestionAdapter(fetcher, NullLogger<HtmlIngestionAdapter>.Instance);
    }

    private static HtmlIngestionAdapter MakeThrowingAdapter()
    {
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var fetcher = new HtmlFetcher(httpClient, NullLogger<HtmlFetcher>.Instance);
        return new HtmlIngestionAdapter(fetcher, NullLogger<HtmlIngestionAdapter>.Instance);
    }

    // -----------------------------------------------------------------------
    // FetchItemsAsync — source selection
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyWhenSourceHasFeedUrl()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource(feedUrl: "https://business.whatsapp.com/blog/feed");

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyOnHttpFailure()
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

    [Fact]
    public async Task FetchItemsAsync_ReturnsEmptyOnInvalidHtml()
    {
        var adapter = MakeAdapter(string.Empty);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.Empty(items);
    }

    // -----------------------------------------------------------------------
    // Listing page parsing — business blog
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FetchItemsAsync_ExtractsArticleLinksFromBusinessBlog()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task FetchItemsAsync_ExtractsTitlesFromListingPage()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.Contains(items, i => i.Title.Contains("New Updates for 2026"));
        Assert.Contains(items, i => i.Title.Contains("Commerce Features"));
        Assert.Contains(items, i => i.Title.Contains("Messaging API"));
    }

    [Fact]
    public async Task FetchItemsAsync_ResolvesRelativeUrls()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.All(items, i => Assert.StartsWith("https://", i.OriginalUrl));
    }

    [Fact]
    public async Task FetchItemsAsync_FiltersOutNonBlogLinks()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.DoesNotContain(items, i => i.OriginalUrl.EndsWith("/privacy"));
        Assert.DoesNotContain(items, i => i.OriginalUrl.EndsWith("/"));
    }

    [Fact]
    public async Task FetchItemsAsync_SetsSourceIdOnAllItems()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.All(items, i => Assert.Equal(source.Id, i.SourceId));
    }

    [Fact]
    public async Task FetchItemsAsync_IsDemoItemDefaultsFalse()
    {
        var adapter = MakeAdapter(BusinessBlogListingFixture);
        var source = MakeSource();

        var items = await adapter.FetchItemsAsync(source);

        Assert.All(items, i => Assert.False(i.IsDemoItem));
    }

    // -----------------------------------------------------------------------
    // Deduplication within batch
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseListingPage_DeduplicatesDuplicateLinks()
    {
        var source = MakeSource();
        var config = new HtmlSourceParserConfig
        {
            ArticleLinkSelector = "a[href*='/blog/']",
            ArticleLinkPattern = @"/blog/.+"
        };
        var adapter = MakeAdapter(DuplicateLinksFixture);

        var items = adapter.ParseListingPage(DuplicateLinksFixture, source, config);

        Assert.Equal(2, items.Count);
    }

    // -----------------------------------------------------------------------
    // Default config — unknown source
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseListingPage_DefaultConfig_ExtractsAllAnchorLinks()
    {
        var source = MakeSource(baseUrl: "https://example.com");
        var config = new HtmlSourceParserConfig(); // default: a[href], no pattern filter
        var adapter = MakeAdapter(DefaultSelectorFixture);

        var items = adapter.ParseListingPage(DefaultSelectorFixture, source, config);

        // Only absolute links pass (2), anchors (#) and empty href are skipped
        Assert.Equal(2, items.Count);
    }

    // -----------------------------------------------------------------------
    // Minimum title length filter
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseListingPage_FiltersShortTitles()
    {
        var html = """
            <html><body>
              <a href="https://example.com/log-in">Log in</a>
              <a href="https://example.com/download">Download</a>
              <a href="https://example.com/read-more">Read more</a>
              <a href="https://example.com/real-article">WhatsApp Announces New Feature for Users</a>
            </body></html>
            """;
        var source = MakeSource(baseUrl: "https://example.com");
        var config = new HtmlSourceParserConfig();
        var adapter = MakeAdapter(html);

        var items = adapter.ParseListingPage(html, source, config);

        Assert.Single(items);
        Assert.Contains("WhatsApp Announces", items[0].Title);
    }

    // -----------------------------------------------------------------------
    // URL resolution
    // -----------------------------------------------------------------------

    [Fact]
    public void ResolveUrl_AbsoluteUrl_ReturnedAsIs()
    {
        var result = HtmlIngestionAdapter.ResolveUrl("https://example.com/page", null);

        Assert.Equal("https://example.com/page", result);
    }

    [Fact]
    public void ResolveUrl_RelativeUrl_ResolvedAgainstBase()
    {
        var baseUri = new Uri("https://business.whatsapp.com/blog");

        var result = HtmlIngestionAdapter.ResolveUrl("/blog/new-post", baseUri);

        Assert.Equal("https://business.whatsapp.com/blog/new-post", result);
    }

    [Fact]
    public void ResolveUrl_RelativeUrl_WithoutBase_ReturnsNull()
    {
        var result = HtmlIngestionAdapter.ResolveUrl("/blog/new-post", null);

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // Content extraction
    // -----------------------------------------------------------------------

    [Fact]
    public void ExtractContent_ExtractsArticleText()
    {
        var config = new HtmlSourceParserConfig
        {
            ContentSelector = "article, .blog-post-content, main"
        };

        var content = HtmlIngestionAdapter.ExtractContent(ArticleFixture, config);

        Assert.NotNull(content);
        Assert.Contains("excited to announce", content);
        Assert.Contains("Enhanced Templates", content);
    }

    [Fact]
    public void ExtractContent_FallsBackToBody_WhenNoMatchingSelector()
    {
        var html = "<html><body><p>Some fallback content</p></body></html>";
        var config = new HtmlSourceParserConfig { ContentSelector = ".nonexistent" };

        var content = HtmlIngestionAdapter.ExtractContent(html, config);

        Assert.NotNull(content);
        Assert.Contains("fallback content", content);
    }

    [Fact]
    public void ExtractContent_SkipsEmptyElements()
    {
        var html = """
            <html><body>
              <article></article>
              <main><p>Main content here</p></main>
            </body></html>
            """;
        var config = new HtmlSourceParserConfig { ContentSelector = "article, main" };

        var content = HtmlIngestionAdapter.ExtractContent(html, config);

        Assert.NotNull(content);
        Assert.Contains("Main content here", content);
    }

    // -----------------------------------------------------------------------
    // Title extraction
    // -----------------------------------------------------------------------

    [Fact]
    public void ExtractTitle_ExtractsH1()
    {
        var config = new HtmlSourceParserConfig { TitleSelector = "h1" };

        var title = HtmlIngestionAdapter.ExtractTitle(ArticleFixture, config);

        Assert.Equal("WhatsApp Business Platform: New Updates for 2026", title);
    }

    [Fact]
    public void ExtractTitle_FallsBackToPageTitle()
    {
        var html = "<html><head><title>Page Title</title></head><body><p>No h1 here</p></body></html>";
        var config = new HtmlSourceParserConfig { TitleSelector = "h1" };

        var title = HtmlIngestionAdapter.ExtractTitle(html, config);

        Assert.Equal("Page Title", title);
    }

    [Fact]
    public void ExtractTitle_MultipleSelectors_TriesInOrder()
    {
        var html = """
            <html><body>
              <h1></h1>
              <span class="post-title">Custom Title</span>
            </body></html>
            """;
        var config = new HtmlSourceParserConfig { TitleSelector = "h1, .post-title" };

        var title = HtmlIngestionAdapter.ExtractTitle(html, config);

        Assert.Equal("Custom Title", title);
    }

    // -----------------------------------------------------------------------
    // Config selection per source
    // -----------------------------------------------------------------------

    [Fact]
    public void GetConfigForSource_ReturnsConfigForWhatsAppBlog()
    {
        var adapter = MakeAdapter(string.Empty);
        var source = MakeSource("https://blog.whatsapp.com");

        var config = adapter.GetConfigForSource(source);

        Assert.Contains(@"blog\.whatsapp\.com", config.ArticleLinkPattern!);
    }

    [Fact]
    public void GetConfigForSource_ReturnsConfigForBusinessBlog()
    {
        var adapter = MakeAdapter(string.Empty);
        var source = MakeSource("https://business.whatsapp.com/blog");

        var config = adapter.GetConfigForSource(source);

        Assert.Contains("/blog/", config.ArticleLinkPattern!);
        Assert.Contains("a[href*='/blog/']", config.ArticleLinkSelector);
    }

    [Fact]
    public void GetConfigForSource_ReturnsConfigForDevDocs()
    {
        var adapter = MakeAdapter(string.Empty);
        var source = MakeSource("https://developers.facebook.com/docs/whatsapp");

        var config = adapter.GetConfigForSource(source);

        Assert.Contains("/docs/whatsapp", config.ArticleLinkPattern!);
    }

    [Fact]
    public void GetConfigForSource_ReturnsConfigForWABetaInfo()
    {
        var adapter = MakeAdapter(string.Empty);
        var source = MakeSource("https://wabetainfo.com");

        var config = adapter.GetConfigForSource(source);

        Assert.Contains(@"wabetainfo\.com", config.ArticleLinkPattern!);
        Assert.Contains("entry-title", config.ArticleLinkSelector);
        Assert.Contains("entry-read-more", config.ArticleLinkSelector);
        Assert.Equal(0, config.MinTitleLength);
    }

    [Fact]
    public void GetConfigForSource_ReturnsDefaultForUnknownSource()
    {
        var adapter = MakeAdapter(string.Empty);
        var source = MakeSource("https://unknown-source.example.com");

        var config = adapter.GetConfigForSource(source);

        Assert.Equal("a[href]", config.ArticleLinkSelector);
        Assert.Null(config.ArticleLinkPattern);
    }

    // -----------------------------------------------------------------------
    // URL pattern filtering — blog.whatsapp.com
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseListingPage_WhatsAppBlog_FiltersNavigationLinks()
    {
        var html = """
            <html><body>
              <nav>
                <a href="https://www.whatsapp.com/">Home</a>
                <a href="https://whatsapp.com/download">Download</a>
                <a href="https://www.whatsapp.com/careers">Careers</a>
              </nav>
              <a href="https://blog.whatsapp.com/new-feature-roundup-free-up-space-and-more">
                New Feature Roundup: Free Up Space and More
              </a>
              <a href="https://blog.whatsapp.com/introducing-voice-chat-for-large-groups">
                Introducing Voice Chat for Large Groups
              </a>
            </body></html>
            """;
        var source = MakeSource("https://blog.whatsapp.com");
        var config = HtmlIngestionAdapter.GetParserConfigForHost("https://blog.whatsapp.com/any");
        var adapter = MakeAdapter(html);

        var items = adapter.ParseListingPage(html, source, config);

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Contains("blog.whatsapp.com", i.OriginalUrl));
        Assert.DoesNotContain(items, i => i.Title.Contains("Download"));
        Assert.DoesNotContain(items, i => i.Title.Contains("Careers"));
    }

    // -----------------------------------------------------------------------
    // URL pattern filtering — wabetainfo.com
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseListingPage_WABetaInfo_FiltersNavigationLinksAndDeduplicates()
    {
        // Real wabetainfo structure: h3.entry-title a (title link) + a.entry-read-more ("Open the post")
        // both point to the same URL. Dedup should keep the real title from h3.entry-title.
        var html = """
            <html><body>
              <nav>
                <a href="/android/">Android</a>
                <a href="/ios/">iOS</a>
              </nav>
              <article class="card">
                <h3 class="entry-title">
                  <a href="https://wabetainfo.com/whatsapp-is-working-on-custom-audiences-for-status/">
                    WhatsApp is working on custom audiences for status
                  </a>
                </h3>
                <a class="kenta-button entry-read-more" href="https://wabetainfo.com/whatsapp-is-working-on-custom-audiences-for-status/">
                  Open the post
                </a>
              </article>
              <article class="card">
                <a class="kenta-button entry-read-more" href="https://wabetainfo.com/whatsapp-beta-for-android-tests-private-summaries/">
                  Open the post
                </a>
              </article>
            </body></html>
            """;
        var source = MakeSource("https://wabetainfo.com");
        var config = HtmlIngestionAdapter.GetParserConfigForHost("https://wabetainfo.com/any");
        var adapter = MakeAdapter(html);

        var items = adapter.ParseListingPage(html, source, config);

        // 2 unique articles: one has real title (dedup keeps h3 link), other only has "Open the post"
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(items, i => i.Title is "Android" or "iOS");
        // First article: real title wins over "Open the post" via dedup
        Assert.Contains(items, i => i.Title.Contains("WhatsApp is working"));
        // Second article: only "Open the post" button exists, still discovered (MinTitleLength=0)
        Assert.Contains(items, i => i.Title.Contains("Open the post"));
    }

    // -----------------------------------------------------------------------
    // URL pattern filtering — business.whatsapp.com
    // -----------------------------------------------------------------------

    [Fact]
    public void ParseListingPage_BusinessBlog_FiltersGenericBlogLinks()
    {
        var html = """
            <html><body>
              <nav><a href="/blog/">Blog Home</a></nav>
              <a href="/blog/ecommerce-order-management">Ecommerce Order Management</a>
              <a href="/blog/remarketing-ads-guide">Remarketing Ads Guide</a>
            </body></html>
            """;
        var source = MakeSource("https://business.whatsapp.com/blog");
        var config = HtmlIngestionAdapter.GetParserConfigForHost("https://business.whatsapp.com/blog/any");
        var adapter = MakeAdapter(html);

        var items = adapter.ParseListingPage(html, source, config);

        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(items, i => i.Title == "Blog Home");
    }

    // -----------------------------------------------------------------------
    // HtmlFetcher — unit tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task HtmlFetcher_ReturnsHtmlOnSuccess()
    {
        var handler = new StaticHttpMessageHandler("<html><body>Test</body></html>");
        var httpClient = new HttpClient(handler);
        var fetcher = new HtmlFetcher(httpClient, NullLogger<HtmlFetcher>.Instance);

        var result = await fetcher.FetchHtmlAsync("https://example.com");

        Assert.NotNull(result);
        Assert.Contains("Test", result);
    }

    [Fact]
    public async Task HtmlFetcher_ReturnsNullOnNetworkFailure()
    {
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var fetcher = new HtmlFetcher(httpClient, NullLogger<HtmlFetcher>.Instance);

        var result = await fetcher.FetchHtmlAsync("https://example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task HtmlFetcher_ReturnsNullOnNonSuccessStatus()
    {
        var handler = new StaticHttpMessageHandler(string.Empty, HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler);
        var fetcher = new HtmlFetcher(httpClient, NullLogger<HtmlFetcher>.Instance);

        var result = await fetcher.FetchHtmlAsync("https://example.com");

        Assert.Null(result);
    }
}
