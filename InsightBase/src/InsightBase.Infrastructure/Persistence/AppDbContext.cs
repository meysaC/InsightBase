using InsightBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using InsightBase.Domain.Entities.Chat;
using System.Text.Json;


namespace InsightBase.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        public DbSet<EmbeddingEntity> EmbeddingEntities => Set<EmbeddingEntity>();
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<Message> Messages => Set<Message>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                      .IsRequired();

                entity.Property(e => e.TokenHash)
                      .IsRequired();

                entity.Property(x => x.CreatedAt)
                      .IsRequired();

                entity.Property(e => e.ExpiresAt)
                      .IsRequired();

                entity.Property(e => e.IsRevoked)
                      .HasDefaultValue(false);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TokenHash);
            });

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
        
            modelBuilder.Entity<Conversation>(entity =>
            {
               entity.ToTable("conversations") ;
               entity.HasKey(e => e.ConversationId);

               entity.Property(e => e.ConversationId)
                        .IsRequired();

                entity.HasOne<ApplicationUser>()
                        .WithMany(u => u.Conversations)
                        .HasForeignKey(c => c.UserId);
                        //.IsRequired(false); // kayıt olmayan kullanıcılar anon... gibi id dışardan veri

                entity.Property(e => e.UserId)
                        .IsRequired();

                entity.Property(e => e.Title)
                    .HasMaxLength(500);
                
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.LegalAreas)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .HasColumnType("jsonb");

                entity.Property(e => e.RelevantLaws)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .HasColumnType("jsonb");

                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .HasColumnType("jsonb");

                entity.OwnsOne(e => e.Settings, settings =>
                {
                    settings.Property(s => s.EnableCitations).HasColumnName("enable_citations");
                    settings.Property(s => s.EnableSourceDisplay).HasColumnName("enable_source_display");
                    settings.Property(s => s.MaxSourcesPerMessage).HasColumnName("max_sources_per_message");
                    settings.Property(s => s.PreferredLegalArea).HasColumnName("preferred_legal_area");
                });

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastMessageAt);
                entity.HasIndex(e => new { e.UserId, e.Status });

                // Relationships
                entity.HasMany(e => e.Messages)
                    .WithOne()
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("messages");
                entity.HasKey(e => e.MessageId);

                entity.Property(e => e.MessageId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.ConversationId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne<ApplicationUser>()
                        .WithMany(u => u.Messages)
                        .HasForeignKey(m => m.UserId);

                entity.Property(e => e.Role)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.Content)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.AttachmentIds)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .HasColumnType("jsonb");

                entity.Property(e => e.Citations)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<CitationInfo>>(v, (JsonSerializerOptions?)null) ?? new List<CitationInfo>())
                    .HasColumnType("jsonb");

                entity.Property(e => e.Sources)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<SourceInfo>>(v, (JsonSerializerOptions?)null) ?? new List<SourceInfo>())
                    .HasColumnType("jsonb");

                entity.Property(e => e.QueryContext)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<QueryContextInfo>(v, (JsonSerializerOptions?)null))
                    .HasColumnType("jsonb");

                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .HasColumnType("jsonb");

                entity.OwnsOne(e => e.Feedback, feedback =>
                {
                    feedback.Property(f => f.IsHelpful).HasColumnName("feedback_is_helpful");
                    feedback.Property(f => f.Comment).HasColumnName("feedback_comment");
                    feedback.Property(f => f.CreatedAt).HasColumnName("feedback_created_at");
                });

                // Indexes
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.ConversationId, e.SequenceNumber });
                
                // Full-text search index (PostgreSQL)
                entity.HasIndex(e => e.Content)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");
            });
        }
    }
}