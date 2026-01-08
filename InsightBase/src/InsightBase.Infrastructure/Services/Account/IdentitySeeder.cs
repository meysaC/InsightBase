using InsightBase.Application.Interfaces;
using InsightBase.Infrastructure.Persistence;
using InsightBase.Infrastructure.Persistence.Account;
using Microsoft.AspNetCore.Identity;

namespace InsightBase.Infrastructure.Services.Account
{
    public class IdentitySeeder : IIdentitySeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public IdentitySeeder(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager) => (_roleManager, _userManager) = (roleManager, userManager);
        public async Task SeedAsync()
        {
            await SeedRoles();
            await SeedAdminUser();
        }

        private async Task SeedRoles()
        {
            var roles = new[]
            {
                UserRoles.Developer,
                UserRoles.Admin,
                UserRoles.User,
            };

            foreach (var role in roles)
            {
                if(!await _roleManager.RoleExistsAsync(role)) await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }


        private async Task SeedAdminUser()
        {
            var adminMail = "blackworld752@gmail.com";

            var user = await _userManager.FindByEmailAsync(adminMail);
            
            if(user == null)
            {            
                user = new ApplicationUser
                {
                    UserName = adminMail,
                    Email = adminMail,
                    EmailConfirmed = true
                };
            }
            var result = await _userManager.CreateAsync(user, "Admin123!");
            if (!result.Succeeded) throw new Exception("Admin user could not be created");

            await _userManager.AddToRoleAsync(user, UserRoles.Admin);
        }
    }
}