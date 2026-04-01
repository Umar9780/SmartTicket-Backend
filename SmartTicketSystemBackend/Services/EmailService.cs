using Microsoft.EntityFrameworkCore;
using SmartTicketSystemBackend.Data;
using SmartTicketSystemBackend.DTOs.Email;
using SmartTicketSystemBackend.Models;

namespace SmartTicketSystemBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmailService> _logger;
        private readonly ITicketService _ticketService;

        public EmailService(AppDbContext context, ILogger<EmailService> logger, ITicketService ticketService)
        {
            _context = context;
            _logger = logger;
            _ticketService = ticketService;
        }

        public async Task<EmailLogResponseDto> SendTicketEmailAsync(int ticketId, SendEmailDto dto, string fromEmail)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId)
                ?? throw new KeyNotFoundException($"Ticket {ticketId} not found");

            var log = new TicketEmailLog
            {
                TicketId = ticketId,
                FromEmail = fromEmail,
                ToEmail = dto.ToEmail,
                Subject = dto.Subject,
                Body = dto.Body,
                Status = EmailLogStatus.Pending
            };

            _context.TicketEmailLogs.Add(log);
            await _context.SaveChangesAsync();

            // Simulate email sending
            try
            {
                _logger.LogInformation("Sending email to {To} for ticket {TicketNumber}", dto.ToEmail, ticket.TicketNumber);
                await Task.Delay(100); // simulate async send

                log.Status = EmailLogStatus.Sent;
                log.SentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Email sent successfully to {To}", dto.ToEmail);
            }
            catch (Exception ex)
            {
                log.Status = EmailLogStatus.Failed;
                log.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
            }

            return MapToDto(log, ticket.TicketNumber);
        }

        public async Task<EmailLogResponseDto> CreateTicketFromEmailAsync(int organizationId, CreateTicketFromEmailDto dto)
        {
            // Find or create system user for the org
            var systemUser = await _context.Users
                .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Role == UserRole.Admin);

            if (systemUser == null)
                throw new InvalidOperationException("No admin found for organization");

            // Create ticket from email
            var ticketDto = new DTOs.Tickets.CreateTicketDto
            {
                Subject = dto.Subject,
                Description = dto.Body,
                Source = TicketSource.Email,
                Priority = TicketPriority.Medium,
                CustomerEmail = dto.FromEmail,
                CustomerName = dto.FromName
            };

            var ticket = await _ticketService.CreateAsync(organizationId, systemUser.Id, ticketDto);

            // Log the inbound email
            var ticketEntity = await _context.Tickets.FindAsync(ticket.Id);
            var log = new TicketEmailLog
            {
                TicketId = ticket.Id,
                FromEmail = dto.FromEmail,
                ToEmail = "support@smartticket.com",
                Subject = dto.Subject,
                Body = dto.Body,
                Status = EmailLogStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            _context.TicketEmailLogs.Add(log);
            await _context.SaveChangesAsync();

            return MapToDto(log, ticket.TicketNumber);
        }

        public async Task<List<EmailLogResponseDto>> GetEmailLogsAsync(int organizationId)
        {
            var logs = await _context.TicketEmailLogs
                .Include(e => e.Ticket)
                .Where(e => e.Ticket.OrganizationId == organizationId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return logs.Select(e => MapToDto(e, e.Ticket.TicketNumber)).ToList();
        }

        public async Task<List<EmailLogResponseDto>> GetEmailLogsByTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            var logs = await _context.TicketEmailLogs
                .Where(e => e.TicketId == ticketId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return logs.Select(e => MapToDto(e, ticket?.TicketNumber ?? string.Empty)).ToList();
        }

        private static EmailLogResponseDto MapToDto(TicketEmailLog e, string ticketNumber) => new()
        {
            Id = e.Id,
            TicketId = e.TicketId,
            TicketNumber = ticketNumber,
            FromEmail = e.FromEmail,
            ToEmail = e.ToEmail,
            Subject = e.Subject,
            Body = e.Body,
            Status = e.Status.ToString(),
            ErrorMessage = e.ErrorMessage,
            CreatedAt = e.CreatedAt,
            SentAt = e.SentAt
        };
    }
}
