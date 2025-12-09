using InsightBase.Domain.Entities;

namespace InsightBase.Application.Interfaces
{
    public interface ITokenService
    {
        string Createtoken(User user);
    }
}