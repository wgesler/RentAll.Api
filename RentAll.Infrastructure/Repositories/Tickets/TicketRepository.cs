using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Serialization;
using System.Data;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Tickets;

public class TicketRepository : ITicketRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SqlColumnJsonSerializerOptions.CaseInsensitive;
    private readonly string _dbConnectionString;

    public TicketRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    public async Task<IEnumerable<Ticket>> GetTicketsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Ticket>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Ticket>> GetTicketsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Ticket>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await LoadTicketByIdAsync(db, null, ticketId, organizationId);
    }

    private async Task<Ticket?> LoadTicketByIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid ticketId,
        Guid organizationId)
    {
        var (headers, notes) = await db.DapperProcQueryMultipleAsync<TicketEntity, TicketNote>("Maintenance.Ticket_GetById", new
        {
            TicketId = ticketId,
            OrganizationId = organizationId
        }, transaction: transaction);

        return MapTicketsWithNoteEntities(headers, notes).FirstOrDefault();
    }

    private static List<Ticket> MapTicketsWithNoteEntities(
        IEnumerable<TicketEntity>? ticketEntities,
        IEnumerable<TicketNote>? noteEntities)
    {
        if (ticketEntities == null || !ticketEntities.Any())
            return new List<Ticket>();

        var notesByTicketId = (noteEntities ?? Enumerable.Empty<TicketNote>())
            .GroupBy(note => note.TicketId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(note => note.TicketNoteId)
                    .Select(noteGroup => noteGroup.First())
                    .OrderBy(note => note.CreatedOn)
                    .ToList());

        var tickets = ticketEntities.Select(ConvertEntityToModel).ToList();
        foreach (var ticket in tickets)
        {
            if (notesByTicketId.TryGetValue(ticket.TicketId, out var notes) && notes.Count > 0)
                ticket.Notes = notes;
        }

        return tickets;
    }

    public async Task<Ticket> CreateTicketAsync(Ticket ticket)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var response = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_Add", new
            {
                OrganizationId = ticket.OrganizationId,
                OfficeId = ticket.OfficeId,
                PropertyId = ticket.PropertyId,
                ReservationId = ticket.ReservationId,
                AssigneeId = ticket.AssigneeId,
                AgentId = ticket.AgentId,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                TicketStateId = (int)ticket.TicketStateType,
                NeedPermissionToEnter = ticket.NeedPermissionToEnter,
                PermissionGranted = ticket.PermissionGranted,
                OwnerContacted = ticket.OwnerContacted,
                ConfirmedWithTenant = ticket.ConfirmedWithTenant,
                FollowedUpWithOwner = ticket.FollowedUpWithOwner,
                WorkOrderCompleted = ticket.WorkOrderCompleted,
                IsActive = ticket.IsActive,
                CreatedBy = ticket.CreatedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Ticket not created");

            var createdTicket = ConvertEntityToModel(response.First());

            if (ticket.Notes != null && ticket.Notes.Any())
            {
                foreach (var note in ticket.Notes)
                {
                    await db.DapperProcQueryAsync<object>("Maintenance.TicketNote_Add", new
                    {
                        TicketId = createdTicket.TicketId,
                        Note = note.Note,
                        CreatedBy = ticket.CreatedBy
                    }, transaction: transaction);
                }
            }

            var populated = await LoadTicketByIdAsync(db, transaction, createdTicket.TicketId, createdTicket.OrganizationId);
            if (populated == null)
                throw new Exception("Ticket not found");

            await transaction.CommitAsync();
            return populated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Ticket> UpdateTicketAsync(Ticket ticket)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var currentTicket = await LoadTicketByIdAsync(db, transaction, ticket.TicketId, ticket.OrganizationId);
            if (currentTicket == null)
                throw new Exception("Ticket not found");

            var response = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_UpdateById", new
            {
                TicketId = ticket.TicketId,
                OrganizationId = ticket.OrganizationId,
                OfficeId = ticket.OfficeId,
                PropertyId = ticket.PropertyId,
                ReservationId = ticket.ReservationId,
                AssigneeId = ticket.AssigneeId,
                AgentId = ticket.AgentId,
                TicketCode = ticket.TicketCode,
                Title = ticket.Title,
                Description = ticket.Description,
                TicketStateId = (int)ticket.TicketStateType,
                NeedPermissionToEnter = ticket.NeedPermissionToEnter,
                PermissionGranted = ticket.PermissionGranted,
                OwnerContacted = ticket.OwnerContacted,
                ConfirmedWithTenant = ticket.ConfirmedWithTenant,
                FollowedUpWithOwner = ticket.FollowedUpWithOwner,
                WorkOrderCompleted = ticket.WorkOrderCompleted,
                IsActive = ticket.IsActive,
                ModifiedBy = ticket.ModifiedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Ticket not updated");

            if (ticket.Notes != null && ticket.Notes.Any())
            {
                foreach (var note in ticket.Notes)
                {
                    if (note.TicketNoteId == 0)
                    {
                        await db.DapperProcQueryAsync<object>("Maintenance.TicketNote_Add", new
                        {
                            TicketId = ticket.TicketId,
                            Note = note.Note,
                            CreatedBy = ticket.ModifiedBy
                        }, transaction: transaction);
                    }
                }
            }

            var updatedResult = await LoadTicketByIdAsync(db, transaction, ticket.TicketId, ticket.OrganizationId);
            if (updatedResult == null)
                throw new Exception("Ticket not updated");

            await transaction.CommitAsync();
            return updatedResult;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteTicketByIdAsync(Guid ticketId, Guid organizationId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Ticket_DeleteById", new
        {
            TicketId = ticketId,
            OrganizationId = organizationId,
            ModifiedBy = modifiedBy
        });
    }

    private static Ticket ConvertEntityToModel(TicketEntity e)
    {
        return new Ticket
        {
            TicketId = e.TicketId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            AssigneeId = e.AssigneeId,
            Assignee = e.Assignee,
            AgentId = e.AgentId,
            Agent = e.Agent,
            TicketCode = e.TicketCode,
            Title = e.Title,
            Description = e.Description,
            TicketStateType = (TicketStateType)e.TicketStateId,
            NeedPermissionToEnter = e.NeedPermissionToEnter,
            PermissionGranted = e.PermissionGranted,
            OwnerContacted = e.OwnerContacted,
            ConfirmedWithTenant = e.ConfirmedWithTenant,
            FollowedUpWithOwner = e.FollowedUpWithOwner,
            WorkOrderCompleted = e.WorkOrderCompleted,
            Notes = DeserializeTicketNotes(e.Notes),
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static List<TicketNote> DeserializeTicketNotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<TicketNote>();

        try
        {
            return JsonSerializer.Deserialize<List<TicketNote>>(json, JsonOptions) ?? new List<TicketNote>();
        }
        catch
        {
            return new List<TicketNote>();
        }
    }
}
