using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<Terminology>> GetTerminologiesByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TerminologyEntity>("Organization.Terminology_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Terminology>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Terminology?> GetTerminologyByIdAsync(int termId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TerminologyEntity>("Organization.Terminology_GetById", new
        {
            TermId = termId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Terminology?> GetTerminologyByTermAsync(string term, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TerminologyEntity>("Organization.Terminology_GetByTerm", new
        {
            Term = term,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<Terminology> CreateTerminologyAsync(Terminology terminology)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TerminologyEntity>("Organization.Terminology_Add", new
        {
            OrganizationId = terminology.OrganizationId,
            OfficeId = terminology.OfficeId,
            Term = terminology.Term,
            Replacement = terminology.Replacement
        });

        if (res == null || !res.Any())
            throw new Exception("Terminology not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<Terminology> UpdateTerminologyByIdAsync(Terminology terminology)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TerminologyEntity>("Organization.Terminology_UpdateById", new
        {
            TermId = terminology.TermId,
            OrganizationId = terminology.OrganizationId,
            OfficeId = terminology.OfficeId,
            Term = terminology.Term,
            Replacement = terminology.Replacement
        });

        if (res == null || !res.Any())
            throw new Exception("Terminology not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteTerminologyByIdAsync(int termId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Terminology_DeleteById", new
        {
            TermId = termId,
            OrganizationId = organizationId
        });
    }
    #endregion
}
