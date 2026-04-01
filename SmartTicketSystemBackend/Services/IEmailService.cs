using SmartTicketSystemBackend.DTOs.Email;

namespace SmartTicketSystemBackend.Services
{
    public interface IEmailService
    {
        Task<EmailLogResponseDto> SendTicketEmailAsync(int ticketId, SendEmailDto dto, string fromEmail);
        Task<EmailLogResponseDto> CreateTicketFromEmailAsync(int organizationId, CreateTicketFromEmailDto dto);
        Task<List<EmailLogResponseDto>> GetEmailLogsAsync(int organizationId);
        Task<List<EmailLogResponseDto>> GetEmailLogsByTicketAsync(int ticketId);
    }
}
