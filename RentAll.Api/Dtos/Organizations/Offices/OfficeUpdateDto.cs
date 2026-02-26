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
            IsInternational = IsInternational,
            IsActive = IsActive
        };
    }
}

