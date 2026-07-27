namespace RentAll.Api.Dtos.Accounting.Owners;

public class OwnerPaymentLineRequestDto
{
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Amount { get; set; }
}
