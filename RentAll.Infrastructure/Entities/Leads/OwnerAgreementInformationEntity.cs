namespace RentAll.Infrastructure.Entities.Leads;

public class OwnerAgreementInformationEntity
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
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
