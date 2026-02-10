using System.ComponentModel.Design;
using DocumentFormat.OpenXml.Drawing.Charts;
using InsightBase.Api.DTOs.Auth;
using InsightBase.Application.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

            if (!result.IsSuccess) return BadRequest(result);
            SetRefreshTokenCookie(result.RefreshToken!);
            return Ok(new { token = result.Token }); // Sadece access tokenı döndürüyoruz
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var result = await _mediator.Send(new LoginUserCommand(Mappers.AuthMapper.LoginApiDtoToApplicationLogin(dto)));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        // [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _mediator.Send(new LogoutUserCommand(refreshToken));
            }
            DeleteRefreshTokenCookie();
            return Ok(new { message = "Çıkış başarılı" });

        }
        // [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> FetchMe()
        {
            var result = await _mediator.Send(new MeCommand());
            return result.IsSuccess ? Ok(result.Value) : Unauthorized();
        }
        
        // !!!!!!!!!!!!!! POST -> /auth/reset-password  /auth/change-password  /auth/confirm-email
        
        // [HttpPost("forget-password")] 
        // public async Task<IActionResult> ForgetPassword()
        // {
        //     var result = await _mediator.Send(new ForgetPasswordCommand());
        //     return result.IsSuccess ? Ok(result) : BadRequest(result);
        // }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized();

            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));

            if (!result.IsSuccess)
                return Unauthorized();

            SetRefreshTokenCookie(result.RefreshToken!);

            return Ok(new { token = result.Token });
        }
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/api/auth/refresh-token" // Sadece bu end pointe gönderilir
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth/refresh-token"
            });
        }
    }
}