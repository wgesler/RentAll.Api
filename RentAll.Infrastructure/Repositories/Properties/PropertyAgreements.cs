using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Properties;

public partial class PropertyRepository
{
    #region Selects
    public async Task<PropertyAgreement?> GetPropertyAgreementByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }

    #endregion

    #region Creates
    public async Task<PropertyAgreement> CreatePropertyAgreementAsync(PropertyAgreement agreement)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_Add", new
        {
            agreement.PropertyId,
            agreement.OfficeId,
            ManagmentFeeTypeId = (int)agreement.ManagementFeeType,
            agreement.W9Path,
            agreement.InsurancePath,
            agreement.InsuranceExpiration,
            agreement.AgreementPath,
            Markup = agreement.Markup,
            RevenueSplitOwner = agreement.RevenueSplitOwner,
            RevenueSplitOffice = agreement.RevenueSplitOffice,
            WorkingCapitalBalance = agreement.WorkingCapitalBalance,
            LinenAndTowelFee = agreement.LinenAndTowelFee,
            agreement.BankName,
            agreement.RoutingNumber,
            agreement.AccountNumber,
            agreement.Notes
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Property agreement not created");

        return ConvertEntityToModel(res.First());
    }

    #endregion

    #region Updates
    public async Task<PropertyAgreement> UpdatePropertyAgreementByPropertyIdAsync(PropertyAgreement agreement)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_UpdateByPropertyId", new
        {
            agreement.PropertyId,
            agreement.OfficeId,
            ManagmentFeeTypeId = (int)agreement.ManagementFeeType,
            agreement.W9Path,
            agreement.InsurancePath,
            agreement.InsuranceExpiration,
            agreement.AgreementPath,
            Markup = agreement.Markup,
            RevenueSplitOwner = agreement.RevenueSplitOwner,
            RevenueSplitOffice = agreement.RevenueSplitOffice,
            WorkingCapitalBalance = agreement.WorkingCapitalBalance,
            LinenAndTowelFee = agreement.LinenAndTowelFee,
            agreement.BankName,
            agreement.RoutingNumber,
            agreement.AccountNumber,
            agreement.Notes
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Property agreement not found");

        return ConvertEntityToModel(res.First());
    }

    #endregion

    #region Deletes
    public async Task DeletePropertyAgreementByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.PropertyAgreements_DeleteByPropertyId", new
        {
            PropertyId = propertyId
        });
    }

    #endregion
}
