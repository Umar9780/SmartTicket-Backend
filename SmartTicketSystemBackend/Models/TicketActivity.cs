using System.ComponentModel.DataAnnotations;

namespace SmartTicketSystemBackend.Models
{
    public enum ActivityType
    {
        TicketCreated,
        StatusChanged,
        PriorityChanged,
        AssigneeChanged,
        CommentAdded,
        TagsChanged,
        DueDateChanged,
        SubjectChanged
    }

    public class TicketActivity
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ActivityType Type { get; set; }

        [MaxLength(200)]
        public string? OldValue { get; set; }

        [MaxLength(200)]
        public string? NewValue { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
