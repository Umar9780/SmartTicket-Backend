using System.ComponentModel.DataAnnotations;
using SmartTicketSystemBackend.Models;

namespace SmartTicketSystemBackend.DTOs.Tickets
{
    public class CreateTicketDto
    {
        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketSource Source { get; set; } = TicketSource.Web;

        [MaxLength(100)]
        public string? Category { get; set; }

        public int? AssignedToId { get; set; }

        [MaxLength(500)]
        public string? CustomerEmail { get; set; }

        [MaxLength(200)]
        public string? CustomerName { get; set; }

        public DateTime? DueDate { get; set; }

        public List<string>? Tags { get; set; }
    }

    public class UpdateTicketDto
    {
        [MaxLength(300)]
        public string? Subject { get; set; }

        public string? Description { get; set; }

        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public int? AssignedToId { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerName { get; set; }
        public int OrganizationId { get; set; }
        public int SubmittedById { get; set; }
        public string SubmittedByName { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string> Tags { get; set; } = new();
        public int CommentCount { get; set; }
        public List<CommentResponseDto> Comments { get; set; } = new();
        public List<ActivityResponseDto> Activities { get; set; } = new();
    }

    public class AddCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TicketStatsDto
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
        public int Urgent { get; set; }
        public int Overdue { get; set; }
        public double AvgResolutionHours { get; set; }
    }
}
