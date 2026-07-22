namespace RentAll.Infrastructure.Entities.Organizations;

public class OfficeEntity
{
    public int OfficeId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OfficeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public string? MaintenanceEmail { get; set; }
    public string? AfterHoursPhone { get; set; }
    public string? AfterHoursInstructions { get; set; }
    public bool UseDailyOnResBoard { get; set; }
    public int DaysToRefundDeposit { get; set; }
    public decimal DefaultDeposit { get; set; }
    public decimal DefaultSdw { get; set; }
    public decimal DefaultKeyFee { get; set; }
    public decimal UndisclosedPetFee { get; set; }
    public decimal MinimumSmokingFee { get; set; }
    public decimal UtilityOneBed { get; set; }
    public decimal UtilityTwoBed { get; set; }
    public decimal UtilityThreeBed { get; set; }
    public decimal UtilityFourBed { get; set; }
    public decimal UtilityHouse { get; set; }
    public decimal MaidOneBed { get; set; }
    public decimal MaidTwoBed { get; set; }
    public decimal MaidThreeBed { get; set; }
    public decimal MaidFourBed { get; set; }
    public decimal MaidHouse { get; set; }
    public decimal ParkingLowEnd { get; set; }
    public decimal ParkingHighEnd { get; set; }

    public int? DefaultMarkup { get; set; }
    public int? DefaultRevenueSplitOwner { get; set; }
    public int? DefaultRevenueSplitOffice { get; set; }
    public decimal? DefaultWorkingCapitalBalance { get; set; }
    public decimal? DefaultHourlyLaborCost { get; set; }
    public decimal? DefaultLinenTowelOneBed { get; set; }
    public decimal? DefaultLinenTowelTwoBed { get; set; }
    public decimal? DefaultLinenTowelThreeBed { get; set; }
    public decimal? DefaultLinenTowelFourBed { get; set; }
    public decimal? DefaultOnlineFee { get; set; }
    public decimal? DefaultOnlineClean { get; set; }
    public decimal? DefaultOfflineFee { get; set; }

    public int? FurnishedRentChargeCcId { get; set; }
    public int? FurnishedRentExpenseCcId { get; set; }
    public int? UnfurnishedRentChargeCcId { get; set; }
    public int? UnfurnishedRentExpenseCcId { get; set; }
    public int? MaidServiceChargeCcId { get; set; }
    public int? MaidServiceExpenseCcId { get; set; }
    public int? ParkingChargeCcId { get; set; }
    public int? ParkingExpenseCcId { get; set; }
    public int? DepartureFeeCcId { get; set; }
    public int? PetFeeCcId { get; set; }
    public int? SecurityDepositCcId { get; set; }
    public int? SecurityDepositWaiverCcId { get; set; }

    public string? QuotePreface { get; set; }
    public string? QuoteSuffix { get; set; }
    public string? QuoteDisclaimer { get; set; }
    public bool QuotePropertyCode { get; set; }
    public bool QuotePetFee { get; set; }
    public bool QuoteDepartureFee { get; set; }
    public bool QuoteMaidFee { get; set; }

    public Guid? DocuSignUserId { get; set; }
    public Guid? DocuSignApiAccountId { get; set; }

    public int? QbNameTypeId { get; set; }
    public int? QbClassTypeId { get; set; }

    public string? EmailListForReservations { get; set; }
    public bool IsInternational { get; set; }
    public bool IsActive { get; set; }
}
