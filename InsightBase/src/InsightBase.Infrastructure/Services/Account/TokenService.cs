using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using SixLabors.ImageSharp;

namespace InsightBase.Infrastructure.Services.Account
{
    public class TokenService : ITokenService
    {
        private readonly Configuration _config;
        private readonly SymmetricSecurityKey _key;
        public TokenService(Configuration config, SymmetricSecurityKey key) => (_config, _key) = (config, key);
        public string Createtoken(User user)
        {
            throw new NotImplementedException();
        }

    }
}