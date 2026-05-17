using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<OwnerInventoryInformation?> GetOwnerInventoryInformationByOwnerIdAsync(int ownerId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerInventoryInformationEntity>("Lead.OwnerInventoryInformation_GetByOwnerId", new
        {
            OwnerId = ownerId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerInventoryInformationEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<OwnerInventoryInformation> CreateOwnerInventoryInformationAsync(OwnerInventoryInformation ownerInventoryInformation)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerInventoryInformationEntity>("Lead.OwnerInventoryInformation_Add", new
        {
            OwnerId = ownerInventoryInformation.OwnerId,
            OrganizationId = ownerInventoryInformation.OrganizationId,
            OnSiteComplexManagementPhone = ownerInventoryInformation.OnSiteComplexManagementPhone,
            KeyCount = ownerInventoryInformation.KeyCount,
            GarageRemoteModelCode = ownerInventoryInformation.GarageRemoteModelCode,
            StorageAccessDetails = ownerInventoryInformation.StorageAccessDetails,
            CableSupplier = ownerInventoryInformation.CableSupplier,
            CablePhone = ownerInventoryInformation.CablePhone,
            CableAccountNumber = ownerInventoryInformation.CableAccountNumber,
            ElectricSupplier = ownerInventoryInformation.ElectricSupplier,
            ElectricPhone = ownerInventoryInformation.ElectricPhone,
            ElectricAccountNumber = ownerInventoryInformation.ElectricAccountNumber,
            InternetSupplier = ownerInventoryInformation.InternetSupplier,
            InternetPhone = ownerInventoryInformation.InternetPhone,
            InternetAccountNumber = ownerInventoryInformation.InternetAccountNumber,
            FuseBoxLocation = ownerInventoryInformation.FuseBoxLocation,
            SchoolDistrict = ownerInventoryInformation.SchoolDistrict,
            LocalEmergencyContact = ownerInventoryInformation.LocalEmergencyContact,
            AccessInformation = ownerInventoryInformation.AccessInformation,
            IsActive = ownerInventoryInformation.IsActive,
            CreatedBy = ownerInventoryInformation.CreatedBy
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner inventory information was not created.");

        return ConvertOwnerInventoryInformationEntityToModel(res.First());
    }

    #endregion

    #region Updates

    public async Task<OwnerInventoryInformation> UpdateOwnerInventoryInformationByIdAsync(OwnerInventoryInformation ownerInventoryInformation)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerInventoryInformationEntity>("Lead.OwnerInventoryInformation_UpdateById", new
        {
            OwnerId = ownerInventoryInformation.OwnerId,
            OrganizationId = ownerInventoryInformation.OrganizationId,
            OnSiteComplexManagementPhone = ownerInventoryInformation.OnSiteComplexManagementPhone,
            KeyCount = ownerInventoryInformation.KeyCount,
            GarageRemoteModelCode = ownerInventoryInformation.GarageRemoteModelCode,
            StorageAccessDetails = ownerInventoryInformation.StorageAccessDetails,
            CableSupplier = ownerInventoryInformation.CableSupplier,
            CablePhone = ownerInventoryInformation.CablePhone,
            CableAccountNumber = ownerInventoryInformation.CableAccountNumber,
            ElectricSupplier = ownerInventoryInformation.ElectricSupplier,
            ElectricPhone = ownerInventoryInformation.ElectricPhone,
            ElectricAccountNumber = ownerInventoryInformation.ElectricAccountNumber,
            InternetSupplier = ownerInventoryInformation.InternetSupplier,
            InternetPhone = ownerInventoryInformation.InternetPhone,
            InternetAccountNumber = ownerInventoryInformation.InternetAccountNumber,
            FuseBoxLocation = ownerInventoryInformation.FuseBoxLocation,
            SchoolDistrict = ownerInventoryInformation.SchoolDistrict,
            LocalEmergencyContact = ownerInventoryInformation.LocalEmergencyContact,
            AccessInformation = ownerInventoryInformation.AccessInformation,
            IsActive = ownerInventoryInformation.IsActive,
            ModifiedBy = ownerInventoryInformation.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner inventory information was not found or not updated.");

        return ConvertOwnerInventoryInformationEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeleteOwnerInventoryInformationByIdAsync(int ownerId, Guid organizationId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Lead.OwnerInventoryInformation_DeleteById", new
        {
            OwnerId = ownerId,
            OrganizationId = organizationId,
            ModifiedBy = modifiedBy
        });
    }

    #endregion
}
