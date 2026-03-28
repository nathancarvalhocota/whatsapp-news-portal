using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatsAppNewsPortal.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceBaseUrlUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_sources_BaseUrl",
                table: "sources",
                column: "BaseUrl",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sources_BaseUrl",
                table: "sources");
        }
    }
}
