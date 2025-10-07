
using InsightBase.Api.DTOs;
using InsightBase.Application.Commands;
using InsightBase.Application.DTOs;

namespace InsightBase.Api.Mappers
{
    public class DocumentMapper 
    {
        // public static UpdateDocumentRequest ToDocumentDto(DocumentDto documentDto)
        // {
        //     var updateDocument = new UpdateDocumentRequest
        //     {
        //         FileName = documentDto.Title,
        //         LegalArea = documentDto.LegalArea,
        //         IsPublic = documentDto.IsPublic
        //     };
        //     return updateDocument;
        // }
        public static UpdateDocumentCommand ToUpdateDocumentCommand(Guid id, UpdateDocumentRequest updateDocument)
        {
            var documentCommand = new UpdateDocumentCommand
            {
                Id = id,
                FileName = updateDocument.UserFileName, 
                LegalArea = updateDocument.LegalArea,
                IsPublic = updateDocument.IsPublic
            };
            return documentCommand;
        }
    }
}