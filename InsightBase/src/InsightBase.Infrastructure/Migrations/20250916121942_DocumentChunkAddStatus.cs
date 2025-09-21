using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentChunkAddStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DocumentChunks");
        }
    }
}
