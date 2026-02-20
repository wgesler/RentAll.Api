using RentAll.Api.Dtos.Accounting.ExtraFeeLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationResponseDto
{
    public Guid ReservationId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public Guid? AgentId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public int ReservationTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public int ReservationNoticeId { get; set; }
    public int NumberOfPeople { get; set; }
    public string? TenantName { get; set; }
    public DateTimeOffset ArrivalDate { get; set; }
    public DateTimeOffset DepartureDate { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }
    public int BillingMethodId { get; set; }
    public int ProrateTypeId { get; set; }
    public int BillingTypeId { get; set; }
    public decimal BillingRate { get; set; }
    public decimal Deposit { get; set; }
    public int DepositTypeId { get; set; }
    public decimal DepartureFee { get; set; }
    public bool HasPets { get; set; }
    public decimal PetFee { get; set; }
    public int NumberOfPets { get; set; }
    public string? PetDescription { get; set; }
    public bool MaidService { get; set; }
    public decimal MaidServiceFee { get; set; }
    public int FrequencyId { get; set; }
    public DateTimeOffset MaidStartDate { get; set; }
    public decimal Taxes { get; set; }
    public string? Notes { get; set; }
    public List<ExtraFeeLineResponseDto> ExtraFeeLines { get; set; } = new List<ExtraFeeLineResponseDto>();
    public bool AllowExtensions { get; set; }
    public int CurrentInvoiceNumber { get; set; }
    public decimal CreditDue { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }


    public ReservationResponseDto(Reservation reservation)
    {
        ReservationId = reservation.ReservationId;
        OrganizationId = reservation.OrganizationId;
        OfficeId = reservation.OfficeId;
        OfficeName = reservation.OfficeName;
        ReservationCode = reservation.ReservationCode;
        AgentId = reservation.AgentId;
        PropertyId = reservation.PropertyId;
        ContactId = reservation.ContactId;
        ContactName = reservation.ContactName;
        ReservationTypeId = (int)reservation.ReservationType;
        ReservationStatusId = (int)reservation.ReservationStatus;
        ReservationNoticeId = (int)reservation.ReservationNotice;
        NumberOfPeople = reservation.NumberOfPeople;
        TenantName = reservation.TenantName;
        ArrivalDate = reservation.ArrivalDate;
        DepartureDate = reservation.DepartureDate;
        CheckInTimeId = (int)reservation.CheckInTime;
        CheckOutTimeId = (int)reservation.CheckOutTime;
        BillingMethodId = (int)reservation.BillingMethod;
        ProrateTypeId = (int)reservation.ProrateType;
        BillingTypeId = (int)reservation.BillingType;
        BillingRate = reservation.BillingRate;
        Deposit = reservation.Deposit;
        DepositTypeId = (int)reservation.DepositType;
        DepartureFee = reservation.DepartureFee;
        HasPets = reservation.HasPets;
        PetFee = reservation.PetFee;
        NumberOfPets = reservation.NumberOfPets;
        PetDescription = reservation.PetDescription;
        MaidService = reservation.MaidService;
        MaidServiceFee = reservation.MaidServiceFee;
        FrequencyId = (int)reservation.Frequency;
        MaidStartDate = reservation.MaidStartDate;
        Taxes = reservation.Taxes;
        Notes = reservation.Notes;
        ExtraFeeLines = reservation.ExtraFeeLines.Select(line => new ExtraFeeLineResponseDto(line)).ToList();
        AllowExtensions = reservation.AllowExtensions;
        CurrentInvoiceNumber = reservation.CurrentInvoiceNumber;
        CreditDue = reservation.CreditDue;
        IsActive = reservation.IsActive;
        CreatedOn = reservation.CreatedOn;
        CreatedBy = reservation.CreatedBy;
        ModifiedOn = reservation.ModifiedOn;
        ModifiedBy = reservation.ModifiedBy;
    }
}


