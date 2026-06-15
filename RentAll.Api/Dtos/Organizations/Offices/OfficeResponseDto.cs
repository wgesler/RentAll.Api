using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Offices;

public class OfficeResponseDto
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
    public int YearEndMonth { get; set; }
    public int YearEndDay { get; set; }
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
    public FileDetails? FileDetails { get; set; }
    public bool IsInternational { get; set; }
    public bool IsActive { get; set; }

    public OfficeResponseDto(Office office)
    {
        OrganizationId = office.OrganizationId;
        OfficeId = office.OfficeId;
        OfficeCode = office.OfficeCode;
        Name = office.Name;
        Address1 = office.Address1;
        Address2 = office.Address2;
        Suite = office.Suite;
        City = office.City;
        State = office.State;
        Zip = office.Zip;
        Phone = office.Phone;
        Fax = office.Fax;
        Website = office.Website;
        YearEndMonth = office.YearEndMonth;
        YearEndDay = office.YearEndDay;
        LogoPath = office.LogoPath;
        MaintenanceEmail = office.MaintenanceEmail;
        AfterHoursPhone = office.AfterHoursPhone;
        AfterHoursInstructions = office.AfterHoursInstructions;
        UseDailyOnResBoard = office.UseDailyOnResBoard;
        DaysToRefundDeposit = office.DaysToRefundDeposit;
        DefaultDeposit = office.DefaultDeposit;
        DefaultSdw = office.DefaultSdw;
        DefaultKeyFee = office.DefaultKeyFee;
        UndisclosedPetFee = office.UndisclosedPetFee;
        MinimumSmokingFee = office.MinimumSmokingFee;
        UtilityOneBed = office.UtilityOneBed;
        UtilityTwoBed = office.UtilityTwoBed;
        UtilityThreeBed = office.UtilityThreeBed;
        UtilityFourBed = office.UtilityFourBed;
        UtilityHouse = office.UtilityHouse;
        MaidOneBed = office.MaidOneBed;
        MaidTwoBed = office.MaidTwoBed;
        MaidThreeBed = office.MaidThreeBed;
        MaidFourBed = office.MaidFourBed;
        MaidHouse = office.MaidHouse;
        ParkingLowEnd = office.ParkingLowEnd;
        ParkingHighEnd = office.ParkingHighEnd;
        DefaultMarkup = office.DefaultMarkup;
        DefaultRevenueSplitOwner = office.DefaultRevenueSplitOwner;
        DefaultRevenueSplitOffice = office.DefaultRevenueSplitOffice;
        DefaultWorkingCapitalBalance = office.DefaultWorkingCapitalBalance;
        DefaultHourlyLaborCost = office.DefaultHourlyLaborCost;
        DefaultLinenTowelOneBed = office.DefaultLinenTowelOneBed;
        DefaultLinenTowelTwoBed = office.DefaultLinenTowelTwoBed;
        DefaultLinenTowelThreeBed = office.DefaultLinenTowelThreeBed;
        DefaultLinenTowelFourBed = office.DefaultLinenTowelFourBed;
        DefaultOnlineFee = office.DefaultOnlineFee;
        DefaultOnlineClean = office.DefaultOnlineClean;
        DefaultOfflineFee = office.DefaultOfflineFee;
        FurnishedRentChargeCcId = office.FurnishedRentChargeCcId;
        FurnishedRentExpenseCcId = office.FurnishedRentExpenseCcId;
        UnfurnishedRentChargeCcId = office.UnfurnishedRentChargeCcId;
        UnfurnishedRentExpenseCcId = office.UnfurnishedRentExpenseCcId;
        MaidServiceChargeCcId = office.MaidServiceChargeCcId;
        MaidServiceExpenseCcId = office.MaidServiceExpenseCcId;
        ParkingChargeCcId = office.ParkingChargeCcId;
        ParkingExpenseCcId = office.ParkingExpenseCcId;
        DepartureFeeCcId = office.DepartureFeeCcId;
        PetFeeCcId = office.PetFeeCcId;
        SecurityDepositCcId = office.SecurityDepositCcId;
        SecurityDepositWaiverCcId = office.SecurityDepositWaiverCcId;
        QuotePreface = office.QuotePreface;
        QuoteSuffix = office.QuoteSuffix;
        QuoteDisclaimer = office.QuoteDisclaimer;
        QuotePropertyCode = office.QuotePropertyCode;
        QuotePetFee = office.QuotePetFee;
        QuoteDepartureFee = office.QuoteDepartureFee;
        QuoteMaidFee = office.QuoteMaidFee;
        DocuSignUserId = office.DocuSignUserId;
        DocuSignApiAccountId = office.DocuSignApiAccountId;
        QbNameTypeId = office.QbNameType.HasValue ? (int?)office.QbNameType.Value : null;
        QbClassTypeId = office.QbClassType.HasValue ? (int?)office.QbClassType.Value : null;
        EmailListForReservations = office.EmailListForReservations;
        FileDetails = office.FileDetails;
        IsInternational = office.IsInternational;
        IsActive = office.IsActive;
    }
}

