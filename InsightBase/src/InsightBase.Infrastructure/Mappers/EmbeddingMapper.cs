using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;
using Pgvector;

namespace InsightBase.Infrastructure.Mappers
{
    public class EmbeddingMapper
    {
        public static EmbeddingEntity ToEntity(Embedding domain)
        {
            var embeddingEntity = new EmbeddingEntity
            {
                Id = domain.Id,
                DocumentChunkId = domain.DocumentChunkId,
                Vector = new Vector(domain.Vector),
                ModelName = domain.ModelName,
                CreatedAt = domain.CreatedAt
            };
            return embeddingEntity;
        }

        public static Embedding ToDomain(EmbeddingEntity entity)
        {
            return new Embedding
            {
                Id = entity.Id,
                DocumentChunkId = entity.DocumentChunkId,
                Vector = entity.Vector.ToArray(),
                ModelName = entity.ModelName,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}