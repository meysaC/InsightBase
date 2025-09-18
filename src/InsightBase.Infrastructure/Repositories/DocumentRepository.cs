using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsightBase.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;
        public DocumentRepository(AppDbContext context) => _context = context;
        public async Task AddAsync(Document document) => await _context.Documents.AddAsync(document);

        public async Task<Document> GetByIdAsync(Guid id)
        {
            return await _context.Documents
                            .Include(d => d.Chunks)
                            .ThenInclude(c => c.Embedding)
                            .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
    }
}