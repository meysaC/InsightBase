using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Mappers;
using InsightBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;

namespace InsightBase.Infrastructure.Services
{
    public class VectorDbService // BU KULLANILMIYOR?? -> EMBEDDINGREPOSITORY KULLANILIYOR
    {
        private readonly AppDbContext _context;
        public VectorDbService(AppDbContext context) => _context = context;

        //yeni embedding kaydeder, Domain entity alır mapper ile EmbeddingEntity ye çevirir.
        public async Task AddEmbeddingAsync(Embedding embedding, CancellationToken cancellationToken = default)
        {
            var entity = Mappers.EmbeddingMapper.ToEntity(embedding);
            await _context.EmbeddingEntities.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        //benzer embeddingleri arar, LINQ pgvector <-> kullanır.
        public async Task<List<Embedding>> SearchSimilarAsync(float[] queryVector, int limit = 5, CancellationToken cancellationToken = default)
        {
            //Pgvector kullanarak benzer vektörleri arar
            var pgVector = new Pgvector.Vector(queryVector);

            var results = await _context.EmbeddingEntities
                .OrderBy(e => e.Vector.CosineDistance(pgVector))
                .Take(limit)
                .ToListAsync(cancellationToken);

            return results.Select(EmbeddingMapper.ToDomain).ToList();
        }

    }
}