namespace SmartTicketSystemBackend.Models
{
    public enum UserRole { Admin, Agent, Customer }
    public enum TicketStatus { Open, InProgress, Resolved, Closed }
    public enum TicketPriority { Low, Medium, High, Urgent }
    public enum TicketSource { Web, Email, Manual }
    public enum EmailLogStatus { Pending, Sent, Failed }
}
