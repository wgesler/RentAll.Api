namespace RentAll.Api.Dtos.Accounting.Owners;

public class OwnerPaymentRequestDto
{
    public DateOnly PaymentDate { get; set; }
    public int ChartOfAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentTypeId { get; set; }
    public List<OwnerPaymentLineRequestDto> Lines { get; set; } = new List<OwnerPaymentLineRequestDto>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (!Enum.IsDefined(typeof(PaymentType), PaymentTypeId))
            return (false, $"Invalid PaymentType value: {PaymentTypeId}");

        if (Lines.Count <= 0)
            return (false, "No owner statement lines submitted for payment");

        if (Lines.Any(line => line.OfficeId <= 0))
            return (false, "OfficeId is required for each owner payment line");

        if (Lines.Any(line => line.OwnerId == Guid.Empty))
            return (false, "OwnerId is required for each owner payment line");

        if (Lines.Any(line => line.PropertyId == Guid.Empty))
            return (false, "PropertyId is required for each owner payment line");

        if (Lines.Any(line => line.Amount == 0))
            return (false, "Amount is required for each owner payment line");

        return (true, null);
    }
}
