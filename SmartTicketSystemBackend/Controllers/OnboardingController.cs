using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTicketSystemBackend.DTOs.Onboarding;
using SmartTicketSystemBackend.Services;

namespace SmartTicketSystemBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        [HttpPost("setup")]
        public async Task<IActionResult> Setup([FromBody] OnboardingDto dto)
        {
            var result = await _onboardingService.SetupOrganizationAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            try
            {
                var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
                var user = await _onboardingService.InviteUserAsync(orgId, dto);
                return Ok(new { user.Id, user.FullName, user.Email, Role = user.Role.ToString() });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("team")]
        public async Task<IActionResult> GetTeam()
        {
            var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
            var members = await _onboardingService.GetTeamMembersAsync(orgId);
            return Ok(members.Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                u.IsActive,
                u.CreatedAt
            }));
        }
    }
}
