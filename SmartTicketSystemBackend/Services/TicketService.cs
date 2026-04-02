using Microsoft.EntityFrameworkCore;
using SmartTicketSystemBackend.Data;
using SmartTicketSystemBackend.DTOs.Tickets;
using SmartTicketSystemBackend.Models;

namespace SmartTicketSystemBackend.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;

        public TicketService(AppDbContext context) => _context = context;

        public async Task<List<TicketResponseDto>> GetAllAsync(int organizationId, string? status, string? priority, int? assignedToId)
        {
            var query = _context.Tickets
                .Include(t => t.SubmittedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Comments)
                .Where(t => t.OrganizationId == organizationId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var s))
                query = query.Where(t => t.Status == s);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TicketPriority>(priority, out var p))
                query = query.Where(t => t.Priority == p);

            if (assignedToId.HasValue)
                query = query.Where(t => t.AssignedToId == assignedToId);

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return tickets.Select(t => MapToDto(t, false)).ToList();
        }

        public async Task<TicketResponseDto?> GetByIdAsync(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.SubmittedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Activities).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            return ticket == null ? null : MapToDto(ticket, true);
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
                AssignedToId = dto.AssignedToId,
                DueDate = dto.DueDate,
                Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Log creation activity
            await LogActivityAsync(ticket.Id, submittedById, ActivityType.TicketCreated,
                null, ticket.Subject, $"Ticket {ticketNumber} created");

            return (await GetByIdAsync(ticket.Id))!;
        }

        public async Task<TicketResponseDto?> UpdateAsync(int id, UpdateTicketDto dto, int updatedByUserId)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return null;

            if (dto.Subject != null && dto.Subject != ticket.Subject)
            {
                await LogActivityAsync(id, updatedByUserId, ActivityType.SubjectChanged, ticket.Subject, dto.Subject);
                ticket.Subject = dto.Subject;
            }

            if (dto.Description != null) ticket.Description = dto.Description;

            if (dto.Status.HasValue && dto.Status.Value != ticket.Status)
            {
                await LogActivityAsync(id, updatedByUserId, ActivityType.StatusChanged,
                    ticket.Status.ToString(), dto.Status.Value.ToString());
                ticket.Status = dto.Status.Value;
                if (dto.Status.Value == TicketStatus.Resolved)
                    ticket.ResolvedAt = DateTime.UtcNow;
            }

            if (dto.Priority.HasValue && dto.Priority.Value != ticket.Priority)
            {
                await LogActivityAsync(id, updatedByUserId, ActivityType.PriorityChanged,
                    ticket.Priority.ToString(), dto.Priority.Value.ToString());
                ticket.Priority = dto.Priority.Value;
            }

            if (dto.AssignedToId.HasValue && dto.AssignedToId != ticket.AssignedToId)
            {
                var newAssignee = dto.AssignedToId > 0
                    ? (await _context.Users.FindAsync(dto.AssignedToId))?.FullName : "Unassigned";
                await LogActivityAsync(id, updatedByUserId, ActivityType.AssigneeChanged,
                    ticket.AssignedToId?.ToString(), newAssignee);
                ticket.AssignedToId = dto.AssignedToId == 0 ? null : dto.AssignedToId;
            }

            if (dto.DueDate != ticket.DueDate)
            {
                await LogActivityAsync(id, updatedByUserId, ActivityType.DueDateChanged,
                    ticket.DueDate?.ToString("yyyy-MM-dd"), dto.DueDate?.ToString("yyyy-MM-dd"));
                ticket.DueDate = dto.DueDate;
            }

            if (dto.Tags != null)
            {
                var newTags = string.Join(",", dto.Tags);
                if (newTags != (ticket.Tags ?? ""))
                {
                    await LogActivityAsync(id, updatedByUserId, ActivityType.TagsChanged,
                        ticket.Tags, newTags);
                    ticket.Tags = newTags;
                }
            }

            if (dto.Category != null) ticket.Category = dto.Category;

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
            await LogActivityAsync(ticketId, userId, ActivityType.CommentAdded,
                null, dto.IsInternal ? "Internal note" : "Public comment",
                dto.Content.Length > 80 ? dto.Content[..80] + "…" : dto.Content);

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

            var resolved = tickets.Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt.HasValue).ToList();
            var avgHours = resolved.Any()
                ? resolved.Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                : 0;

            return new TicketStatsDto
            {
                Total = tickets.Count,
                Open = tickets.Count(t => t.Status == TicketStatus.Open),
                InProgress = tickets.Count(t => t.Status == TicketStatus.InProgress),
                Resolved = tickets.Count(t => t.Status == TicketStatus.Resolved),
                Closed = tickets.Count(t => t.Status == TicketStatus.Closed),
                Urgent = tickets.Count(t => t.Priority == TicketPriority.Urgent),
                Overdue = tickets.Count(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow
                                          && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),
                AvgResolutionHours = Math.Round(avgHours, 1)
            };
        }

        public async Task<List<ActivityResponseDto>> GetRecentActivitiesAsync(int organizationId, int count = 20)
        {
            var activities = await _context.TicketActivities
                .Include(a => a.User)
                .Include(a => a.Ticket)
                .Where(a => a.Ticket.OrganizationId == organizationId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();

            return activities.Select(MapActivityToDto).ToList();
        }

        // ── Private helpers ────────────────────────
        private async Task LogActivityAsync(int ticketId, int userId, ActivityType type,
            string? oldValue, string? newValue, string? description = null)
        {
            _context.TicketActivities.Add(new TicketActivity
            {
                TicketId = ticketId,
                UserId = userId,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description
            });
            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateTicketNumberAsync(int organizationId)
        {
            var count = await _context.Tickets.CountAsync(t => t.OrganizationId == organizationId);
            return $"TKT-{organizationId:D3}-{(count + 1):D5}";
        }

        private static TicketResponseDto MapToDto(Ticket t, bool includeActivities) => new()
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
            DueDate = t.DueDate,
            Tags = string.IsNullOrEmpty(t.Tags)
                ? new List<string>()
                : t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            CommentCount = t.Comments?.Count ?? 0,
            Comments = t.Comments?.Select(c => new CommentResponseDto
            {
                Id = c.Id, TicketId = c.TicketId, UserId = c.UserId,
                UserName = c.User?.FullName ?? string.Empty,
                Content = c.Content, IsInternal = c.IsInternal, CreatedAt = c.CreatedAt
            }).ToList() ?? new(),
            Activities = includeActivities
                ? t.Activities?.OrderByDescending(a => a.CreatedAt).Select(MapActivityToDto).ToList() ?? new()
                : new()
        };

        private static ActivityResponseDto MapActivityToDto(TicketActivity a) => new()
        {
            Id = a.Id,
            TicketId = a.TicketId,
            TicketNumber = a.Ticket?.TicketNumber ?? string.Empty,
            UserId = a.UserId,
            UserName = a.User?.FullName ?? string.Empty,
            Type = a.Type.ToString(),
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            Description = a.Description,
            CreatedAt = a.CreatedAt
        };
    }
}
