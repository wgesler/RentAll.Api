using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Properties;

public partial class PropertyRepository
{
    public async Task<IEnumerable<PropertyReportData>> GetPropertyReportDataAsync(
        Guid organizationId,
        string officeIds,
        Guid? propertyId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyReportDataEntity>("Property.Property_GetReportData", new
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<PropertyReportData>();

        return res.Select(ConvertPropertyReportDataEntityToModel);
    }

    private static PropertyReportData ConvertPropertyReportDataEntityToModel(PropertyReportDataEntity entity)
    {
        return new PropertyReportData
        {
            PropertyId = entity.PropertyId,
            PropertyCode = entity.PropertyCode,
            OfficeId = entity.OfficeId,
            OfficeName = entity.OfficeName,
            PropertyType = (PropertyType)entity.PropertyTypeId,
            PropertyTypeDescription = entity.PropertyType,
            PropertyLeaseType = (PropertyLeaseType)entity.PropertyLeaseTypeId,
            PrimaryOwnerId = entity.PrimaryOwnerId,
            OwnerType = entity.OwnerTypeId.HasValue ? (OwnerType)entity.OwnerTypeId.Value : null,
            CompanyName = entity.CompanyName,
            OwnerNames = entity.OwnerNames,
            OwnerNameLine = entity.OwnerNameLine,
            WorkingCapitalBalance = entity.WorkingCapitalBalance,
            ManagementFeeType = (ManagementFeeType)entity.ManagementFeeTypeId,
            RevenueSplitOwner = entity.RevenueSplitOwner,
            RevenueSplitOffice = entity.RevenueSplitOffice
        };
    }
}
