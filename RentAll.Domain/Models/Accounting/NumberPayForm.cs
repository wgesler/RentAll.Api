namespace RentAll.Domain.Models.Accounting;

public class NumberPayForm
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
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
}
