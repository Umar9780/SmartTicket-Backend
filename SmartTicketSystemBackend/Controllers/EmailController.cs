using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTicketSystemBackend.DTOs.Email;
using SmartTicketSystemBackend.Services;

namespace SmartTicketSystemBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("tickets/{ticketId}/send")]
        public async Task<IActionResult> SendEmail(int ticketId, [FromBody] SendEmailDto dto)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email)!;
            var result = await _emailService.SendTicketEmailAsync(ticketId, dto, userEmail);
            return Ok(result);
        }

        [HttpPost("inbound")]
        [AllowAnonymous]
        public async Task<IActionResult> InboundEmail([FromQuery] int organizationId, [FromBody] CreateTicketFromEmailDto dto)
        {
            var result = await _emailService.CreateTicketFromEmailAsync(organizationId, dto);
            return Ok(result);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
            var logs = await _emailService.GetEmailLogsAsync(orgId);
            return Ok(logs);
        }

        [HttpGet("tickets/{ticketId}/logs")]
        public async Task<IActionResult> GetLogsByTicket(int ticketId)
        {
            var logs = await _emailService.GetEmailLogsByTicketAsync(ticketId);
            return Ok(logs);
        }
    }
}
