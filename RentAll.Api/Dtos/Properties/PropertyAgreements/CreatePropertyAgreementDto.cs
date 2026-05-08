using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class CreatePropertyAgreementDto
{
    public int? ManagementFeeTypeId { get; set; }
    public decimal? FlatRateAmount { get; set; }
    public FileDetails? W9FileDetails { get; set; }
    public FileDetails? InsuranceFileDetails { get; set; }
    public DateOnly? InsuranceExpiration { get; set; }
    public string? AgreementPath { get; set; }
    public FileDetails? AgreementFileDetails { get; set; }
    public int? Markup { get; set; }
    public int? RevenueSplitOwner { get; set; }
    public int? RevenueSplitOffice { get; set; }
    public decimal? WorkingCapitalBalance { get; set; }
    public decimal? LinenAndTowelFee { get; set; }
    public decimal? HourlyLaborCost { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public int? RentalIncomeCcId { get; set; }
    public int? RentalExpenseCcId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePropertyAgreementLineDto> AgreementLines { get; set; } = new();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ManagementFeeTypeId.HasValue && !Enum.IsDefined(typeof(ManagementFeeType), ManagementFeeTypeId.Value))
            return (false, $"Invalid ManagementFeeType value: {ManagementFeeTypeId.Value}");

        if (FlatRateAmount.HasValue && FlatRateAmount.Value < 0)
            return (false, "FlatRateAmount cannot be negative");

        if (HourlyLaborCost.HasValue && HourlyLaborCost.Value < 0)
            return (false, "HourlyLaborCost cannot be negative");

        if (Markup.HasValue && Markup.Value < 0)
            return (false, "Markup cannot be negative");

        if (RevenueSplitOwner.HasValue && (RevenueSplitOwner.Value < 0 || RevenueSplitOwner.Value > 100))
            return (false, "RevenueSplitOwner must be between 0 and 100");

        if (RevenueSplitOffice.HasValue && (RevenueSplitOffice.Value < 0 || RevenueSplitOffice.Value > 100))
            return (false, "RevenueSplitOffice must be between 0 and 100");

        if (RevenueSplitOwner.HasValue && RevenueSplitOffice.HasValue
            && RevenueSplitOwner.Value + RevenueSplitOffice.Value != 100)
            return (false, "When both splits are provided, RevenueSplitOwner and RevenueSplitOffice must sum to 100");

        if (RentalIncomeCcId.HasValue && RentalIncomeCcId.Value <= 0)
            return (false, "RentalIncomeCcId must be greater than zero when provided");

        if (RentalExpenseCcId.HasValue && RentalExpenseCcId.Value <= 0)
            return (false, "RentalExpenseCcId must be greater than zero when provided");

        foreach (var line in AgreementLines ?? new List<CreatePropertyAgreementLineDto>())
        {
            var (isLineValid, lineError) = line.IsValid();
            if (!isLineValid)
                return (false, $"AgreementLine validation failed: {lineError}");
        }

        return (true, null);
    }

    public PropertyAgreement ToModel(Guid propertyId, int officeId)
    {
        return new PropertyAgreement
        {
            PropertyId = propertyId,
            OfficeId = officeId,
            ManagementFeeType = ManagementFeeTypeId.HasValue ? (ManagementFeeType)ManagementFeeTypeId.Value : ManagementFeeType.FlatRate,
            FlatRateAmount = FlatRateAmount ?? 0m,
            W9Path = null,
            InsurancePath = null,
            InsuranceExpiration = InsuranceExpiration,
            AgreementPath = AgreementPath,
            Markup = Markup ?? 25,
            RevenueSplitOwner = RevenueSplitOwner ?? 75,
            RevenueSplitOffice = RevenueSplitOffice ?? 25,
            WorkingCapitalBalance = WorkingCapitalBalance ?? 0m,
            LinenAndTowelFee = LinenAndTowelFee ?? 0m,
            HourlyLaborCost = HourlyLaborCost ?? 0m,
            BankName = BankName,
            RoutingNumber = RoutingNumber,
            AccountNumber = AccountNumber,
            RentalIncomeCcId = RentalIncomeCcId,
            RentalExpenseCcId = RentalExpenseCcId,
            Notes = Notes,
            AgreementLines = AgreementLines?.Select(line => line.ToModel(propertyId)).ToList() ?? new List<AgreementLine>()
        };
    }
}
