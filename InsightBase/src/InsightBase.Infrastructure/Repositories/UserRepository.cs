using InsightBase.Application.DTOs.Auth;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using InsightBase.Infrastructure.Persistence;
using InsightBase.Infrastructure.Persistence.Account;
using Microsoft.AspNetCore.Identity;

namespace InsightBase.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UserRepository(UserManager<ApplicationUser> userManager) => (_userManager) = (userManager);
        public async Task<User?> GetUserByIdAsync(string id)
        {
            var identityUser = await _userManager.FindByIdAsync(id);
            return Mappers.UserMapper.ToDomain(identityUser);
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return null;

            return  Mappers.UserMapper.ToDomain(user);
        }
        public async Task<IList<string>> GetRolesById(string id)
        {
            var user =  await _userManager.FindByIdAsync(id);
            var roles = await _userManager.GetRolesAsync(user);
            return roles;
        }
        public Task<List<string>> GetUserOrganizationsAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<string> AddUser(string email, string password)
        {
            var identityUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };
            var result = await _userManager.CreateAsync(identityUser, password);
            if(result.Succeeded) return identityUser.Id;
            return null;
        }

        public async Task<bool> AddRoleToUserById(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            
            var roleResult = await _userManager.AddToRoleAsync(user, UserRoles.User);
            if (!roleResult.Succeeded)
            {
                // Kullanıcı oluşturuldu ama role eklenemedi - rollback yapılabilir
                // await _userManager.DeleteAsync(user);
                return false;
            }
            return true;
        }

        public async Task<bool> PasswordCheck(string userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var passwordCheck = await _userManager.CheckPasswordAsync(user, password);
            if(!passwordCheck) return false;
            return true;
        }

    }
}