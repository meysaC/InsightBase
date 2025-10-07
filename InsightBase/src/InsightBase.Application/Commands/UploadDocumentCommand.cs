using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.DTOs;
using MediatR;

namespace InsightBase.Application.Commands
{
    public class UploadDocumentCommand : IRequest<DocumentDto> //Guid
    {
        public string? UserId { get; set; } = string.Empty;
        public string? UserFileName { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; } = null!;
        public byte[] Content { get; set; } = Array.Empty<byte>();

    }
}