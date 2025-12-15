using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands.Document
{
    public class UploadDocumentCommand : IRequest<DocumentDto> //Guid
    {
        public string? UserId { get; set; } = string.Empty;
        public string? UserFileName { get; set; }
        public string FileName { get; set; }
        //public string FileType { get; set; } = null!;
        public string? DocumentType { get; set; }
        public string? LegalArea { get; set; }
        public bool IsPublic { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();

    }
}