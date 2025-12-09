namespace InsightBase.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<List<string>> GetUserOrganizationsAsync(string userId);
        Task<List<string>> GetUserRolesAsync(string userId);
    }
}