using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<OwnerAgreementInformation?> GetOwnerAgreementInformationByIdAsync(Guid ownerAgreementInformationId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerAgreementInformationEntity>("Lead.OwnerAgreementInformation_GetById", new
        {
            OwnerAgreementInformationId = ownerAgreementInformationId,
            OrganizationId = organizationId,
            OfficeId = (int?)null,
            PropertyId = (Guid?)null
        });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerAgreementInformationEntityToModel(res.First());
    }

    public async Task<OwnerAgreementInformation?> GetOwnerAgreementInformationByScopeAsync(Guid organizationId, int? officeId, Guid? propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerAgreementInformationEntity>("Lead.OwnerAgreementInformation_GetByScope", new
        {
            PropertyId = propertyId,
            OfficeId = officeId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerAgreementInformationEntityToModel(res.First());
    }

    public async Task<OwnerAgreementInformation?> GetOwnerAgreementInformationByExactScopeAsync(Guid organizationId, int? officeId, Guid? propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerAgreementInformationEntity>("Lead.OwnerAgreementInformation_GetById", new
        {
            OwnerAgreementInformationId = (Guid?)null,
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerAgreementInformationEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<OwnerAgreementInformation> CreateOwnerAgreementInformationAsync(OwnerAgreementInformation ownerAgreementInformation)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerAgreementInformationEntity>("Lead.OwnerAgreementInformation_Add", new
        {
            PropertyId = ownerAgreementInformation.PropertyId,
            OfficeId = ownerAgreementInformation.OfficeId,
            OrganizationId = ownerAgreementInformation.OrganizationId,
            AgreementIntroduction = ownerAgreementInformation.AgreementIntroduction,
            Recitals = ownerAgreementInformation.Recitals,
            SectionOneEmploymentOfAvenueWest = ownerAgreementInformation.SectionOneEmploymentOfAvenueWest,
            SectionTwoAgentDuties = ownerAgreementInformation.SectionTwoAgentDuties,
            SectionThreeOwnersDuties = ownerAgreementInformation.SectionThreeOwnersDuties,
            SectionFourAdvertisingAndPromotion = ownerAgreementInformation.SectionFourAdvertisingAndPromotion,
            SectionFiveMaintenanceRepairsAndOperations = ownerAgreementInformation.SectionFiveMaintenanceRepairsAndOperations,
            SectionSixReimbursements = ownerAgreementInformation.SectionSixReimbursements,
            SectionSevenGovernmentRegulations = ownerAgreementInformation.SectionSevenGovernmentRegulations,
            SectionEightInsurance = ownerAgreementInformation.SectionEightInsurance,
            SectionNineCollectionOfIncomeAndInstitutionOfLegalAction = ownerAgreementInformation.SectionNineCollectionOfIncomeAndInstitutionOfLegalAction,
            SectionTenBankAccounts = ownerAgreementInformation.SectionTenBankAccounts,
            SectionElevenRecordsAndReports = ownerAgreementInformation.SectionElevenRecordsAndReports,
            SectionTwelveAdditionalDutiesAndRightsOfAvenueWest = ownerAgreementInformation.SectionTwelveAdditionalDutiesAndRightsOfAvenueWest,
            SectionThirteenTerminationAndRenewal = ownerAgreementInformation.SectionThirteenTerminationAndRenewal,
            SectionFourteenSaleOfPropertyAccess = ownerAgreementInformation.SectionFourteenSaleOfPropertyAccess,
            SectionFifteenSummaryOfFees = ownerAgreementInformation.SectionFifteenSummaryOfFees,
            SectionSixteenForeignOwnership = ownerAgreementInformation.SectionSixteenForeignOwnership,
            SectionSeventeenIndemnity = ownerAgreementInformation.SectionSeventeenIndemnity,
            SectionEighteenMiscellaneous = ownerAgreementInformation.SectionEighteenMiscellaneous,
            SectionNineteenAdditionalForms = ownerAgreementInformation.SectionNineteenAdditionalForms,
            InWitnessWhereof = ownerAgreementInformation.InWitnessWhereof,
            CreatedBy = ownerAgreementInformation.CreatedBy
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner agreement information was not created.");

        return ConvertOwnerAgreementInformationEntityToModel(res.First());
    }

    #endregion

    #region Updates

    public async Task<OwnerAgreementInformation> UpdateOwnerAgreementInformationByIdAsync(OwnerAgreementInformation ownerAgreementInformation)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerAgreementInformationEntity>("Lead.OwnerAgreementInformation_UpdateById", new
        {
            PropertyId = ownerAgreementInformation.PropertyId,
            OfficeId = ownerAgreementInformation.OfficeId,
            OrganizationId = ownerAgreementInformation.OrganizationId,
            AgreementIntroduction = ownerAgreementInformation.AgreementIntroduction,
            Recitals = ownerAgreementInformation.Recitals,
            SectionOneEmploymentOfAvenueWest = ownerAgreementInformation.SectionOneEmploymentOfAvenueWest,
            SectionTwoAgentDuties = ownerAgreementInformation.SectionTwoAgentDuties,
            SectionThreeOwnersDuties = ownerAgreementInformation.SectionThreeOwnersDuties,
            SectionFourAdvertisingAndPromotion = ownerAgreementInformation.SectionFourAdvertisingAndPromotion,
            SectionFiveMaintenanceRepairsAndOperations = ownerAgreementInformation.SectionFiveMaintenanceRepairsAndOperations,
            SectionSixReimbursements = ownerAgreementInformation.SectionSixReimbursements,
            SectionSevenGovernmentRegulations = ownerAgreementInformation.SectionSevenGovernmentRegulations,
            SectionEightInsurance = ownerAgreementInformation.SectionEightInsurance,
            SectionNineCollectionOfIncomeAndInstitutionOfLegalAction = ownerAgreementInformation.SectionNineCollectionOfIncomeAndInstitutionOfLegalAction,
            SectionTenBankAccounts = ownerAgreementInformation.SectionTenBankAccounts,
            SectionElevenRecordsAndReports = ownerAgreementInformation.SectionElevenRecordsAndReports,
            SectionTwelveAdditionalDutiesAndRightsOfAvenueWest = ownerAgreementInformation.SectionTwelveAdditionalDutiesAndRightsOfAvenueWest,
            SectionThirteenTerminationAndRenewal = ownerAgreementInformation.SectionThirteenTerminationAndRenewal,
            SectionFourteenSaleOfPropertyAccess = ownerAgreementInformation.SectionFourteenSaleOfPropertyAccess,
            SectionFifteenSummaryOfFees = ownerAgreementInformation.SectionFifteenSummaryOfFees,
            SectionSixteenForeignOwnership = ownerAgreementInformation.SectionSixteenForeignOwnership,
            SectionSeventeenIndemnity = ownerAgreementInformation.SectionSeventeenIndemnity,
            SectionEighteenMiscellaneous = ownerAgreementInformation.SectionEighteenMiscellaneous,
            SectionNineteenAdditionalForms = ownerAgreementInformation.SectionNineteenAdditionalForms,
            InWitnessWhereof = ownerAgreementInformation.InWitnessWhereof,
            ModifiedBy = ownerAgreementInformation.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner agreement information was not found or not updated.");

        return ConvertOwnerAgreementInformationEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeleteOwnerAgreementInformationByIdAsync(Guid ownerAgreementInformationId, Guid organizationId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Lead.OwnerAgreementInformation_DeleteById", new
        {
            OwnerAgreementInformationId = ownerAgreementInformationId,
            OrganizationId = organizationId,
            ModifiedBy = modifiedBy
        });
    }

    #endregion
}
