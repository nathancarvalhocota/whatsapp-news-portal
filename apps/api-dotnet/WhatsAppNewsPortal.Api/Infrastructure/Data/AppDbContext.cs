using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<SourceItem> SourceItems => Set<SourceItem>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleSourceReference> ArticleSourceReferences => Set<ArticleSourceReference>();
    public DbSet<ProcessingLog> ProcessingLogs => Set<ProcessingLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSource(modelBuilder);
        ConfigureSourceItem(modelBuilder);
        ConfigureArticle(modelBuilder);
        ConfigureArticleSourceReference(modelBuilder);
        ConfigureProcessingLog(modelBuilder);
    }

    private static void ConfigureSource(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Source>(entity =>
        {
            entity.ToTable("sources");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired()
                  .HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FeedUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

            entity.HasIndex(e => e.BaseUrl).IsUnique();
        });
    }

    private static void ConfigureSourceItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SourceItem>(entity =>
        {
            entity.ToTable("source_items");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CanonicalUrl).HasMaxLength(1000);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RawContent).HasColumnType("text");
            entity.Property(e => e.NormalizedContent).HasColumnType("text");
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.Property(e => e.Status).IsRequired()
                  .HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.SourceClassification).HasMaxLength(100);
            entity.Property(e => e.IsDemoItem).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.ErrorMessage).HasColumnType("text");
            entity.Property(e => e.DiscoveredAt).IsRequired().HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

            entity.HasIndex(e => e.OriginalUrl);
            entity.HasIndex(e => e.CanonicalUrl);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Source)
                  .WithMany()
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureArticle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("articles");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Slug).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Excerpt).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentHtml).IsRequired().HasColumnType("text");
            entity.Property(e => e.MetaTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MetaDescription).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SchemaJsonLd).HasColumnType("text");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.Property(e => e.ArticleType).IsRequired()
                  .HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired()
                  .HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);

            entity.HasOne(e => e.SourceItem)
                  .WithMany()
                  .HasForeignKey(e => e.SourceItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureArticleSourceReference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleSourceReference>(entity =>
        {
            entity.ToTable("article_source_references");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.SourceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SourceUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ReferenceType).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.ArticleId);

            entity.HasOne(e => e.Article)
                  .WithMany(a => a.SourceReferences)
                  .HasForeignKey(e => e.ArticleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProcessingLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessingLog>(entity =>
        {
            entity.ToTable("processing_logs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StepName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Message).HasColumnType("text");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("now()");

            entity.HasIndex(e => e.SourceItemId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
