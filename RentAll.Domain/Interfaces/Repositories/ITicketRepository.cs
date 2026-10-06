using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> GetTicketsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Ticket>> GetTicketsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<Ticket?> GetTicketByIdAsync(Guid ticketId, Guid organizationId);

    Task<Ticket> CreateTicketAsync(Ticket ticket);
    Task<Ticket> UpdateTicketAsync(Ticket ticket);
    Task DeleteTicketByIdAsync(Guid ticketId, Guid organizationId, Guid modifiedBy);
    Task<IReadOnlyList<TicketImage>> GetTicketImagesByTicketIdAsync(Guid ticketId);
    Task AddTicketImageAsync(Guid ticketId, string storagePath, Guid createdBy);
    Task DeleteTicketImageByPathAsync(Guid ticketId, string storagePath);
}
