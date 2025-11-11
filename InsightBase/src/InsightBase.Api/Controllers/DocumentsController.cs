using InsightBase.Api.DTOs;
using InsightBase.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using InsightBase.Api.Mappers;
using InsightBase.Application.DTOs;

namespace InsightBase.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DocumentsController(IMediator mediator) => _mediator = mediator;
        //UPLOAD
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0) return BadRequest("File is empty.");
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream);

            var command = new UploadDocumentCommand
            {
                UserFileName = request.UserFileName ?? request.File.FileName,
                FileName = request.File.FileName,
                DocumentType = request.DocumentType,
                LegalArea = request.LegalArea,
                IsPublic = request.IsPublic,
                Content = memoryStream.ToArray()
            };

            // var documentId = await _mediator.Send(command);
            var dto = await _mediator.Send(command);
            if (dto == null) return NotFound(); // 404
            return Ok(dto);

            // return CreatedAtAction(nameof(GetById), new { id = documentId, version = "1.0"}, new { DocumentId = documentId}); //POST sonrası resource’un nerede olduğunu göster
            // return Ok(new { DocumentId = documentId });
        }
        //READ LIST
        [HttpGet]
        public async Task<ActionResult<PagedResult<DocumentDto?>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = new GetDocumentsQueryCommand(page, pageSize);
            var result = await _mediator.Send(query);
            return result;
        }
        //READ DETAIL
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DocumentDto>> GetById([FromRoute] Guid id)
        {
            var query = new GetDocumentByIdCommand(id);
            var dto = await _mediator.Send(query);
            if (dto == null) return NotFound(); // 404 döndürür
            return dto; //IActionResult değil e ActionResult yaptığımız için return OK(dto) şeklinde yapmamıza gerek yok
        }
        //UPDATE
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDocumentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var command = DocumentMapper.ToUpdateDocumentCommand(id, request);
            var dto = await _mediator.Send(command);
            if (dto == null) return NotFound(); // 404
            return Ok(dto);
        }
        //DELETE
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteDocumentCommand(id);
            var result = await _mediator.Send(command);
            if (result == null) return StatusCode(500, "Unexcepted null result.");
            if (result.Failed.Any()) return NotFound(result);
            return NoContent();
        }
    }
}