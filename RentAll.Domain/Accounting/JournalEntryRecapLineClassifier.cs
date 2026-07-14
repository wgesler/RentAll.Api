using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Domain.Accounting;

public static class JournalEntryRecapLineClassifier
{
    public static bool TryClassify(JournalEntryRecapClassificationLine line, out JournalEntryRecapClassificationResult result)
    {
        result = default;
        var memo = AccountingManager.CoalesceJournalEntryMemo(line.JournalEntryMemo, line.LineMemo);
        var sourceTypeId = line.SourceTypeId ?? 0;
        var debit = line.Debit;
        var credit = line.Credit;

        if (sourceTypeId == (int)SourceType.Invoice
            && line.DefaultActRcvableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultActRcvableAccountId
            && debit > 0)
        {
            result = BuildResult("ExpectedIncome", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (AccountingManager.IsRentPlus4000JournalCreditLine(sourceTypeId, credit, line.IsRentalIncomeAccount))
        {
            result = BuildResult("RentPlus4000", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId == (int)SourceType.Invoice
            && memo.Contains(": Security Deposit Waiver", StringComparison.Ordinal)
            && credit > 0)
        {
            result = BuildResult("SDW", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId == (int)SourceType.Invoice
            && memo.Contains(": Security Deposit", StringComparison.Ordinal)
            && !memo.Contains(": Security Deposit Waiver", StringComparison.Ordinal)
            && credit > 0)
        {
            result = BuildResult("SecurityDeposit", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId == (int)SourceType.Invoice
            && (memo.Contains(": Departure Fee", StringComparison.Ordinal)
                || memo.Contains(": Pet Fee", StringComparison.Ordinal))
            && credit > 0)
        {
            result = BuildResult("Fee", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId == (int)SourceType.InvoicePayment
            && line.DefaultUndepFundsAccountId is > 0
            && line.ChartOfAccountId == line.DefaultUndepFundsAccountId
            && AccountingManager.MatchPaymentMemo(memo).IsMatch)
        {
            result = BuildResult("Payment", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (line.DefaultPrePayAccountId is > 0
            && line.ChartOfAccountId == line.DefaultPrePayAccountId
            && AccountingManager.MatchPrePaymentMemo(memo).IsMatch)
        {
            result = BuildResult("PrePayment", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId
            && AccountingManager.MatchOwnerPaymentMemo(memo).IsMatch)
        {
            result = BuildResult("OwnerPayment", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId
            && AccountingManager.MatchOwnerActualRentMemo(memo).IsMatch)
        {
            // Same Owner AP credit side as Expected (OwnerRent uses credit - debit).
            result = BuildResult("OwnerRentActual", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId
            && AccountingManager.MatchOwnerExpectedRentMemo(memo).IsMatch)
        {
            result = BuildResult("OwnerRent", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId
            && (AccountingManager.MatchOwnerBillMemo(memo).IsMatch
                || AccountingManager.MatchOwnerWorkOrderMemo(memo).IsMatch))
        {
            result = BuildResult("Expense", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId is (int)SourceType.Bill or (int)SourceType.Receipt
            && line.DefaultOwnerExpAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnerExpAccountId
            && AccountingManager.MatchOwnerBillMemo(memo).IsMatch
            && !memo.Contains(": Owner: Utility:", StringComparison.Ordinal))
        {
            result = BuildResult("Expense", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId is (int)SourceType.Bill or (int)SourceType.Receipt or (int)SourceType.WorkOrder or (int)SourceType.LinensAndTowels
            && line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId)
        {
            result = BuildResult("Expense", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (sourceTypeId == (int)SourceType.Invoice
            && line.DefaultTenantIncAccountId is > 0
            && line.ChartOfAccountId == line.DefaultTenantIncAccountId
            && credit > 0)
        {
            result = BuildResult("TenantIncome", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        return false;
    }

    public static IEnumerable<string> ExtractReachBackInvoiceCodes(IEnumerable<JournalEntryRecapClassificationLine> inDateRangeLines)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in inDateRangeLines.Where(line => line.IsInDateRange))
        {
            var memo = AccountingManager.CoalesceJournalEntryMemo(line.JournalEntryMemo, line.LineMemo);
            if (line.SourceTypeId == (int)SourceType.InvoicePayment
                && line.DefaultUndepFundsAccountId is > 0
                && line.ChartOfAccountId == line.DefaultUndepFundsAccountId
                && AccountingManager.MatchPaymentMemo(memo).IsMatch
                && AccountingManager.TryParseInvoiceSourceCodeFromMemo(memo) is { } paymentInvoiceCode)
            {
                codes.Add(paymentInvoiceCode);
                continue;
            }

            if (line.DefaultPrePayAccountId is > 0
                && line.ChartOfAccountId == line.DefaultPrePayAccountId
                && line.Credit - line.Debit < 0
                && AccountingManager.MatchPrePaymentMemo(memo).IsMatch
                && AccountingManager.TryParseInvoiceSourceCodeFromMemo(memo) is { } prepayInvoiceCode)
            {
                codes.Add(prepayInvoiceCode);
            }
        }

        return codes;
    }

    private static JournalEntryRecapClassificationResult BuildResult(
        string recapCategory,
        decimal amount,
        JournalEntryRecapClassificationLine line,
        string memo,
        int sourceTypeId)
    {
        return new JournalEntryRecapClassificationResult
        {
            RecapCategory = recapCategory,
            Amount = amount,
            Activity = ResolveActivity(sourceTypeId, memo)
        };
    }

    private static string ResolveActivity(int sourceTypeId, string memo)
    {
        if (sourceTypeId == (int)SourceType.Invoice && memo.Contains(": Owner:", StringComparison.Ordinal))
            return "Owner";

        return sourceTypeId switch
        {
            (int)SourceType.Invoice => "Invoice",
            (int)SourceType.InvoicePayment => "Payment",
            (int)SourceType.Bill => "Bill",
            (int)SourceType.Receipt => "Receipt",
            (int)SourceType.WorkOrder => "WorkOrder",
            (int)SourceType.LinensAndTowels => "LinensAndTowels",
            _ => "Other"
        };
    }
}

public sealed class JournalEntryRecapClassificationLine
{
    public int? SourceTypeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? LineMemo { get; set; }
    public string? JournalEntryMemo { get; set; }
    public int? DefaultActRcvableAccountId { get; set; }
    public int? DefaultUndepFundsAccountId { get; set; }
    public int? DefaultPrePayAccountId { get; set; }
    public int? DefaultOwnActPayableAccountId { get; set; }
    public int? DefaultOwnerExpAccountId { get; set; }
    public int? DefaultTenantIncAccountId { get; set; }
    public bool IsRentalIncomeAccount { get; set; }
    public bool IsInDateRange { get; set; } = true;
}

public readonly struct JournalEntryRecapClassificationResult
{
    public string RecapCategory { get; init; }
    public decimal Amount { get; init; }
    public string Activity { get; init; }
}
