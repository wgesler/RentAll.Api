using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

/// <summary>
/// A contact's bank account (ACH). Entered on the contact screen, or by the tenant on the pay-request page, which also
/// keeps EasyPay's (Number) consent, the reservation and when the tenant authorized debits.
/// </summary>
public class ContactBank
{
    public int ContactBankId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int BankAccountTypeId { get; set; } = (int)BankAccountType.Unknown;
    public string? NameOnAccount { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    /// <summary>Base64 of the encrypted account number; empty when none is stored.</summary>
    public string AccountNumber { get; set; } = string.Empty;
    public string? LastFour { get; set; }
    /// <summary>Number's token for the account; debit it with ACH/ProcPayment.</summary>
    public int? NumberConsentId { get; set; }
    public Guid? ReservationId { get; set; }
    public DateTimeOffset? AuthorizedOn { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}
