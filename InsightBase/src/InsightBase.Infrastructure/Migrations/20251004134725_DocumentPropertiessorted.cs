using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentPropertiessorted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Documents",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "Keywords",
                table: "Documents",
                newName: "UserFileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserFileName",
                table: "Documents",
                newName: "Keywords");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Documents",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Documents",
                type: "text",
                nullable: true);
        }
    }
}
