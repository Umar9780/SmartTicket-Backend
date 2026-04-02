namespace SmartTicketSystemBackend.Services
{
    public interface IAiService
    {
        Task<string> SummarizeTicketAsync(string subject, string description, string status,
            string priority, string? assignedTo, List<string> comments);
    }
}
