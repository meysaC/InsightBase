using InsightBase.Application.DTOs;
using InsightBase.Domain.Entities;

namespace InsightBase.Application.Mapper
{
    public class DocumentMapper
    {
        public static DocumentDto ToDocumentDto(Document document)
        {
            var documentDto = new DocumentDto
            {
                Id = document.Id,
                FileName = document.FileName,
                UserFileName = document.UserFileName,
                DocumentType = document.DocumentType,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                LegalArea = document.LegalArea,
                IsPublic = document.IsPublic
            };
            return documentDto;

        }
    }
}