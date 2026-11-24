using RentAll.Domain.Models.Accounting;

namespace RentAll.Domain.Interfaces.Services;

public interface INumberService
{
    Task<NumberSession> CreateSessionAsync(string? acctCodeSecretName, string? tokenSecretName, CancellationToken cancellationToken = default);
    /// <summary>Standard PayForm (PF), sent at $0 so the payer only saves a card.</summary>
    Task<string> CreatePayFormUrlAsync(NumberSession session, NumberPayForm payForm, CancellationToken cancellationToken = default);
    /// <summary>Custom PayForm (PFS) on the REST API; charges the amount and saves the card.</summary>
    Task<string> CreatePfsFormUrlAsync(NumberSession session, NumberPayForm payForm, CancellationToken cancellationToken = default);
    Task<NumberTransaction?> GetTransactionAsync(NumberSession session, long transactionId, CancellationToken cancellationToken = default);
    Task<NumberConsent?> GetConsentAsync(NumberSession session, long consentId, CancellationToken cancellationToken = default);
    Task<NumberStoredCardChargeResult> ChargeStoredCardAsync(NumberSession session, NumberStoredCardCharge charge, CancellationToken cancellationToken = default);
    Task<NumberStoredCardChargeResult> ChargeBankAccountAsync(NumberSession session, long consentId, decimal amount, CancellationToken cancellationToken = default);
    /// <summary>Puts a PFS subscription on hold so it never bills; the saved card stays usable.</summary>
    Task<bool> HoldSubscriptionAsync(NumberSession session, long consentId, CancellationToken cancellationToken = default);
    /// <summary>Saves a bank account as an ACH consent with no debit (ACH/Combo, Amount 0).</summary>
    Task<NumberAchConsentResult> CreateAchConsentAsync(NumberSession session, NumberAchConsentRequest ach, CancellationToken cancellationToken = default);
}
