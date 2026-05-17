using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class UpdateOwnerAgreementInformationDto
{
    public int? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? AgreementIntroduction { get; set; }
    public string? Recitals { get; set; }
    public string? SectionOneEmploymentOfAvenueWest { get; set; }
    public string? SectionTwoAgentDuties { get; set; }
    public string? SectionThreeOwnersDuties { get; set; }
    public string? SectionFourAdvertisingAndPromotion { get; set; }
    public string? SectionFiveMaintenanceRepairsAndOperations { get; set; }
    public string? SectionSixReimbursements { get; set; }
    public string? SectionSevenGovernmentRegulations { get; set; }
    public string? SectionEightInsurance { get; set; }
    public string? SectionNineCollectionOfIncomeAndInstitutionOfLegalAction { get; set; }
    public string? SectionTenBankAccounts { get; set; }
    public string? SectionElevenRecordsAndReports { get; set; }
    public string? SectionTwelveAdditionalDutiesAndRightsOfAvenueWest { get; set; }
    public string? SectionThirteenTerminationAndRenewal { get; set; }
    public string? SectionFourteenSaleOfPropertyAccess { get; set; }
    public string? SectionFifteenSummaryOfFees { get; set; }
    public string? SectionSixteenForeignOwnership { get; set; }
    public string? SectionSeventeenIndemnity { get; set; }
    public string? SectionEighteenMiscellaneous { get; set; }
    public string? SectionNineteenAdditionalForms { get; set; }
    public string? InWitnessWhereof { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (PropertyId.HasValue && PropertyId.Value == Guid.Empty)
            return (false, "PropertyId is invalid");

        if (!PropertyId.HasValue && OfficeId.HasValue && OfficeId.Value <= 0)
            return (false, "OfficeId is invalid");

        return (true, null);
    }

    public OwnerAgreementInformation ToModel(Guid currentUser)
    {
        return new OwnerAgreementInformation
        {
            PropertyId = PropertyId,
            OfficeId = OfficeId,
            OrganizationId = OrganizationId,
            AgreementIntroduction = AgreementIntroduction,
            Recitals = Recitals,
            SectionOneEmploymentOfAvenueWest = SectionOneEmploymentOfAvenueWest,
            SectionTwoAgentDuties = SectionTwoAgentDuties,
            SectionThreeOwnersDuties = SectionThreeOwnersDuties,
            SectionFourAdvertisingAndPromotion = SectionFourAdvertisingAndPromotion,
            SectionFiveMaintenanceRepairsAndOperations = SectionFiveMaintenanceRepairsAndOperations,
            SectionSixReimbursements = SectionSixReimbursements,
            SectionSevenGovernmentRegulations = SectionSevenGovernmentRegulations,
            SectionEightInsurance = SectionEightInsurance,
            SectionNineCollectionOfIncomeAndInstitutionOfLegalAction = SectionNineCollectionOfIncomeAndInstitutionOfLegalAction,
            SectionTenBankAccounts = SectionTenBankAccounts,
            SectionElevenRecordsAndReports = SectionElevenRecordsAndReports,
            SectionTwelveAdditionalDutiesAndRightsOfAvenueWest = SectionTwelveAdditionalDutiesAndRightsOfAvenueWest,
            SectionThirteenTerminationAndRenewal = SectionThirteenTerminationAndRenewal,
            SectionFourteenSaleOfPropertyAccess = SectionFourteenSaleOfPropertyAccess,
            SectionFifteenSummaryOfFees = SectionFifteenSummaryOfFees,
            SectionSixteenForeignOwnership = SectionSixteenForeignOwnership,
            SectionSeventeenIndemnity = SectionSeventeenIndemnity,
            SectionEighteenMiscellaneous = SectionEighteenMiscellaneous,
            SectionNineteenAdditionalForms = SectionNineteenAdditionalForms,
            InWitnessWhereof = InWitnessWhereof,
            ModifiedBy = currentUser
        };
    }
}
