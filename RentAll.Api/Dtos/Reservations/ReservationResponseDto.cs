using RentAll.Domain.Models.Rentals;

namespace RentAll.Api.Dtos.Rentals;

public class ReservationResponseDto
{
    public Guid ReservationId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public string? PropertyAddress { get; set; }
    public int PropertyStatusId { get; set; }
    public Guid ContactId { get; set; }
    public int ClientTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? ArrivalDate { get; set; }
    public DateTimeOffset? DepartureDate { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int NumberOfPeople { get; set; }
    public decimal Deposit { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal Taxes { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public ReservationResponseDto(Reservation rental)
    {
        ReservationId = rental.ReservationId;
        AgentId = rental.AgentId;
        PropertyId = rental.PropertyId;
        PropertyCode = rental.PropertyCode;
        PropertyAddress = rental.PropertyAddress;
        PropertyStatusId = (int)rental.PropertyStatus;
        ContactId = rental.ContactId;
        ClientTypeId = (int)rental.ClientType;
        ReservationStatusId = (int)rental.ReservationStatus;
        IsActive = rental.IsActive;

        ArrivalDate = rental.ArrivalDate;
        DepartureDate = rental.DepartureDate;
        CheckInTimeId = (int)rental.CheckInTime;
        CheckOutTimeId = (int)rental.CheckOutTime;
        MonthlyRate = rental.MonthlyRate;
        DailyRate = rental.DailyRate;
        Bedrooms = rental.Bedrooms;
        Bathrooms = rental.Bathrooms;
        NumberOfPeople = rental.NumberOfPeople;
        Deposit = rental.Deposit;
        DepartureFee = rental.DepartureFee;
        Taxes = rental.Taxes;

        CreatedOn = rental.CreatedOn;
        CreatedBy = rental.CreatedBy;
        ModifiedOn = rental.ModifiedOn;
        ModifiedBy = rental.ModifiedBy;
    }
}


