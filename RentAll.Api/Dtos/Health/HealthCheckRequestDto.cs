namespace RentAll.Api.Dtos.Health;

public class HealthCheckRequestDto
{
    public int[] OfficeIds { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds != null && OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        return (true, null);
    }
}

public class PaymentHealthCheckRequestDto : HealthCheckRequestDto
{
    public int? PaymentKindId { get; set; }

    public new (bool IsValid, string? ErrorMessage) IsValid()
    {
        var (baseValid, baseError) = base.IsValid();
        if (!baseValid)
            return (baseValid, baseError);

        if (PaymentKindId.HasValue && PaymentKindId.Value is not (0 or 1 or 2))
            return (false, "PaymentKindId must be 0 (Invoice), 1 (Bill), 2 (Owner), or omitted for all payments");

        return (true, null);
    }
}
