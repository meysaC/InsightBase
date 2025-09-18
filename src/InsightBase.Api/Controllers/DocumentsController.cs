using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Api.DTOs;
using InsightBase.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Minio.DataModel;

namespace InsightBase.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DocumentsController(IMediator mediator) => _mediator = mediator;
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0) return BadRequest("File is empty.");
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);

            var command = new UploadDocumentCommand
            {
                FileName = request.File.FileName, //doğru dosya adını alır (.pdf, .docx, .txt uzantısı dahil)
                Content = memoryStream.ToArray()
            };
            var documentId = await _mediator.Send(command);
            return Ok(new { DocumentId = documentId });
        }

    }
}