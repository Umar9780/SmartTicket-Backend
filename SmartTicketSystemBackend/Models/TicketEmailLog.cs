using System.ComponentModel.DataAnnotations;

namespace SmartTicketSystemBackend.Models
{
    public class TicketEmailLog
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        [Required, MaxLength(200)]
        public string FromEmail { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ToEmail { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public EmailLogStatus Status { get; set; } = EmailLogStatus.Pending;

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
    }
}
