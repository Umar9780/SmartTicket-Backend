using System.ComponentModel.DataAnnotations;

namespace SmartTicketSystemBackend.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketSource Source { get; set; } = TicketSource.Web;

        [MaxLength(100)]
        public string? Category { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        public int SubmittedById { get; set; }
        public User SubmittedBy { get; set; } = null!;

        public int? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        [MaxLength(500)]
        public string? CustomerEmail { get; set; }

        [MaxLength(200)]
        public string? CustomerName { get; set; }

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public ICollection<TicketEmailLog> EmailLogs { get; set; } = new List<TicketEmailLog>();
    }
}
