using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class OwnerAgreementInformationResponseDto
{
    public Guid OwnerAgreementInformationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? AgreementIntroduction { get; set; }
    public string? Recitals { get; set; }
    public string? SectionOneEmployment { get; set; }
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
    public string? SectionTwelveAdditionalDutiesAndRights { get; set; }
    public string? SectionThirteenTerminationAndRenewal { get; set; }
    public string? SectionFourteenSaleOfPropertyAccess { get; set; }
    public string? SectionFifteenSummaryOfFees { get; set; }
    public string? SectionSixteenForeignOwnership { get; set; }
    public string? SectionSeventeenIndemnity { get; set; }
    public string? SectionEighteenMiscellaneous { get; set; }
    public string? SectionNineteenAdditionalForms { get; set; }
    public string? InWitnessWhereof { get; set; }

    public OwnerAgreementInformationResponseDto(OwnerAgreementInformation ownerAgreementInformation)
    {
        OwnerAgreementInformationId = ownerAgreementInformation.OwnerAgreementInformationId;
        OfficeId = ownerAgreementInformation.OfficeId;
        PropertyId = ownerAgreementInformation.PropertyId;
        OrganizationId = ownerAgreementInformation.OrganizationId;
        AgreementIntroduction = ownerAgreementInformation.AgreementIntroduction;
        Recitals = ownerAgreementInformation.Recitals;
        SectionOneEmployment = ownerAgreementInformation.SectionOneEmployment;
        SectionTwoAgentDuties = ownerAgreementInformation.SectionTwoAgentDuties;
        SectionThreeOwnersDuties = ownerAgreementInformation.SectionThreeOwnersDuties;
        SectionFourAdvertisingAndPromotion = ownerAgreementInformation.SectionFourAdvertisingAndPromotion;
        SectionFiveMaintenanceRepairsAndOperations = ownerAgreementInformation.SectionFiveMaintenanceRepairsAndOperations;
        SectionSixReimbursements = ownerAgreementInformation.SectionSixReimbursements;
        SectionSevenGovernmentRegulations = ownerAgreementInformation.SectionSevenGovernmentRegulations;
        SectionEightInsurance = ownerAgreementInformation.SectionEightInsurance;
        SectionNineCollectionOfIncomeAndInstitutionOfLegalAction = ownerAgreementInformation.SectionNineCollectionOfIncomeAndInstitutionOfLegalAction;
        SectionTenBankAccounts = ownerAgreementInformation.SectionTenBankAccounts;
        SectionElevenRecordsAndReports = ownerAgreementInformation.SectionElevenRecordsAndReports;
        SectionTwelveAdditionalDutiesAndRights = ownerAgreementInformation.SectionTwelveAdditionalDutiesAndRights;
        SectionThirteenTerminationAndRenewal = ownerAgreementInformation.SectionThirteenTerminationAndRenewal;
        SectionFourteenSaleOfPropertyAccess = ownerAgreementInformation.SectionFourteenSaleOfPropertyAccess;
        SectionFifteenSummaryOfFees = ownerAgreementInformation.SectionFifteenSummaryOfFees;
        SectionSixteenForeignOwnership = ownerAgreementInformation.SectionSixteenForeignOwnership;
        SectionSeventeenIndemnity = ownerAgreementInformation.SectionSeventeenIndemnity;
        SectionEighteenMiscellaneous = ownerAgreementInformation.SectionEighteenMiscellaneous;
        SectionNineteenAdditionalForms = ownerAgreementInformation.SectionNineteenAdditionalForms;
        InWitnessWhereof = ownerAgreementInformation.InWitnessWhereof;
    }
}
