using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InsightBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdendityUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "162ae62a-23ae-4f44-ab4b-2fa4edebc1b4", null, "User", "USER" },
                    { "535dbe66-2586-45dd-900b-6af2a8a1ce45", null, "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "162ae62a-23ae-4f44-ab4b-2fa4edebc1b4");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "535dbe66-2586-45dd-900b-6af2a8a1ce45");
        }
    }
}
