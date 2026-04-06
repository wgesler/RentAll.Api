namespace RentAll.Domain.Models;

public class PropertyAgreement
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string? W9Path { get; set; }
    public string? InsurancePath { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public string? AgreementPath { get; set; }
    public int Markup { get; set; }
    public int RevenueSplitOwner { get; set; }
    public int RevenueSplitOffice { get; set; }
    public decimal WorkingCapitalBalance { get; set; }
    public decimal LinenAndTowelFee { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Notes { get; set; }
}
