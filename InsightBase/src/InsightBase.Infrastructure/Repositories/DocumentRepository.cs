using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DocumentRepository> _logger;
        public DocumentRepository(AppDbContext context, ILogger<DocumentRepository> logger) => (_context, _logger)  = (context, logger) ;
        public async Task AddAsync(Document document) => await _context.Documents.AddAsync(document);

        public async Task<Document> GetByIdAsync(Guid id)
        {
            // OKUMA İŞİ OLDUĞU İÇİN AsNoTracking kullanmak tracked entity’ler çakışıyor sorunu olmasını aza indirir
            try
            {
                return await _context.Documents
                                .AsNoTracking()
                                .Include(d => d.Chunks)
                                .ThenInclude(c => c.Embedding)
                                .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving document for documentId {id}, ex");
                throw;
            }
        }

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
    }
}