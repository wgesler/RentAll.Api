using RentAll.Domain.Models.ESignature;

namespace RentAll.Domain.Interfaces.Services;

public interface IDocuSignService
{
    Task<DocuSignEnvelopeResult> SendEnvelopeAsync(
        byte[] pdfBytes,
        string fileName,
        string subject,
        IReadOnlyList<DocuSignSigner> signers,
        string returnUrl,
        string senderEmail,
        string senderName,
        Guid? userId = null,
        Guid? apiAccountId = null,
        string? baseUri = null,
        CancellationToken cancellationToken = default);
}
