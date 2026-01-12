using InsightBase.Api.DTOs.Auth;
using InsightBase.Application.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace InsightBase.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator) => _mediator = mediator;
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto dto)
        {
            var result = await _mediator.Send(new RegisterUserCommand(Mappers.AuthMapper.RegisterApiDtoToApplicationRegister(dto)));
            return result.IsSuccess ? Ok(result) : BadRequest(result.Errors);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var result = await _mediator.Send(new LoginUserCommand(Mappers.AuthMapper.LoginApiDtoToApplicationLogin(dto)));
            return result.IsSuccess ? Ok(result) : BadRequest(result.Errors);
        }

    }
}