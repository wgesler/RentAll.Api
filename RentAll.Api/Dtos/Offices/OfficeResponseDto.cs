using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Offices;

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
        LogoPath = office.LogoPath;
        MaintenanceEmail = office.MaintenanceEmail;
        AfterHoursPhone = office.AfterHoursPhone;
        AfterHoursInstructions = office.AfterHoursInstructions;
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
        ParkingLowEnd = office.ParkingLowEnd;
        ParkingHighEnd = office.ParkingHighEnd;
        FileDetails = office.FileDetails;
        IsInternational = office.IsInternational;
        IsActive = office.IsActive;
    }
}

