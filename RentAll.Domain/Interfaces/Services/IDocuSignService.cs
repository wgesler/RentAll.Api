using RentAll.Domain.Models.ESignature;

namespace RentAll.Domain.Interfaces.Services;

public interface IDocuSignService
{
    Task<DocuSignEnvelopeResult> SendEnvelopeAsync(
        string? companyName,
        byte[] pdfBytes,
        string fileName,
        string subject,
        IReadOnlyList<DocuSignSigner> signers,
        string returnUrl,
        string senderEmail,
        string senderName,
        CancellationToken cancellationToken = default);
}
