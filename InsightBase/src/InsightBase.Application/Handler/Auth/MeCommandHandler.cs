using InsightBase.Application.Commands.Auth;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using InsightBase.Domain.Entities;
using MediatR;

namespace InsightBase.Application.Handler.Auth
{
    public class MeCommandHandler : IRequestHandler<MeCommand, Result<DTOs.Auth.MeResponseDto>>
    {
        private readonly IUserRepository _userRepo;
        private readonly ICurrentUserService _currentUser;
        public MeCommandHandler(IUserRepository userRepo, ICurrentUserService currentUser) => (_userRepo, _currentUser) = (userRepo, currentUser);
        public async Task<Result<DTOs.Auth.MeResponseDto>> Handle(MeCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId;
            if(userId is null ) return Result<DTOs.Auth.MeResponseDto>.Fail("Unauthorized");

            var user = await _userRepo.GetUserByIdAsync(userId); 
            if(user is null) return Result<DTOs.Auth.MeResponseDto>.Fail("Kullanıcı Bulunamadı");

            return Result<DTOs.Auth.MeResponseDto>.Success(Mapper.AuthMapper.UserDomainToMeResponseDto(user));
        }

    }
}