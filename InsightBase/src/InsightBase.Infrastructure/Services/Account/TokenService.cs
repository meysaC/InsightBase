using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace InsightBase.Infrastructure.Services.Account
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        public TokenService(IConfiguration config)
        {
            _config = config;
            var secret = config["JWT:SIGNINGKEY"];
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }
        public string Createtoken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };
            var tokenHandler = new JwtSecurityTokenHandler(); //JsonWebTokenHandler
            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            token.Header.Add("kid", "5985526266324489"); // Key Id ekliyoruz.
            return tokenHandler.WriteToken(token);
        }

    }
}