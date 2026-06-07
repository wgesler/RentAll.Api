using RentAll.Domain.Models.ESignature;

namespace RentAll.Api.Dtos.ESignature;

public class SendDocumentForSignatureResponseDto
{
    public string EnvelopeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SenderViewUrl { get; set; } = string.Empty;

    public SendDocumentForSignatureResponseDto()
    {
    }

    public SendDocumentForSignatureResponseDto(DocuSignEnvelopeResult result)
    {
        EnvelopeId = result.EnvelopeId;
        Status = result.Status;
        SenderViewUrl = result.SenderViewUrl;
    }
}
