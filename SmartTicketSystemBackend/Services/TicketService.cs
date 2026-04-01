using Microsoft.EntityFrameworkCore;
using SmartTicketSystemBackend.Data;
using SmartTicketSystemBackend.DTOs.Tickets;
using SmartTicketSystemBackend.Models;

namespace SmartTicketSystemBackend.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;

        public TicketService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TicketResponseDto>> GetAllAsync(int organizationId, string? status, string? priority, int? assignedToId)
        {
            var query = _context.Tickets
                .Include(t => t.SubmittedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Comments)
                .Where(t => t.OrganizationId == organizationId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var statusEnum))
                query = query.Where(t => t.Status == statusEnum);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TicketPriority>(priority, out var priorityEnum))
                query = query.Where(t => t.Priority == priorityEnum);

            if (assignedToId.HasValue)
                query = query.Where(t => t.AssignedToId == assignedToId);

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return tickets.Select(MapToDto).ToList();
        }

        public async Task<TicketResponseDto?> GetByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.SubmittedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            return ticket == null ? null : MapToDto(ticket);
        }

        public async Task<TicketResponseDto> CreateAsync(int organizationId, int submittedById, CreateTicketDto dto)
        {
            var ticketNumber = await GenerateTicketNumberAsync(organizationId);

            var ticket = new Ticket
            {
                TicketNumber = ticketNumber,
                Subject = dto.Subject,
                Description = dto.Description,
                Priority = dto.Priority,
                Source = dto.Source,
                Category = dto.Category,
                CustomerEmail = dto.CustomerEmail,
                CustomerName = dto.CustomerName,
                OrganizationId = organizationId,
                SubmittedById = submittedById,
                AssignedToId = dto.AssignedToId
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(ticket.Id))!;
        }

        public async Task<TicketResponseDto?> UpdateAsync(int id, UpdateTicketDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return null;

            if (dto.Subject != null) ticket.Subject = dto.Subject;
            if (dto.Description != null) ticket.Description = dto.Description;
            if (dto.Status.HasValue)
            {
                ticket.Status = dto.Status.Value;
                if (dto.Status.Value == TicketStatus.Resolved)
                    ticket.ResolvedAt = DateTime.UtcNow;
            }
            if (dto.Priority.HasValue) ticket.Priority = dto.Priority.Value;
            if (dto.Category != null) ticket.Category = dto.Category;
            if (dto.AssignedToId.HasValue) ticket.AssignedToId = dto.AssignedToId;

            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return false;
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CommentResponseDto> AddCommentAsync(int ticketId, int userId, AddCommentDto dto)
        {
            var comment = new TicketComment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = dto.Content,
                IsInternal = dto.IsInternal
            };

            _context.TicketComments.Add(comment);

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.UpdatedAt = DateTime.UtcNow;
                if (ticket.Status == TicketStatus.Open)
                    ticket.Status = TicketStatus.InProgress;
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            return new CommentResponseDto
            {
                Id = comment.Id,
                TicketId = ticketId,
                UserId = userId,
                UserName = user?.FullName ?? string.Empty,
                Content = comment.Content,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<TicketStatsDto> GetStatsAsync(int organizationId)
        {
            var tickets = await _context.Tickets
                .Where(t => t.OrganizationId == organizationId)
                .ToListAsync();

            return new TicketStatsDto
            {
                Total = tickets.Count,
                Open = tickets.Count(t => t.Status == TicketStatus.Open),
                InProgress = tickets.Count(t => t.Status == TicketStatus.InProgress),
                Resolved = tickets.Count(t => t.Status == TicketStatus.Resolved),
                Closed = tickets.Count(t => t.Status == TicketStatus.Closed),
                Urgent = tickets.Count(t => t.Priority == TicketPriority.Urgent)
            };
        }

        private async Task<string> GenerateTicketNumberAsync(int organizationId)
        {
            var count = await _context.Tickets.CountAsync(t => t.OrganizationId == organizationId);
            return $"TKT-{organizationId:D3}-{(count + 1):D5}";
        }

        private static TicketResponseDto MapToDto(Ticket t) => new()
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            Subject = t.Subject,
            Description = t.Description,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            Source = t.Source.ToString(),
            Category = t.Category,
            CustomerEmail = t.CustomerEmail,
            CustomerName = t.CustomerName,
            OrganizationId = t.OrganizationId,
            SubmittedById = t.SubmittedById,
            SubmittedByName = t.SubmittedBy?.FullName ?? string.Empty,
            AssignedToId = t.AssignedToId,
            AssignedToName = t.AssignedTo?.FullName,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            ResolvedAt = t.ResolvedAt,
            CommentCount = t.Comments?.Count ?? 0,
            Comments = t.Comments?.Select(c => new CommentResponseDto
            {
                Id = c.Id,
                TicketId = c.TicketId,
                UserId = c.UserId,
                UserName = c.User?.FullName ?? string.Empty,
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt
            }).ToList() ?? new()
        };
    }
}
