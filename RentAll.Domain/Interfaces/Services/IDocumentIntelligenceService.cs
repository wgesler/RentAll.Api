using RentAll.Domain.Models.Contacts;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Domain.Interfaces.Services;

public interface IDocumentIntelligenceService
{
    bool IsEnabled { get; }

    Task<ReceiptDocumentExtraction> ExtractReceiptAsync(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<CreditCardStatementExtraction> ExtractCreditCardStatementAsync(
        byte[] content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken = default);

    Task<W9DocumentExtraction> ExtractW9Async(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);
}
