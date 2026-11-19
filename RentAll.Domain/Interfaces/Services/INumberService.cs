using RentAll.Domain.Models.Accounting;

namespace RentAll.Domain.Interfaces.Services;

public interface INumberService
{
    Task<NumberSession> CreateSessionAsync(string? acctCodeSecretName, string? tokenSecretName, CancellationToken cancellationToken = default);
    Task<string> CreatePayFormUrlAsync(NumberSession session, NumberPayForm payForm, CancellationToken cancellationToken = default);
    Task<NumberTransaction?> GetTransactionAsync(NumberSession session, long transactionId, CancellationToken cancellationToken = default);
    Task<NumberConsent?> GetConsentAsync(NumberSession session, long consentId, CancellationToken cancellationToken = default);
    Task<NumberStoredCardChargeResult> ChargeStoredCardAsync(NumberSession session, NumberStoredCardCharge charge, CancellationToken cancellationToken = default);
}
