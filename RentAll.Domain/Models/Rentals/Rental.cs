namespace RentAll.Domain.Models.Rentals;

public class Rental
{
    public Guid RentalId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContactId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

