using InsightBase.Api.DTOs.Chat;
using InsightBase.Application.Commands.Chat;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InsightBase.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ChatController(IMediator mediator) => _mediator = mediator;

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeQuery([FromBody] AnalyzeQueryRequest request)
        {
            var result = await _mediator.Send(new AnalyzeQueryCommand(
                request.Query,
                null
            ));
            return Ok(result);
        }
        
    }
}