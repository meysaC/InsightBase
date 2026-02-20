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
                ConfirmPassword = apiDto.ConfirmPassword,
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

        // public static Api.DTOs.Auth.MeResponseDto ApplicationMeDtoToApiMeDto(Application.DTOs.Auth.MeResponseDto userApp)
        // {
        //     var apiMeDto = new MeResponseDto
        //     {
        //         Id = userApp.Id,
        //         Email = userApp.Email,
        //         UserName = userApp.UserName
        //     };
        //     return apiMeDto;
        // }
    }
}