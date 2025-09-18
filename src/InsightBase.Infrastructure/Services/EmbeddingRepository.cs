using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;

namespace InsightBase.Infrastructure.Services
{
    public class EmbeddingRepository : IEmbeddingRepository
    {
        private readonly AppDbContext _context;
        public EmbeddingRepository(AppDbContext context) =>  _context = context;

        public Task<Embedding?> GetEmbeddingEntitiesByChunkIdAsync(Guid chunkId)
        {
           var entity =  _context.EmbeddingEntities.FirstOrDefault(e => e.DocumentChunkId == chunkId);
           return entity == null ? null : Task.FromResult(Mappers.EmbeddingMapper.ToDomain(entity));
        }

        public async Task SaveEmbeddingEntitiesAsync(Embedding domainEmbedding)
        {
           var entity = Mappers.EmbeddingMapper.ToEntity(domainEmbedding);
           await _context.EmbeddingEntities.AddAsync(entity);
           await _context.SaveChangesAsync();
        }

    }
}