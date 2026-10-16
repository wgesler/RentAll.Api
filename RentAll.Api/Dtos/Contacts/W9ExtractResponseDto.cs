using RentAll.Domain.Models.Contacts;

namespace RentAll.Api.Dtos.Contacts;

public class W9ExtractResponseDto
{
    public int? FederalTaxClassificationId { get; set; }
    public int? OwnerIdTypeId { get; set; }
    public string? IdentificationNumber { get; set; }
    public List<string> Warnings { get; set; } = [];

    public static W9ExtractResponseDto FromExtraction(W9DocumentExtraction extraction)
    {
        return new W9ExtractResponseDto
        {
            FederalTaxClassificationId = extraction.FederalTaxClassificationId,
            OwnerIdTypeId = extraction.OwnerIdTypeId,
            IdentificationNumber = extraction.IdentificationNumber,
            Warnings = extraction.Warnings
        };
    }
}
