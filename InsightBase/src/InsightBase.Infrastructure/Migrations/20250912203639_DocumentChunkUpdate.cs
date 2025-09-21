using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentChunkUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EndToken",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Length",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartToken",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EndToken",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "StartToken",
                table: "DocumentChunks");
        }
    }
}
