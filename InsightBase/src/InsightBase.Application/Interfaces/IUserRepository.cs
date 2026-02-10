using InsightBase.Application.DTOs.Auth;
using InsightBase.Domain.Entities;

namespace InsightBase.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<string> AddUser(string email, string password);
        Task<bool> AddRoleToUserById(string userId);
        Task<bool> PasswordCheck(string userId, string password);
        Task<User?> GetUserByIdAsync(string id);
        Task<List<string>> GetUserOrganizationsAsync(string userId);
        Task<IList<string>> GetRolesById(string id);
        Task<User?> GetUserByEmailAsync(string email);
    }
}