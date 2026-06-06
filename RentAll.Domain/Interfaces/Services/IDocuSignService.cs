using RentAll.Domain.Models.ESignature;

namespace RentAll.Domain.Interfaces.Services;

public interface IDocuSignService
{
    Task<DocuSignEnvelopeResult> SendEnvelopeAsync(
        string? docuSignSecretName,
        byte[] pdfBytes,
        string fileName,
        string subject,
        IReadOnlyList<DocuSignSigner> signers,
        CancellationToken cancellationToken = default);
}
