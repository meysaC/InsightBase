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
        public string? Id
        {
            get
            {
                var user = _context.HttpContext?.User;
                if(user?.Identity?.IsAuthenticated == true) return user.FindFirstValue(ClaimTypes.NameIdentifier); //?.Value 

                return _context.HttpContext?.Items["AnonymousUserId"]?.ToString(); // Anonymous kullanıcılar için HttpContext.Items'ta tutulan geçici Id'yi döndür
            }
        }
    }
}