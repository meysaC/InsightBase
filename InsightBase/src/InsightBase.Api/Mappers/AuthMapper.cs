using InsightBase.Api.DTOs.Auth;

namespace InsightBase.Api.Mappers
{
    public class AuthMapper
    {
        public static Application.DTOs.Auth.RegisterDto RegisterApiDtoToApplicationRegister(RegisterRequestDto apiDto)
        {
            var register = new Application.DTOs.Auth.RegisterDto
            {
                Email = apiDto.Email,
                Password = apiDto.Password,
                FullName = apiDto.FullName
            };
            return register;
        }

        public static Application.DTOs.Auth.LoginDto LoginApiDtoToApplicationLogin(LoginRequestDto apiDto)
        {
            var login = new Application.DTOs.Auth.LoginDto
            {
                Email = apiDto.Email,
                Password = apiDto.Password
            };
            return login;
        }
    }
}