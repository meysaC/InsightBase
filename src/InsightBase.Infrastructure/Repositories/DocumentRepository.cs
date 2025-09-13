using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;

namespace InsightBase.Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;
        public DocumentRepository(AppDbContext context) => _context = context;
        public async Task AddAsync(Document document) => await _context.Documents.AddAsync(document);
        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();
    }
}