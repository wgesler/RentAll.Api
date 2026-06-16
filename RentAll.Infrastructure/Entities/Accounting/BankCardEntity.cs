namespace RentAll.Infrastructure.Entities.Accounting;

public class BankCardEntity
{
    public int BankCardId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int CardTypeId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public byte[] CardNumber { get; set; } = [];
    public string LastFour { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
}
