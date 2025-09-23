using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Services
{
    public class EmbeddingRepository : IEmbeddingRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmbeddingRepository> _logger;
        public EmbeddingRepository(AppDbContext context,  ILogger<EmbeddingRepository> logger) => (_context, logger) = (context, _logger);

        public Task<Embedding?> GetEmbeddingEntitiesByChunkIdAsync(Guid chunkId)
        {
            try
            {
                var entity =  _context.EmbeddingEntities
                                        .AsNoTracking()
                                        .FirstOrDefault(e => e.DocumentChunkId == chunkId);
                return entity == null ? null : Task.FromResult(Mappers.EmbeddingMapper.ToDomain(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving embedding for chunkId {chunkId}", ex);
                throw;
            }
        }

        public async Task AddEmbeddingEntitiesAsync(Embedding domainEmbedding)
        {
            // BAŞARIYLA EMBEDDING KAYDEDİLİRSE O EMBEDDİNGİN DOCUMENTCHUNK'I PROCESSED OLARAK DEĞİŞTİRİLMELİ!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
           var entity = Mappers.EmbeddingMapper.ToEntity(domainEmbedding);
           await _context.EmbeddingEntities.AddAsync(entity);
           await _context.SaveChangesAsync();
        }

    }
}