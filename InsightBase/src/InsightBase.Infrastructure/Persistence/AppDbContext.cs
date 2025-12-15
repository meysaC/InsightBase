using InsightBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace InsightBase.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        public DbSet<EmbeddingEntity> EmbeddingEntities => Set<EmbeddingEntity>();
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Name = "User",
                    NormalizedName = "USER"
                }
            };
            modelBuilder.Entity<IdentityRole>().HasData(roles);

            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<EmbeddingEntity>(entity =>
            {
                entity.ToTable("Embeddings"); // Tablo adı "Embeddings"
                entity.HasKey(e => e.Id);

                entity.HasOne<DocumentChunk>()
                      .WithOne()
                      .HasForeignKey<EmbeddingEntity>(e => e.DocumentChunkId)
                      .OnDelete(DeleteBehavior.Cascade); // DocumentChunk silindiğinde ilişkili Embedding de silinsin
                entity.HasIndex(e => e.DocumentChunkId).IsUnique(); // her chunk için tek embedding

                entity.Property(e => e.Vector)
                    .HasColumnType("vector(1536)"); //pgvector sütun tipi text-embedding-3-small için 1536 boyutlu vektör
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Document ↔ DocumentChunk 1:N
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity
                    .HasOne(dc => dc.Document)
                    .WithMany(e => e.Chunks)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade); // Document silindiğinde ilişkili DocumentChunk'lar da silinsin
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull); // Kullanıcı silindiğinde UserId null olur

                //filtreleme performansı için
                entity.HasIndex(e => e.DocumentType); //Tabloya veri eklerken fazladan bir veri yapısı yaratılır
                entity.HasIndex(e => e.LegalArea);
                entity.HasIndex(e => e.PublishDate);
            });
        }
    }
}