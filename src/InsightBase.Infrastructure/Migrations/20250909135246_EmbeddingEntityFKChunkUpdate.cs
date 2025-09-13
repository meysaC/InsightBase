using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmbeddingEntityFKChunkUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_DocumentChunkId",
                table: "Embeddings",
                column: "DocumentChunkId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Embeddings_DocumentChunks_DocumentChunkId",
                table: "Embeddings",
                column: "DocumentChunkId",
                principalTable: "DocumentChunks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Embeddings_DocumentChunks_DocumentChunkId",
                table: "Embeddings");

            migrationBuilder.DropIndex(
                name: "IX_Embeddings_DocumentChunkId",
                table: "Embeddings");
        }
    }
}
