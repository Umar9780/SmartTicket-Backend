using SmartTicketSystemBackend.DTOs.Auth;

namespace SmartTicketSystemBackend.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    }
}
