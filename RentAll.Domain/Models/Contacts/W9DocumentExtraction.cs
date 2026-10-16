namespace RentAll.Domain.Models.Contacts;

public class W9DocumentExtraction
{
    public int? FederalTaxClassificationId { get; set; }
    public int? OwnerIdTypeId { get; set; }
    public string? IdentificationNumber { get; set; }
    public List<string> Warnings { get; set; } = [];
}
