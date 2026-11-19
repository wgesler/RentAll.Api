using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ContactCard
{
    public int ContactCardId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int CardTypeId { get; set; } = (int)CardType.Visa;
    public string CardName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    /// <summary>MM/YY, as Number reports it.</summary>
    public string? ExpirationDate { get; set; }
    /// <summary>Number's token for the card; charge it with ConsentAnnual/ChargeStoredCard.</summary>
    public int? NumberConsentId { get; set; }
    /// <summary>The full number is held by the payment processor, not encrypted here.</summary>
    public bool IsExternallyStored { get; set; }
    public int? ChartOfAccountId { get; set; }
}
