using SmartTicketSystemBackend.DTOs.Auth;
using SmartTicketSystemBackend.DTOs.Onboarding;
using SmartTicketSystemBackend.Models;

namespace SmartTicketSystemBackend.Services
{
    public interface IOnboardingService
    {
        Task<AuthResponseDto> SetupOrganizationAsync(OnboardingDto dto);
        Task<User> InviteUserAsync(int organizationId, InviteUserDto dto);
        Task<List<User>> GetTeamMembersAsync(int organizationId);
    }
}
