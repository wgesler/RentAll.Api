namespace RentAll.Domain.Models.Accounting;

public class NumberPayForm
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string? RedirectUrl { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public Guid ReferenceGuid { get; set; }
    public decimal Amount { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;

    /// <summary>PFS only: when set, Number creates a subscription (and a consent) for this card.</summary>
    public NumberSubscription? Subscription { get; set; }
}

/// <summary>A PFS subscription. Period is Number's value, e.g. WEEKLY or MONTHLY; FirstPayDate must be today or later.</summary>
public class NumberSubscription
{
    public decimal Amount { get; set; }
    public string Period { get; set; } = "MONTHLY";
    public DateOnly FirstPayDate { get; set; }
}
