using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Serialization;
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
        var res = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetListByOfficeIds", new
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
        var res = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetListByPropertyId", new
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
        var res = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetById", new
        {
            TicketId = ticketId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
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
                ReservationCode = ticket.ReservationCode,
                TicketCode = ticket.TicketCode,
                Description = ticket.Description,
                TicketStateId = (int)ticket.TicketStateType,
                PermissionToEnter = ticket.PermissionToEnter,
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

            var populated = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetById", new
            {
                TicketId = createdTicket.TicketId,
                OrganizationId = createdTicket.OrganizationId
            }, transaction: transaction);

            if (populated == null || !populated.Any())
                throw new Exception("Ticket not found");

            await transaction.CommitAsync();
            return ConvertEntityToModel(populated.First());
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
            var currentTicketResult = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetById", new
            {
                TicketId = ticket.TicketId,
                OrganizationId = ticket.OrganizationId
            }, transaction: transaction);

            if (currentTicketResult == null || !currentTicketResult.Any())
                throw new Exception("Ticket not found");

            var currentTicket = ConvertEntityToModel(currentTicketResult.First());
            var currentTicketNoteIds = currentTicket.Notes.Select(note => note.TicketNoteId).ToHashSet();
            var incomingTicketNoteIds = ticket.Notes.Where(note => note.TicketNoteId != 0).Select(note => note.TicketNoteId).ToHashSet();

            var response = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_UpdateById", new
            {
                TicketId = ticket.TicketId,
                OrganizationId = ticket.OrganizationId,
                OfficeId = ticket.OfficeId,
                PropertyId = ticket.PropertyId,
                ReservationId = ticket.ReservationId,
                ReservationCode = ticket.ReservationCode,
                TicketCode = ticket.TicketCode,
                Description = ticket.Description,
                TicketStateId = (int)ticket.TicketStateType,
                PermissionToEnter = ticket.PermissionToEnter,
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
                    else if (currentTicketNoteIds.Contains(note.TicketNoteId))
                    {
                        await db.DapperProcQueryAsync<object>("Maintenance.TicketNote_UpdateById", new
                        {
                            TicketNoteId = note.TicketNoteId,
                            TicketId = ticket.TicketId,
                            Note = note.Note,
                            ModifiedBy = ticket.ModifiedBy
                        }, transaction: transaction);
                    }
                }
            }

            var notesToDelete = currentTicketNoteIds.Except(incomingTicketNoteIds).ToList();
            foreach (var ticketNoteId in notesToDelete)
            {
                await db.DapperProcExecuteAsync("Maintenance.TicketNote_DeleteById", new
                {
                    TicketNoteId = ticketNoteId
                }, transaction: transaction);
            }

            var updatedResult = await db.DapperProcQueryAsync<TicketEntity>("Maintenance.Ticket_GetById", new
            {
                TicketId = ticket.TicketId,
                OrganizationId = ticket.OrganizationId
            }, transaction: transaction);

            if (updatedResult == null || !updatedResult.Any())
                throw new Exception("Ticket not updated");

            await transaction.CommitAsync();
            return ConvertEntityToModel(updatedResult.First());
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
            TicketCode = e.TicketCode,
            Description = e.Description,
            TicketStateType = (TicketStateType)e.TicketStateId,
            PermissionToEnter = e.PermissionToEnter,
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
