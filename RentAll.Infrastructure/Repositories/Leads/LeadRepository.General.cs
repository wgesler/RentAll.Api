using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<IEnumerable<LeadGeneral>> GetGeneralsByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<GeneralEntity>("Lead.General_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
        {
            return Enumerable.Empty<LeadGeneral>();
        }

        return res.Select(ConvertGeneralEntityToModel);
    }

    public async Task<LeadGeneral?> GetGeneralByIdAsync(int generalId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<GeneralEntity>("Lead.General_GetById", new { GeneralId = generalId });

        if (res == null || !res.Any())
        {
            return null;
        }

        return ConvertGeneralEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<LeadGeneral> CreateGeneralAsync(LeadGeneral lead)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<GeneralEntity>("Lead.General_Add", new
        {
            OrganizationId = lead.OrganizationId,
            OfficeId = lead.OfficeId,
            LeadStateId = (int)lead.LeadState,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            PhoneMobile = lead.PhoneMobile,
            Message = lead.Message,
            IsActive = lead.IsActive
        });

        if (res == null || !res.Any())
        {
            throw new InvalidOperationException("General lead was not created.");
        }

        return ConvertGeneralEntityToModel(res.First());
    }

    #endregion

    #region Updates

    public async Task<LeadGeneral> UpdateGeneralByIdAsync(LeadGeneral lead)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<GeneralEntity>("Lead.General_UpdateById", new
        {
            GeneralId = lead.GeneralId,
            OrganizationId = lead.OrganizationId,
            OfficeId = lead.OfficeId,
            LeadStateId = (int)lead.LeadState,
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            PhoneMobile = lead.PhoneMobile,
            Message = lead.Message,
            IsActive = lead.IsActive
        });

        if (res == null || !res.Any())
        {
            throw new InvalidOperationException("General lead was not found or not updated.");
        }

        return ConvertGeneralEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeleteGeneralByIdAsync(int generalId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Lead.General_DeleteById", new { GeneralId = generalId });
    }

    #endregion
}
