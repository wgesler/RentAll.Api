using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Converts a signed amount into non-negative debit/credit values.
    /// Use positiveIsDebit=true for assets/expenses and false for liabilities/income.
    /// </summary>
    static (decimal Debit, decimal Credit) SignedAmountToDebitCredit(decimal signedAmount, bool positiveIsDebit)
    {
        if (signedAmount == 0)
            return (0, 0);

        var amount = Math.Abs(signedAmount);
        return positiveIsDebit
            ? signedAmount > 0 ? (amount, 0) : (0, amount)
            : signedAmount > 0 ? (0, amount) : (amount, 0);
    }

    static List<ReceiptSplit> ResolveDocumentSplitLines(Receipt document)
    {
        var splitLines = (document.Splits ?? new List<ReceiptSplit>())
            .Where(s => s.Amount != 0)
            .OrderBy(s => s.ReceiptSplitId)
            .ToList();

        if (splitLines.Count == 0 && document.Amount != 0)
        {
            splitLines.Add(new ReceiptSplit
            {
                Amount = document.Amount,
                Description = document.Description
            });
        }

        return splitLines;
    }
}
