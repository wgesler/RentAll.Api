using RentAll.Domain.Models.Maintenances;

namespace RentAll.Domain.Interfaces.Services;

public interface IDocumentIntelligenceService
{
    bool IsEnabled { get; }

    Task<ReceiptDocumentExtraction> ExtractReceiptAsync(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);
}
