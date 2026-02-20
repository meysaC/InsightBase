using System.Security.Claims;
using DocumentFormat.OpenXml.InkML;
using InsightBase.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace InsightBase.Infrastructure.Services.Account
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _context;
        public CurrentUserService(IHttpContextAccessor context) => _context = context;
        public string? UserId => 
                        _context.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //ClaimTypes.NameIdentifier -> jwt claim'den HttpContext.User.Claims okunuyor 
    }
}