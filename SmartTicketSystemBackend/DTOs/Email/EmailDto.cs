using System.ComponentModel.DataAnnotations;

namespace SmartTicketSystemBackend.DTOs.Email
{
    public class SendEmailDto
    {
        [Required, EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;
    }

    public class CreateTicketFromEmailDto
    {
        [Required, EmailAddress]
        public string FromEmail { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string FromName { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;
    }

    public class EmailLogResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
