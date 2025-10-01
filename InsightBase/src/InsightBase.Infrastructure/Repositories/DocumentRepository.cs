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

        public async Task DeleteAsync(Document document)
        {
            _context.Documents.Remove(document);
            await Task.CompletedTask;
        }

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

        public async Task UpdateAsync(Document document)
        {
            var documentDomain = await _context.Documents.FirstOrDefaultAsync(d => d.Id == document.Id);

            documentDomain.Title = document.Title;
            documentDomain.LegalArea = document.LegalArea;
            documentDomain.IsPublic = document.IsPublic;

            _context.Documents.Update(documentDomain);
            await Task.CompletedTask; //ef core zaten change tracker üzerindne takip ediyor
        }
    }
}