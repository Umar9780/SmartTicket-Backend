using SmartTicketSystemBackend.DTOs.Tickets;

namespace SmartTicketSystemBackend.Services
{
    public interface ITicketService
    {
        Task<List<TicketResponseDto>> GetAllAsync(int organizationId, string? status, string? priority, int? assignedToId);
        Task<TicketResponseDto?> GetByIdAsync(int id);
        Task<TicketResponseDto> CreateAsync(int organizationId, int submittedById, CreateTicketDto dto);
        Task<TicketResponseDto?> UpdateAsync(int id, UpdateTicketDto dto, int updatedByUserId);
        Task<bool> DeleteAsync(int id);
        Task<CommentResponseDto> AddCommentAsync(int ticketId, int userId, AddCommentDto dto);
        Task<TicketStatsDto> GetStatsAsync(int organizationId);
        Task<List<ActivityResponseDto>> GetRecentActivitiesAsync(int organizationId, int count = 20);
    }
}
