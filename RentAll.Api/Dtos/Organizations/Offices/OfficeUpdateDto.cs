using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Offices;

public class OfficeUpdateDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
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
    public decimal ParkingLowEnd { get; set; }
    public decimal ParkingHighEnd { get; set; }
    public int? TenantChargeCcId { get; set; }
    public int? TenantExpenseCcId { get; set; }
    public int? OwnerChargeCcId { get; set; }
    public int? OwnerExpenseCcId { get; set; }
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
    public string? EmailListForReservations { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsInternational { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "Office ID is required");

        if (string.IsNullOrWhiteSpace(OfficeCode))
            return (false, "Office Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");


        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (TenantChargeCcId.HasValue && TenantChargeCcId.Value <= 0)
            return (false, "TenantChargeCcId must be greater than 0 when provided");

        if (TenantExpenseCcId.HasValue && TenantExpenseCcId.Value <= 0)
            return (false, "TenantExpenseCcId must be greater than 0 when provided");

        if (OwnerChargeCcId.HasValue && OwnerChargeCcId.Value <= 0)
            return (false, "OwnerChargeCcId must be greater than 0 when provided");

        if (OwnerExpenseCcId.HasValue && OwnerExpenseCcId.Value <= 0)
            return (false, "OwnerExpenseCcId must be greater than 0 when provided");

        if (FurnishedRentChargeCcId.HasValue && FurnishedRentChargeCcId.Value <= 0)
            return (false, "FurnishedRentChargeCcId must be greater than 0 when provided");

        if (FurnishedRentExpenseCcId.HasValue && FurnishedRentExpenseCcId.Value <= 0)
            return (false, "FurnishedRentExpenseCcId must be greater than 0 when provided");

        if (UnfurnishedRentChargeCcId.HasValue && UnfurnishedRentChargeCcId.Value <= 0)
            return (false, "UnfurnishedRentChargeCcId must be greater than 0 when provided");

        if (UnfurnishedRentExpenseCcId.HasValue && UnfurnishedRentExpenseCcId.Value <= 0)
            return (false, "UnfurnishedRentExpenseCcId must be greater than 0 when provided");

        if (MaidServiceChargeCcId.HasValue && MaidServiceChargeCcId.Value <= 0)
            return (false, "MaidServiceChargeCcId must be greater than 0 when provided");

        if (MaidServiceExpenseCcId.HasValue && MaidServiceExpenseCcId.Value <= 0)
            return (false, "MaidServiceExpenseCcId must be greater than 0 when provided");

        if (ParkingChargeCcId.HasValue && ParkingChargeCcId.Value <= 0)
            return (false, "ParkingChargeCcId must be greater than 0 when provided");

        if (ParkingExpenseCcId.HasValue && ParkingExpenseCcId.Value <= 0)
            return (false, "ParkingExpenseCcId must be greater than 0 when provided");

        if (DepartureFeeCcId.HasValue && DepartureFeeCcId.Value <= 0)
            return (false, "DepartureFeeCcId must be greater than 0 when provided");

        if (PetFeeCcId.HasValue && PetFeeCcId.Value <= 0)
            return (false, "PetFeeCcId must be greater than 0 when provided");

        if (SecurityDepositCcId.HasValue && SecurityDepositCcId.Value <= 0)
            return (false, "SecurityDepositCcId must be greater than 0 when provided");

        if (SecurityDepositWaiverCcId.HasValue && SecurityDepositWaiverCcId.Value <= 0)
            return (false, "SecurityDepositWaiverCcId must be greater than 0 when provided");

        return (true, null);
    }

    public Office ToModel()
    {
        return new Office
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            OfficeCode = OfficeCode,
            Name = Name,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Fax = Fax,
            Website = Website,
            LogoPath = LogoPath,
            MaintenanceEmail = MaintenanceEmail,
            AfterHoursPhone = AfterHoursPhone,
            AfterHoursInstructions = AfterHoursInstructions,
            UseDailyOnResBoard = UseDailyOnResBoard,
            DaysToRefundDeposit = DaysToRefundDeposit,
            DefaultDeposit = DefaultDeposit,
            DefaultSdw = DefaultSdw,
            DefaultKeyFee = DefaultKeyFee,
            UndisclosedPetFee = UndisclosedPetFee,
            MinimumSmokingFee = MinimumSmokingFee,
            UtilityOneBed = UtilityOneBed,
            UtilityTwoBed = UtilityTwoBed,
            UtilityThreeBed = UtilityThreeBed,
            UtilityFourBed = UtilityFourBed,
            UtilityHouse = UtilityHouse,
            MaidOneBed = MaidOneBed,
            MaidTwoBed = MaidTwoBed,
            MaidThreeBed = MaidThreeBed,
            MaidFourBed = MaidFourBed,
            ParkingLowEnd = ParkingLowEnd,
            ParkingHighEnd = ParkingHighEnd,
            TenantChargeCcId = TenantChargeCcId,
            TenantExpenseCcId = TenantExpenseCcId,
            OwnerChargeCcId = OwnerChargeCcId,
            OwnerExpenseCcId = OwnerExpenseCcId,
            FurnishedRentChargeCcId = FurnishedRentChargeCcId,
            FurnishedRentExpenseCcId = FurnishedRentExpenseCcId,
            UnfurnishedRentChargeCcId = UnfurnishedRentChargeCcId,
            UnfurnishedRentExpenseCcId = UnfurnishedRentExpenseCcId,
            MaidServiceChargeCcId = MaidServiceChargeCcId,
            MaidServiceExpenseCcId = MaidServiceExpenseCcId,
            ParkingChargeCcId = ParkingChargeCcId,
            ParkingExpenseCcId = ParkingExpenseCcId,
            DepartureFeeCcId = DepartureFeeCcId,
            PetFeeCcId = PetFeeCcId,
            SecurityDepositCcId = SecurityDepositCcId,
            SecurityDepositWaiverCcId = SecurityDepositWaiverCcId,
            EmailListForReservations = EmailListForReservations,
            IsInternational = IsInternational,
            IsActive = IsActive
        };
    }
}

