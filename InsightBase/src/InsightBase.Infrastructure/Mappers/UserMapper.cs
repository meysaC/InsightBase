using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;

namespace InsightBase.Infrastructure.Mappers
{
    public class UserMapper
    {
        public static User ToDomain(ApplicationUser entity) 
        {
            var user = new User
            {
                Id = entity.Id,
                Email = entity.Email,
                UserName = entity.UserName
            };
            return user;
        }

        public static ApplicationUser ToIdentityUser(User domain)
        {
            var applicationUser = new ApplicationUser
            {
                Id = domain.Id,
                Email = domain.Email,
                UserName = domain.UserName
            };
            return applicationUser;
        }
    }
}