using InsightBase.Domain.Entities;

namespace InsightBase.Application.Mapper
{
    public class AuthMapper
    {
        public static DTOs.Auth.MeResponseDto UserDomainToMeResponseDto(User userDomain)
        {
            var meDto = new DTOs.Auth.MeResponseDto
            {
                Id = userDomain.Id,
                Email = userDomain.Email,
                UserName = userDomain.UserName
            };
            return meDto;
        }
    }
}