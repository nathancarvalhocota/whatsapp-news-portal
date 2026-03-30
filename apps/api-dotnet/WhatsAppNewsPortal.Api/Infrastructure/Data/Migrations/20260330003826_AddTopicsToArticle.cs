using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppNewsPortal.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicsToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topics",
                table: "articles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topics",
                table: "articles");
        }
    }
}
