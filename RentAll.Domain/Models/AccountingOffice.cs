namespace RentAll.Domain.Models;

public class AccountingOffice
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankRouting { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string BankSwiftCode { get; set; } = string.Empty;
    public string BankAddress { get; set; } = string.Empty;
    public string BankPhone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
