using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands
{
    public class UpdateDocumentCommand : IRequest<DocumentDto?> //DocumentDto tipinde geri döndürülcek
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string? LegalArea { get; set; }
        public bool IsPublic { get; set; }
    }
}