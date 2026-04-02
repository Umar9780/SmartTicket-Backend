using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTicketSystemBackend.DTOs.Tickets;
using SmartTicketSystemBackend.Services;

namespace SmartTicketSystemBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IAiService _aiService;

        public TicketsController(ITicketService ticketService, IAiService aiService)
        {
            _ticketService = ticketService;
            _aiService = aiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] int? assignedToId)
        {
            var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
            return Ok(await _ticketService.GetAllAsync(orgId, status, priority, assignedToId));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
            return Ok(await _ticketService.GetStatsAsync(orgId));
        }

        [HttpGet("activities")]
        public async Task<IActionResult> GetActivities([FromQuery] int count = 20)
        {
            var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
            return Ok(await _ticketService.GetRecentActivitiesAsync(orgId, count));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket == null) return NotFound();
            return Ok(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
        {
            var orgId = int.Parse(User.FindFirstValue("OrganizationId")!);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ticket = await _ticketService.CreateAsync(orgId, userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ticket = await _ticketService.UpdateAsync(id, dto, userId);
            if (ticket == null) return NotFound();
            return Ok(ticket);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _ticketService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/summarize")]
        public async Task<IActionResult> Summarize(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var comments = ticket.Comments.Select(c => $"{c.UserName}: {c.Content}").ToList();
            var summary = await _aiService.SummarizeTicketAsync(
                ticket.Subject, ticket.Description,
                ticket.Status, ticket.Priority,
                ticket.AssignedToName, comments);

            return Ok(new { summary });
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var comment = await _ticketService.AddCommentAsync(id, userId, dto);
            return Ok(comment);
        }
    }
}
