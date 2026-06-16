using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class BankCard
{
    public int BankCardId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int CardTypeId { get; set; } = (int)CardType.Visa;
    public string CardName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
}
