using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Create
    public async Task<Office> CreateAsync(Office office)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_Add", new
        {
            OrganizationId = office.OrganizationId,
            OfficeCode = office.OfficeCode,
            Name = office.Name,
            Address1 = office.Address1,
            Address2 = office.Address2,
            Suite = office.Suite,
            City = office.City,
            State = office.State,
            Zip = office.Zip,
            Phone = office.Phone,
            Fax = office.Fax,
            Website = office.Website,
            LogoPath = office.LogoPath,
            MaintenanceEmail = office.MaintenanceEmail,
            AfterHoursPhone = office.AfterHoursPhone,
            AfterHoursInstructions = office.AfterHoursInstructions,
            DaysToRefundDeposit = office.DaysToRefundDeposit,
            DefaultDeposit = office.DefaultDeposit,
            DefaultSdw = office.DefaultSdw,
            DefaultKeyFee = office.DefaultKeyFee,
            UndisclosedPetFee = office.UndisclosedPetFee,
            MinimumSmokingFee = office.MinimumSmokingFee,
            UtilityOneBed = office.UtilityOneBed,
            UtilityTwoBed = office.UtilityTwoBed,
            UtilityThreeBed = office.UtilityThreeBed,
            UtilityFourBed = office.UtilityFourBed,
            UtilityHouse = office.UtilityHouse,
            MaidOneBed = office.MaidOneBed,
            MaidTwoBed = office.MaidTwoBed,
            MaidThreeBed = office.MaidThreeBed,
            MaidFourBed = office.MaidFourBed,
            ParkingLowEnd = office.ParkingLowEnd,
            ParkingHighEnd = office.ParkingHighEnd,
            IsInternational = office.IsInternational,
            IsActive = office.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Office not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<IEnumerable<Office>> GetAllAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_GetAll", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Office>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Office>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Office>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Office?> GetByIdAsync(int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_GetById", new
        {
            OfficeId = officeId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Office?> GetByOfficeCodeAsync(string officeCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_GetByCode", new
        {
            OfficeCode = officeCode,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsByOfficeCodeAsync(string officeCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Organization.Office_ExistsByCode", new
        {
            OfficeCode = officeCode,
            OrganizationId = organizationId
        });

        return result == 1;
    }
    #endregion

    #region Update
    public async Task<Office> UpdateByIdAsync(Office office)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OfficeEntity>("Organization.Office_UpdateById", new
        {
            OfficeId = office.OfficeId,
            OrganizationId = office.OrganizationId,
            OfficeCode = office.OfficeCode,
            Name = office.Name,
            Address1 = office.Address1,
            Address2 = office.Address2,
            Suite = office.Suite,
            City = office.City,
            State = office.State,
            Zip = office.Zip,
            Phone = office.Phone,
            Fax = office.Fax,
            Website = office.Website,
            LogoPath = office.LogoPath,
            MaintenanceEmail = office.MaintenanceEmail,
            AfterHoursPhone = office.AfterHoursPhone,
            AfterHoursInstructions = office.AfterHoursInstructions,
            DaysToRefundDeposit = office.DaysToRefundDeposit,
            DefaultDeposit = office.DefaultDeposit,
            DefaultSdw = office.DefaultSdw,
            DefaultKeyFee = office.DefaultKeyFee,
            UndisclosedPetFee = office.UndisclosedPetFee,
            MinimumSmokingFee = office.MinimumSmokingFee,
            UtilityOneBed = office.UtilityOneBed,
            UtilityTwoBed = office.UtilityTwoBed,
            UtilityThreeBed = office.UtilityThreeBed,
            UtilityFourBed = office.UtilityFourBed,
            UtilityHouse = office.UtilityHouse,
            MaidOneBed = office.MaidOneBed,
            MaidTwoBed = office.MaidTwoBed,
            MaidThreeBed = office.MaidThreeBed,
            MaidFourBed = office.MaidFourBed,
            ParkingLowEnd = office.ParkingLowEnd,
            ParkingHighEnd = office.ParkingHighEnd,
            IsInternational = office.IsInternational,
            IsActive = office.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Office not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Delete
    public async Task DeleteByIdAsync(int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Office_DeleteById", new
        {
            OfficeId = officeId
        });
    }
    #endregion
}
