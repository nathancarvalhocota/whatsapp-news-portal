using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppNewsPortal.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "sources",
                newName: "BaseUrl");

            migrationBuilder.RenameColumn(
                name: "Active",
                table: "sources",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "source_items",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "articles",
                newName: "Excerpt");

            migrationBuilder.RenameColumn(
                name: "EditorialType",
                table: "articles",
                newName: "ArticleType");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "articles",
                newName: "ContentHtml");

            migrationBuilder.AddColumn<string>(
                name: "FeedUrl",
                table: "sources",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "sources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "source_items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "source_items",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "source_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "source_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoItem",
                table: "source_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceClassification",
                table: "source_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "source_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "articles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "articles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SchemaJsonLd",
                table: "articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "articles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "article_source_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_source_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_article_source_references_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processing_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SourceItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_source_items_CanonicalUrl",
                table: "source_items",
                column: "CanonicalUrl");

            migrationBuilder.CreateIndex(
                name: "IX_source_items_ContentHash",
                table: "source_items",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_articles_Category",
                table: "articles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_article_source_references_ArticleId",
                table: "article_source_references",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_processing_logs_CreatedAt",
                table: "processing_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_processing_logs_SourceItemId",
                table: "processing_logs",
                column: "SourceItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_source_references");

            migrationBuilder.DropTable(
                name: "processing_logs");

            migrationBuilder.DropIndex(
                name: "IX_source_items_CanonicalUrl",
                table: "source_items");

            migrationBuilder.DropIndex(
                name: "IX_source_items_ContentHash",
                table: "source_items");

            migrationBuilder.DropIndex(
                name: "IX_articles_Category",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "FeedUrl",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "source_items");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "source_items");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "source_items");

            migrationBuilder.DropColumn(
                name: "IsDemoItem",
                table: "source_items");

            migrationBuilder.DropColumn(
                name: "SourceClassification",
                table: "source_items");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "source_items");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "SchemaJsonLd",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "articles");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "sources",
                newName: "Active");

            migrationBuilder.RenameColumn(
                name: "BaseUrl",
                table: "sources",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                table: "source_items",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "Excerpt",
                table: "articles",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "ContentHtml",
                table: "articles",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "ArticleType",
                table: "articles",
                newName: "EditorialType");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "source_items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
