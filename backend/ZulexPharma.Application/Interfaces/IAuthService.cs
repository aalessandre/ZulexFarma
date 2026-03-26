using ZulexPharma.Application.DTOs.Auth;

namespace ZulexPharma.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
}
