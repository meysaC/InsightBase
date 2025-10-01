using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Application.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Domain.Entities.Document> GetByIdAsync(Guid id);
        Task AddAsync(Domain.Entities.Document document); //Application katmanı → Domain katmanına bağımlı olabilir (ama Infrastructure’a olamaz). 
                                                          // o yüzden UploadDocumentCommand içerisinde IFormFile kullanamayacağımız için burada kullanıyoruz.
        Task UpdateAsync(Domain.Entities.Document document);
        Task DeleteAsync(Domain.Entities.Document document);
        Task<int> SaveAsync();
    }
}