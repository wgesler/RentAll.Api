using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Health Document Type Identification
    private static class HealthDocumentTypeIdentification
    {
        private static readonly Guid EmptyGuid = Guid.Empty;

        internal static void CollectFixTargets(IEnumerable<DocumentHealthIssue> issues, ISet<Guid> paymentIds, ISet<Guid> depositIds, ISet<Guid> transferIds)
        {
            foreach (var issue in issues ?? [])
                CollectFixTarget(issue, paymentIds, depositIds, transferIds);
        }

        internal static IReadOnlyList<Guid> CollectPaymentFixIds(IEnumerable<DocumentHealthIssue> issues)
        {
            var paymentIds = new HashSet<Guid>();
            CollectFixTargets(issues, paymentIds, new HashSet<Guid>(), new HashSet<Guid>());
            return paymentIds.OrderBy(id => id).ToList();
        }

        internal static IReadOnlyList<Guid> CollectFixDocumentIds(IEnumerable<DocumentHealthIssue> issues)
        {
            var ids = new HashSet<Guid>();
            foreach (var issue in issues ?? [])
            {
                if (issue.DocumentId != EmptyGuid)
                    ids.Add(issue.DocumentId);

                var normalizedIssue = (issue.Issue ?? string.Empty).Trim();
                if (issue.RelatedId is { } relatedId
                    && relatedId != EmptyGuid
                    && normalizedIssue.Contains("Duplicate invoice payment documents", StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(relatedId);
                }
            }

            return ids.OrderBy(id => id).ToList();
        }

        private static void CollectFixTarget(DocumentHealthIssue issue, ISet<Guid> paymentIds, ISet<Guid> depositIds, ISet<Guid> transferIds)
        {
            AddDocumentId(issue.DocumentId, issue.Issue, paymentIds, depositIds, transferIds);
            AddRelatedId(issue.RelatedId, issue.Issue, paymentIds, depositIds, transferIds);
        }

        private static void AddDocumentId(Guid documentId, string issueText, ISet<Guid> paymentIds, ISet<Guid> depositIds, ISet<Guid> transferIds)
        {
            if (documentId == EmptyGuid)
                return;

            RouteDocumentId(documentId, issueText, paymentIds, depositIds, transferIds);
        }

        private static void AddRelatedId(Guid? relatedId, string issueText, ISet<Guid> paymentIds, ISet<Guid> depositIds, ISet<Guid> transferIds)
        {
            if (relatedId is not { } id || id == EmptyGuid)
                return;

            var normalizedIssue = (issueText ?? string.Empty).Trim();
            if (normalizedIssue.Contains("Duplicate open invoice payment JE", StringComparison.OrdinalIgnoreCase)
                || normalizedIssue.Contains("Duplicate open Invoice Charge JE", StringComparison.OrdinalIgnoreCase)
                || normalizedIssue.Contains("Duplicate open Deposit JE", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (normalizedIssue.Contains("Duplicate invoice payment documents", StringComparison.OrdinalIgnoreCase))
            {
                paymentIds.Add(id);
                return;
            }

            if (normalizedIssue.Contains("Deposited payment", StringComparison.OrdinalIgnoreCase))
            {
                depositIds.Add(id);
                return;
            }

            if (normalizedIssue.Contains("Transfer deposit accounting period mismatch", StringComparison.OrdinalIgnoreCase))
            {
                depositIds.Add(id);
                return;
            }

            RouteDocumentId(id, issueText ?? string.Empty, paymentIds, depositIds, transferIds);
        }

        private static void RouteDocumentId(Guid documentId, string issueText, ISet<Guid> paymentIds, ISet<Guid> depositIds, ISet<Guid> transferIds)
        {
            var normalizedIssue = (issueText ?? string.Empty).Trim();

            if (IsPaymentIssue(normalizedIssue))
            {
                paymentIds.Add(documentId);
                return;
            }

            if (IsDepositIssue(normalizedIssue))
            {
                depositIds.Add(documentId);
                return;
            }

            if (IsTransferIssue(normalizedIssue))
            {
                transferIds.Add(documentId);
                return;
            }

            paymentIds.Add(documentId);
        }

        private static bool IsPaymentIssue(string issueText)
            => issueText.Contains("payment", StringComparison.OrdinalIgnoreCase)
                && !issueText.Contains("Deposit UF split", StringComparison.OrdinalIgnoreCase);

        private static bool IsDepositIssue(string issueText)
            => issueText.Contains("Deposit", StringComparison.OrdinalIgnoreCase)
                && !issueText.Contains("Deposited payment", StringComparison.OrdinalIgnoreCase);

        private static bool IsTransferIssue(string issueText)
            => issueText.Contains("Transfer split", StringComparison.OrdinalIgnoreCase)
                || issueText.Contains("Transfer deposit accounting period mismatch", StringComparison.OrdinalIgnoreCase);
    }
    #endregion
}
