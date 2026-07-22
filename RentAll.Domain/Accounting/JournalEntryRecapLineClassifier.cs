using RentAll.Domain.Enums;
using RentAll.Domain.Managers;

namespace RentAll.Domain.Accounting;

public static class JournalEntryRecapLineClassifier
{
    public static bool TryClassify(JournalEntryRecapClassificationLine line, out JournalEntryRecapClassificationResult result)
    {
        result = default;
        var memo = AccountingManager.CoalesceJournalEntryMemo(line.JournalEntryMemo, line.LineMemo);
        var sourceTypeId = line.SourceTypeId ?? 0;
        var kind = line.JournalEntryKindId ?? 0;
        var debit = line.Debit;
        var credit = line.Credit;

        // Kind-first for JE types that are one Kind = one recap role (account still selects the money line).
        if (kind == (int)JournalEntryKind.Payment
            && line.DefaultUndepFundsAccountId is > 0
            && line.ChartOfAccountId == line.DefaultUndepFundsAccountId)
        {
            result = BuildResult("Payment", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if (kind is (int)JournalEntryKind.PrePaymentReceive or (int)JournalEntryKind.PrePaymentApply
            && line.DefaultPrePayAccountId is > 0
            && line.ChartOfAccountId == line.DefaultPrePayAccountId)
        {
            result = BuildResult("PrePayment", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (kind == (int)JournalEntryKind.OwnerActual
            && line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId)
        {
            result = BuildResult("OwnerRentActual", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if (kind == (int)JournalEntryKind.OwnerExpected
            && line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId)
        {
            result = BuildResult("OwnerRent", credit - debit, line, memo, sourceTypeId);
            return true;
        }

        if ((kind is (int)JournalEntryKind.Bill or (int)JournalEntryKind.Receipt or (int)JournalEntryKind.Expense
                or (int)JournalEntryKind.OwnerUtility or (int)JournalEntryKind.OwnerTransfer)
            && line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId)
        {
            result = BuildResult("Expense", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        if ((kind is (int)JournalEntryKind.Bill or (int)JournalEntryKind.Receipt)
            && line.DefaultOwnerExpAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnerExpAccountId)
        {
            result = BuildResult("Expense", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        // Charge JE: line-level buckets still use account / charge labels (one Kind, many line roles).
        if (kind == (int)JournalEntryKind.Charge || kind == 0)
        {
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

            if (sourceTypeId == (int)SourceType.Invoice
                && line.DefaultTenantIncAccountId is > 0
                && line.ChartOfAccountId == line.DefaultTenantIncAccountId
                && credit > 0)
            {
                result = BuildResult("TenantIncome", credit - debit, line, memo, sourceTypeId);
                return true;
            }
        }

        // Work order / linens owner AP (Kind.Expense or source-typed) without dedicated Kind on older rows.
        if (sourceTypeId is (int)SourceType.Bill or (int)SourceType.Receipt or (int)SourceType.WorkOrder or (int)SourceType.LinensAndTowels
            && line.DefaultOwnActPayableAccountId is > 0
            && line.ChartOfAccountId == line.DefaultOwnActPayableAccountId)
        {
            result = BuildResult("Expense", debit - credit, line, memo, sourceTypeId);
            return true;
        }

        return false;
    }

    public static IEnumerable<string> ExtractReachBackInvoiceCodes(IEnumerable<JournalEntryRecapClassificationLine> inDateRangeLines)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in inDateRangeLines.Where(line => line.IsInDateRange))
        {
            var kind = line.JournalEntryKindId ?? 0;
            var sourceDocumentCode = (line.SourceDocumentCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sourceDocumentCode))
                continue;

            if (kind == (int)JournalEntryKind.Payment
                && line.DefaultUndepFundsAccountId is > 0
                && line.ChartOfAccountId == line.DefaultUndepFundsAccountId)
            {
                codes.Add(sourceDocumentCode);
                continue;
            }

            if (kind is (int)JournalEntryKind.PrePaymentReceive or (int)JournalEntryKind.PrePaymentApply
                && line.DefaultPrePayAccountId is > 0
                && line.ChartOfAccountId == line.DefaultPrePayAccountId
                && line.Credit - line.Debit < 0)
            {
                codes.Add(sourceDocumentCode);
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
            Activity = ResolveActivity(line.JournalEntryKindId, sourceTypeId, memo)
        };
    }

    private static string ResolveActivity(int? journalEntryKindId, int sourceTypeId, string memo)
    {
        return journalEntryKindId switch
        {
            (int)JournalEntryKind.OwnerExpected or (int)JournalEntryKind.OwnerActual => "Owner",
            (int)JournalEntryKind.Charge => "Invoice",
            (int)JournalEntryKind.Payment or (int)JournalEntryKind.PrePaymentReceive or (int)JournalEntryKind.PrePaymentApply => "Payment",
            (int)JournalEntryKind.Bill => "Bill",
            (int)JournalEntryKind.Receipt => "Receipt",
            (int)JournalEntryKind.Expense => "WorkOrder",
            _ => sourceTypeId switch
            {
                (int)SourceType.Invoice => memo.Contains(": Owner:", StringComparison.Ordinal) ? "Owner" : "Invoice",
                (int)SourceType.InvoicePayment => "Payment",
                (int)SourceType.Bill => "Bill",
                (int)SourceType.Receipt => "Receipt",
                (int)SourceType.WorkOrder => "WorkOrder",
                (int)SourceType.LinensAndTowels => "LinensAndTowels",
                _ => "Other"
            }
        };
    }
}

public sealed class JournalEntryRecapClassificationLine
{
    public int? SourceTypeId { get; set; }
    public int? JournalEntryKindId { get; set; }
    public string? SourceDocumentCode { get; set; }
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
    public bool IsCashOnly { get; set; }
    public bool IsInDateRange { get; set; } = true;
}

public readonly struct JournalEntryRecapClassificationResult
{
    public string RecapCategory { get; init; }
    public decimal Amount { get; init; }
    public string Activity { get; init; }
}
