using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<Contractor>> GetContractorsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContractorEntity>("Maintenance.Contractor_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Contractor>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Contractor?> GetContractorByIdAsync(Guid contractorId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContractorEntity>("Maintenance.Contractor_GetById", new
        {
            ContractorId = contractorId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<Contractor> CreateContractorAsync(Contractor contractor)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContractorEntity>("Maintenance.Contractor_Add", new
        {
            OrganizationId = contractor.OrganizationId,
            OfficeId = contractor.OfficeId,
            ContractorCode = contractor.ContractorCode,
            Name = contractor.Name,
            Phone = contractor.Phone,
            Website = contractor.Website,
            Rating = contractor.Rating,
            Notes = contractor.Notes,
            IsActive = contractor.IsActive,
            CreatedBy = contractor.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Contractor record not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<Contractor> UpdateContractorAsync(Contractor contractor)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ContractorEntity>("Maintenance.Contractor_UpdateById", new
        {
            ContractorId = contractor.ContractorId,
            OrganizationId = contractor.OrganizationId,
            OfficeId = contractor.OfficeId,
            ContractorCode = contractor.ContractorCode,
            Name = contractor.Name,
            Phone = contractor.Phone,
            Website = contractor.Website,
            Rating = contractor.Rating,
            Notes = contractor.Notes,
            IsActive = contractor.IsActive,
            ModifiedBy = contractor.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Contractor record not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteContractorByIdAsync(Guid contractorId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Contractor_DeleteById", new
        {
            ContractorId = contractorId,
            OrganizationId = organizationId
        });
    }
    #endregion
}
