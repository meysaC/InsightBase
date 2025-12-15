using InsightBase.Domain.Entities;

namespace InsightBase.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Document> GetByIdAsync(Guid id);
        Task<(IEnumerable<Document> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
        Task AddAsync(Document document); //Application katmanı → Domain katmanına bağımlı olabilir (ama Infrastructure’a olamaz). 
                                                          // o yüzden UploadDocumentCommand içerisinde IFormFile kullanamayacağımız için burada kullanıyoruz.
        Task UpdateAsync(Document document);
        Task<bool> DeleteAsync(Document document);
        Task<int> SaveAsync();


        Task<bool> IsGlobalAsync(string documentId);
        Task<bool> UserHasDirectAccessAsync(string userId, string documentId);
        Task<bool> UserHasOrgAccessAsync(string userId, string documentId);
        Task<bool> UserHasRoleAccessAsync(string userId, string documentId);
    }
}