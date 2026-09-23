using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Duplicate Payment Documents
    private async Task ReconcileDuplicateInvoicePaymentDocumentsForIssuesAsync(IEnumerable<DocumentHealthIssue> issues, Guid organizationId, Guid currentUser, JournalEntrySyncResult result)
    {
        var processedPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var issue in issues ?? [])
        {
            if (!(issue.Issue ?? string.Empty).Contains("Duplicate invoice payment documents", StringComparison.OrdinalIgnoreCase))
                continue;

            if (issue.DocumentId == Guid.Empty || issue.RelatedId is not { } relatedPaymentId || relatedPaymentId == Guid.Empty)
                continue;

            var pairKey = string.CompareOrdinal(issue.DocumentId.ToString(), relatedPaymentId.ToString()) < 0
                ? $"{issue.DocumentId}:{relatedPaymentId}"
                : $"{relatedPaymentId}:{issue.DocumentId}";

            if (!processedPairs.Add(pairKey))
                continue;

            await ReconcileDuplicateInvoicePaymentDocumentPairAsync(
                issue.DocumentId,
                relatedPaymentId,
                organizationId,
                currentUser,
                result);
        }
    }

    private async Task ReconcileDuplicateInvoicePaymentDocumentPairAsync(Guid firstPaymentId, Guid secondPaymentId, Guid organizationId, Guid currentUser, JournalEntrySyncResult result)
    {
        var first = await _accountingRepository.GetPaymentByIdAsync(firstPaymentId, organizationId);
        var second = await _accountingRepository.GetPaymentByIdAsync(secondPaymentId, organizationId);
        if (first == null || second == null || !first.IsActive || !second.IsActive)
            return;

        if (first.PaymentKindId != (int)PaymentKind.Invoice || second.PaymentKindId != (int)PaymentKind.Invoice)
            return;

        var keeper = await SelectDuplicateInvoicePaymentDocumentKeeperAsync(first, second, organizationId);
        var loser = keeper.PaymentId == first.PaymentId ? second : first;

        if (keeper.DepositId is { } keeperDepositId && keeperDepositId != Guid.Empty
            && loser.DepositId is { } loserDepositId && loserDepositId != Guid.Empty
            && keeperDepositId != loserDepositId
            && loser.DepositId != keeper.DepositId)
        {
            result.Errors.Add(
                $"Duplicate payments {first.PaymentCode} and {second.PaymentCode} are stamped to different deposits; kept both.");
            return;
        }

        try
        {
            result.JournalEntriesDeleted += await PruneDuplicateOpenInvoicePaymentJournalEntriesByPaymentIdAsync(
                loser.PaymentId,
                organizationId,
                currentUser);

            await DeletePaymentAsync(loser.PaymentId, organizationId, currentUser);
            result.JournalEntriesDeleted++;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Duplicate payment cleanup {loser.PaymentCode}: {ex.Message}");
        }
    }

    private async Task<Payment> SelectDuplicateInvoicePaymentDocumentKeeperAsync(Payment first, Payment second, Guid organizationId)
    {
        var firstScore = await ScoreDuplicateInvoicePaymentDocumentRetentionAsync(first, organizationId);
        var secondScore = await ScoreDuplicateInvoicePaymentDocumentRetentionAsync(second, organizationId);

        if (secondScore > firstScore)
            return second;

        if (firstScore > secondScore)
            return first;

        return string.Compare(first.PaymentCode, second.PaymentCode, StringComparison.OrdinalIgnoreCase) <= 0
            ? first
            : second;
    }

    private async Task<int> ScoreDuplicateInvoicePaymentDocumentRetentionAsync(Payment payment, Guid organizationId)
    {
        var score = 0;

        if (payment.DepositId is { } depositId && depositId != Guid.Empty)
            score += 100;

        if (await PaymentHasHealthPaymentJournalEntryAsync(payment.PaymentId, organizationId))
            score += 20;

        return score;
    }
    #endregion
}
