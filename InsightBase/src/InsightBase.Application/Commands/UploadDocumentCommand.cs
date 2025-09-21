using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace InsightBase.Application.Commands
{
    public class UploadDocumentCommand : IRequest<Guid>
    {
        public string? UserId { get; set; } = string.Empty;
        public string FileName { get; set; } = null!;
        public byte[] Content { get; set; } = Array.Empty<byte>();

    }
}