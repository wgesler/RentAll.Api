using RentAll.Domain.Models;
using System.Text.RegularExpressions;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private static readonly Regex DocumentSourceCodePattern = new(@"\b(?:WO-[A-Za-z0-9-]+|R-\d+(?:-\d+)*|RC-[A-Za-z0-9-]+|TR-[A-Za-z0-9-]+|DP-[A-Za-z0-9-]+)\b", RegexOptions.Compiled);
    private static readonly Regex InvoiceSourceCodePattern = new(@"\bR-\d+-\d+\b", RegexOptions.Compiled);
    private static readonly Regex OwnerStartingBalanceMemoPattern = new(@": Owner: BAL-\d{2}-\d{4}$", RegexOptions.Compiled);
    private static readonly Regex OwnerExpectedRentMemoPattern = new(@"^R-\d+-\d+: Owner: Expected: .+$", RegexOptions.Compiled);
    private static readonly Regex OwnerActualRentMemoPattern = new(@"^R-\d+-\d+: Owner: Actual: .+$", RegexOptions.Compiled);
    private static readonly Regex OwnerPaymentMemoPattern = new(@"^R-\d+-\d+: Owner: Payment: .+$", RegexOptions.Compiled);
    private static readonly Regex OwnerBillMemoPattern = new(@"^RC-[^:]+: Owner: .+$", RegexOptions.Compiled);
    private static readonly Regex OwnerWorkOrderMemoPattern = new(@"^WO-[^:]+: Owner: .+$", RegexOptions.Compiled);

    #region Invoice Memo

    // Example: R-001053-001: Rental Fee (04/01-04/30)
    public static string BuildInvoiceMemo(string invoiceCode, string description)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoiceCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{invoiceCode.Trim()}: {description.Trim()}";
    }

    // Example: R-001053-001: Rental Fee (04/01-04/30)
    public static string BuildInvoiceChargeLineMemo(string invoiceCode, string lineDescription)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoiceCode));
        if (string.IsNullOrWhiteSpace(lineDescription))
            throw new ArgumentException("Line description is required.", nameof(lineDescription));

        return $"{invoiceCode.Trim()}: {lineDescription.Trim()}";
    }

    public static string BuildInvoiceChargeLineMemo(Invoice invoice, LedgerLine line)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoice));
        if (string.IsNullOrWhiteSpace(line.Description))
            throw new ArgumentException("Line description is required.", nameof(line));

        return $"{invoice.InvoiceCode.Trim()}: {line.Description.Trim()}";
    }

    // Example: Payment: Check #123
    public static string BuildPaymentDocumentMemo(string paymentDescription)
    {
        if (string.IsNullOrWhiteSpace(paymentDescription))
            throw new ArgumentException("Payment description is required.", nameof(paymentDescription));

        return $"Payment: {paymentDescription.Trim()}";
    }

    // Example: R-001053-001: Payment: Check #123
    public static string BuildInvoicePaymentMemo(string invoiceCode, string ledgerLineDescription)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoiceCode));
        if (string.IsNullOrWhiteSpace(ledgerLineDescription))
            throw new ArgumentException("Ledger line description is required.", nameof(ledgerLineDescription));

        return $"{invoiceCode.Trim()}: Payment: {ledgerLineDescription.Trim()}";
    }

    // Example: R-001053-001: Payment: Check #123
    public static JournalEntryMemoMatch MatchPaymentMemo(string? journalMemo, string? lineMemo = null)
        => MatchPaymentMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchPaymentMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo) || !normalizedMemo.Contains(": Payment:", StringComparison.Ordinal))
            return JournalEntryMemoMatch.None;

        var sourceCode = normalizedMemo.Split(": Payment:", 2, StringSplitOptions.None)[0].Trim();
        var detail = normalizedMemo.Split(": Payment:", 2, StringSplitOptions.None).Length > 1
            ? normalizedMemo.Split(": Payment:", 2, StringSplitOptions.None)[1].Trim()
            : string.Empty;

        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.Payment,
            SourceCode = sourceCode,
            Detail = detail
        };
    }

    // Example: R-001053-001: Prepayment: Check #123
    public static string BuildInvoicePrePaymentMemo(string invoiceCode, string ledgerLineDescription)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoiceCode));
        if (string.IsNullOrWhiteSpace(ledgerLineDescription))
            throw new ArgumentException("Ledger line description is required.", nameof(ledgerLineDescription));

        return $"{invoiceCode.Trim()}: Prepayment: {ledgerLineDescription.Trim()}";
    }

    // Example: R-001053-001: Prepayment: Check #123
    public static JournalEntryMemoMatch MatchPrePaymentMemo(string? journalMemo, string? lineMemo = null)
        => MatchPrePaymentMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchPrePaymentMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo) || !normalizedMemo.Contains(": Prepayment:", StringComparison.Ordinal))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Prepayment:", 2, StringSplitOptions.None);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.PrePayment,
            SourceCode = parts[0].Trim(),
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    // Example: R-001053-001: Accounts Receivable
    public static string BuildAccountsReceivableMemo(string invoiceCode)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoiceCode));

        return $"{invoiceCode.Trim()}: Accounts Receivable";
    }

    // Example: R-001053-001: Accounts Receivable
    public static JournalEntryMemoMatch MatchAccountsReceivableMemo(string? journalMemo, string? lineMemo = null)
        => MatchAccountsReceivableMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchAccountsReceivableMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!normalizedMemo.EndsWith(": Accounts Receivable", StringComparison.Ordinal))
            return JournalEntryMemoMatch.None;

        var sourceCode = normalizedMemo[..^": Accounts Receivable".Length].Trim();
        if (string.IsNullOrWhiteSpace(sourceCode))
            return JournalEntryMemoMatch.None;

        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.AccountsReceivable,
            SourceCode = sourceCode,
            Detail = normalizedMemo
        };
    }

    #endregion

    #region Owner Memo

    // Example: R-001053-001: Owner: Expected: Rental Fee (04/01-04/30)
    public static string BuildOwnerExpectedRentMemo(Invoice invoice)
    {
        if (!TryGetInvoiceRentalLedgerLine(invoice, out var rentalLine))
            throw new InvalidOperationException("Invoice rental ledger line is required to build owner expected rent memo.");

        if (string.IsNullOrWhiteSpace(invoice.InvoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoice));

        if (string.IsNullOrWhiteSpace(rentalLine.Description))
            throw new ArgumentException("Rental ledger line description is required.");

        return $"{invoice.InvoiceCode.Trim()}: Owner: Expected: {rentalLine.Description.Trim()}";
    }

    // Example: R-001053-001: Owner: Expected: Rental Fee (04/01-04/30)
    public static JournalEntryMemoMatch MatchOwnerExpectedRentMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerExpectedRentMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchOwnerExpectedRentMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!OwnerExpectedRentMemoPattern.IsMatch(normalizedMemo))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Owner: Expected: ", 2, StringSplitOptions.None);
        _ = TryParseInvoiceSourceCodeFromMemo(normalizedMemo, out var invoiceSourceCode);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.OwnerRent,
            SourceCode = invoiceSourceCode,
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    // Example: R-001053-001: Owner: Actual: Rental Fee (04/01-04/30)
    public static string BuildOwnerActualRentMemo(Invoice invoice)
    {
        if (!TryGetInvoiceRentalLedgerLine(invoice, out var rentalLine))
            throw new InvalidOperationException("Invoice rental ledger line is required to build owner actual rent memo.");

        if (string.IsNullOrWhiteSpace(invoice.InvoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoice));

        if (string.IsNullOrWhiteSpace(rentalLine.Description))
            throw new ArgumentException("Rental ledger line description is required.");

        return $"{invoice.InvoiceCode.Trim()}: Owner: Actual: {rentalLine.Description.Trim()}";
    }

    // Example: R-001053-001: Owner: Actual: Rental Fee (04/01-04/30) (Check #1234)
    public static string BuildOwnerActualRentMemo(Invoice invoice, LedgerLine paymentLedgerLine)
    {
        var memo = BuildOwnerActualRentMemo(invoice);
        if (paymentLedgerLine == null || string.IsNullOrWhiteSpace(paymentLedgerLine.Description))
            return memo;

        return $"{memo} ({paymentLedgerLine.Description.Trim()})";
    }

    // Example: R-001053-001: Owner: Actual: Rental Fee (04/01-04/30)
    public static JournalEntryMemoMatch MatchOwnerActualRentMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerActualRentMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchOwnerActualRentMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!OwnerActualRentMemoPattern.IsMatch(normalizedMemo))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Owner: Actual: ", 2, StringSplitOptions.None);
        _ = TryParseInvoiceSourceCodeFromMemo(normalizedMemo, out var invoiceSourceCode);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.OwnerRentActual,
            SourceCode = invoiceSourceCode,
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    public static JournalEntryMemoMatch MatchOwnerRentMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerExpectedRentMemo(journalMemo, lineMemo);

    public static JournalEntryMemoMatch MatchOwnerRentMemo(string? memo)
        => MatchOwnerExpectedRentMemo(memo);

    // Example: R-001053-001: Owner: Payment: Check #1234
    public static string BuildOwnerPaymentMemo(string invoiceCode, string checkOrAchNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceCode))
            throw new ArgumentException("Invoice code is required.", nameof(invoiceCode));
        if (string.IsNullOrWhiteSpace(checkOrAchNumber))
            throw new ArgumentException("Check or ACH number is required.", nameof(checkOrAchNumber));

        return $"{invoiceCode.Trim()}: Owner: Payment: {checkOrAchNumber.Trim()}";
    }

    // Example: R-001053-001: Owner: Payment: Check #1234
    public static JournalEntryMemoMatch MatchOwnerPaymentMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerPaymentMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchOwnerPaymentMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!OwnerPaymentMemoPattern.IsMatch(normalizedMemo))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Owner: Payment: ", 2, StringSplitOptions.None);
        _ = TryParseInvoiceSourceCodeFromMemo(normalizedMemo, out var invoiceSourceCode);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.OwnerPayment,
            SourceCode = invoiceSourceCode,
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    // Example: RC-000123: Owner: City of Littleton - Water
    public static string BuildOwnerBillMemo(string receiptCode, string description)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{receiptCode.Trim()}: Owner: {description.Trim()}";
    }

    // Example: RC-000123: Owner: Utility: City of Littleton - Water
    public static string BuildOwnerUtilityBillMemo(string receiptCode, string description)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{receiptCode.Trim()}: Owner: Utility: {description.Trim()}";
    }

    // Example: RC-000123: Owner: City of Littleton - Water
    public static JournalEntryMemoMatch MatchOwnerBillMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerBillMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchOwnerBillMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!OwnerBillMemoPattern.IsMatch(normalizedMemo))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Owner: ", 2, StringSplitOptions.None);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.OwnerBill,
            SourceCode = parts[0].Trim(),
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    // Example: WO-123: Owner: HVAC Repair
    public static string BuildOwnerWorkOrderMemo(string workOrderCode, string description)
    {
        if (string.IsNullOrWhiteSpace(workOrderCode))
            throw new ArgumentException("Work order code is required.", nameof(workOrderCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{workOrderCode.Trim()}: Owner: {description.Trim()}";
    }

    // Example: WO-123: Owner: HVAC Repair
    public static JournalEntryMemoMatch MatchOwnerWorkOrderMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerWorkOrderMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchOwnerWorkOrderMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!OwnerWorkOrderMemoPattern.IsMatch(normalizedMemo))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Owner: ", 2, StringSplitOptions.None);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.OwnerWorkOrder,
            SourceCode = parts[0].Trim(),
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    public static JournalEntryMemoMatch MatchOwnerExpenseMemo(string? journalMemo, string? lineMemo = null)
    {
        var billMatch = MatchOwnerBillMemo(journalMemo, lineMemo);
        if (billMatch.IsMatch)
            return billMatch;

        return MatchOwnerWorkOrderMemo(journalMemo, lineMemo);
    }

    // Example: BAR505: Owner: BAL-03-2026
    public static JournalEntryMemoMatch MatchOwnerStartingBalanceMemo(string? journalMemo, string? lineMemo = null)
        => MatchOwnerStartingBalanceMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchOwnerStartingBalanceMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (!OwnerStartingBalanceMemoPattern.IsMatch(normalizedMemo))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Owner: ", 2, StringSplitOptions.None);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.OwnerStartingBalance,
            SourceCode = parts[0].Trim(),
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    // Example: BAR505: Owner: Monthly Linen & Towel
    public static string BuildOwnerLinenAndTowelMemo(string propertyCode, bool isMonthly)
    {
        if (string.IsNullOrWhiteSpace(propertyCode))
            throw new ArgumentException("Property code is required.", nameof(propertyCode));

        var cadenceLabel = isMonthly ? "Monthly" : "Annual";
        return $"{propertyCode.Trim()}: Owner: {cadenceLabel} Linen & Towel";
    }

    // Example: BAR505: Owner: Annual Linen & Towel Unused Portion
    public static string BuildOwnerLinenAndTowelUnusedMemo(string propertyCode)
    {
        if (string.IsNullOrWhiteSpace(propertyCode))
            throw new ArgumentException("Property code is required.", nameof(propertyCode));

        return $"{propertyCode.Trim()}: Owner: Annual Linen & Towel Unused Portion";
    }

    #endregion

    #region Linen And Towel Memo

    // Example: BAR505: Monthly Linen & Towel
    public static string BuildLinenAndTowelJournalMemo(string propertyCode, bool isMonthly)
    {
        if (string.IsNullOrWhiteSpace(propertyCode))
            throw new ArgumentException("Property code is required.", nameof(propertyCode));

        var cadenceLabel = isMonthly ? "Monthly" : "Annual";
        return $"{propertyCode.Trim()}: {cadenceLabel} Linen & Towel";
    }

    // Example: BAR505: Monthly Linen & Towel
    public static string BuildLinenAndTowelIncomeMemo(string propertyCode, bool isMonthly)
    {
        if (string.IsNullOrWhiteSpace(propertyCode))
            throw new ArgumentException("Property code is required.", nameof(propertyCode));

        var cadenceLabel = isMonthly ? "Monthly" : "Annual";
        return $"{propertyCode.Trim()}: {cadenceLabel} Linen & Towel";
    }

    // Example: BAR505: Annual Linen & Towel Unused Portion
    public static string BuildLinenAndTowelUnusedMemo(string propertyCode)
    {
        if (string.IsNullOrWhiteSpace(propertyCode))
            throw new ArgumentException("Property code is required.", nameof(propertyCode));

        return $"{propertyCode.Trim()}: Annual Linen & Towel Unused Portion";
    }

    #endregion

    #region Transfer And Deposit Memo

    // Example: TR-123: Transfer: 3/11/2026
    public static string BuildTransferMemo(string transferCode, DateOnly transferDate)
    {
        if (string.IsNullOrWhiteSpace(transferCode))
            throw new ArgumentException("Transfer code is required.", nameof(transferCode));
        if (transferDate == default)
            throw new ArgumentException("Transfer date is required.", nameof(transferDate));

        return $"{transferCode.Trim()}: Transfer: {transferDate:M/d/yyyy}";
    }

    // Example: TR-123: Transfer: 3/11/2026
    public static JournalEntryMemoMatch MatchTransferMemo(string? journalMemo, string? lineMemo = null)
        => MatchTransferMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchTransferMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo) || !normalizedMemo.Contains(": Transfer: ", StringComparison.Ordinal))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Transfer: ", 2, StringSplitOptions.None);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.Transfer,
            SourceCode = parts[0].Trim(),
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    // Example: DP-123: Deposit: Wells Fargo 3/11/2026
    public static string BuildDepositMemo(string depositCode, string bankAccountDisplayName, DateOnly depositDate)
    {
        if (string.IsNullOrWhiteSpace(depositCode))
            throw new ArgumentException("Deposit code is required.", nameof(depositCode));
        if (string.IsNullOrWhiteSpace(bankAccountDisplayName))
            throw new ArgumentException("Bank account display name is required.", nameof(bankAccountDisplayName));
        if (depositDate == default)
            throw new ArgumentException("Deposit date is required.", nameof(depositDate));

        return $"{depositCode.Trim()}: Deposit: {bankAccountDisplayName.Trim()} {depositDate:M/d/yyyy}";
    }

    // Example: DP-123: Deposit: Wells Fargo 3/11/2026
    public static JournalEntryMemoMatch MatchDepositMemo(string? journalMemo, string? lineMemo = null)
        => MatchDepositMemo(CoalesceJournalEntryMemo(journalMemo, lineMemo));

    public static JournalEntryMemoMatch MatchDepositMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo) || !normalizedMemo.Contains(": Deposit: ", StringComparison.Ordinal))
            return JournalEntryMemoMatch.None;

        var parts = normalizedMemo.Split(": Deposit: ", 2, StringSplitOptions.None);
        return new JournalEntryMemoMatch
        {
            Category = JournalEntryMemoCategory.Deposit,
            SourceCode = parts[0].Trim(),
            Detail = parts.Length > 1 ? parts[1].Trim() : string.Empty
        };
    }

    #endregion

    #region Bill And Receipt Memo

    // Example: RC-000123: Bill: Carpet Clean
    public static string BuildBillMemo(string receiptCode, string description)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{receiptCode.Trim()}: Bill: {description.Trim()}";
    }

    // Example: RC-000123: Receipt: Spoons
    public static string BuildReceiptMemo(string receiptCode, string description)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{receiptCode.Trim()}: Receipt: {description.Trim()}";
    }

    // Example: RC-000123: Owner: Spoons
    public static string BuildOwnerReceiptMemo(string receiptCode, string description)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return $"{receiptCode.Trim()}: Owner: {description.Trim()}";
    }

    // Example: RC-000123: Receipt: Visa
    public static string BuildReceiptBankCardMemo(string receiptCode, string bankCardDisplayName)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(bankCardDisplayName))
            throw new ArgumentException("Bank card display name is required.", nameof(bankCardDisplayName));

        return $"{receiptCode.Trim()}: Receipt: {bankCardDisplayName.Trim()}";
    }

    // Example: RC-000123: Receipt: Denver Office
    public static string BuildReceiptOfficeMemo(string receiptCode, string officeName)
    {
        if (string.IsNullOrWhiteSpace(receiptCode))
            throw new ArgumentException("Receipt code is required.", nameof(receiptCode));
        if (string.IsNullOrWhiteSpace(officeName))
            throw new ArgumentException("Office name is required.", nameof(officeName));

        return $"{receiptCode.Trim()}: Receipt: {officeName.Trim()}";
    }

    #endregion

    #region Retained Earnings Memo

    public static string BuildRetainedEarningsMemo(DateOnly processingDate)
        => $"Retained Earnings for {processingDate:MM/dd/yyyy}";

    #endregion

    #region Departure And Pet Memo

    // Example: R-001053: Departure Fee
    public static string BuildDepartureFeeMemo(string reservationCode)
    {
        if (string.IsNullOrWhiteSpace(reservationCode))
            throw new ArgumentException("Reservation code is required.", nameof(reservationCode));

        return $"{reservationCode.Trim()}: Departure Fee";
    }

    // Example: R-001053: Departure Fee
    public static string BuildDepartureFeeIncomeMemo(string reservationCode)
    {
        if (string.IsNullOrWhiteSpace(reservationCode))
            throw new ArgumentException("Reservation code is required.", nameof(reservationCode));

        return $"{reservationCode.Trim()}: Departure Fee";
    }

    // Example: R-001053: Departure Fee
    public static string BuildDeparturesMemo(string reservationCode)
    {
        if (string.IsNullOrWhiteSpace(reservationCode))
            throw new ArgumentException("Reservation code is required.", nameof(reservationCode));

        return $"{reservationCode.Trim()}: Departure Fee";
    }

    // Example: R-001053: Pet Fee
    public static string BuildPetFeeMemo(string reservationCode)
    {
        if (string.IsNullOrWhiteSpace(reservationCode))
            throw new ArgumentException("Reservation code is required.", nameof(reservationCode));

        return $"{reservationCode.Trim()}: Pet Fee";
    }

    #endregion

    #region Memo Parsing

    public static string CoalesceJournalEntryMemo(string? journalMemo, string? lineMemo)
    {
        var lineText = (lineMemo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(lineText))
            return lineText;

        return (journalMemo ?? string.Empty).Trim();
    }

    public static string? TryParseInvoiceSourceCodeFromMemo(string? memo) => TryParseInvoiceSourceCodeFromMemo(memo, out var sourceCode) ? sourceCode : null;

    public static bool TryParseInvoiceSourceCodeFromMemo(string? memo, out string sourceCode)
    {
        sourceCode = string.Empty;
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo))
            return false;

        foreach (Match match in InvoiceSourceCodePattern.Matches(normalizedMemo))
        {
            var candidate = match.Value.Trim();
            if (InvoiceSourceCodePattern.IsMatch(candidate))
            {
                sourceCode = candidate;
                return true;
            }
        }

        return false;
    }

    public static string? TryParseDocumentSourceCodeFromMemo(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo))
            return null;

        foreach (Match match in DocumentSourceCodePattern.Matches(normalizedMemo))
        {
            var candidate = match.Value.Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        return ExtractMemoSourceCode(normalizedMemo);
    }

    public static string? TryParseMemoSourceCode(string? memo) => ExtractMemoSourceCode(memo);

    public static string StripTenantPaymentMemoForDisplay(string? memo)
    {
        var trimmed = (memo ?? string.Empty).Trim();
        var paymentIndex = trimmed.IndexOf(": Payment: ", StringComparison.Ordinal);
        if (paymentIndex >= 0)
            return trimmed[(paymentIndex + ": Payment: ".Length)..].Trim();

        var prePaymentIndex = trimmed.IndexOf(": Prepayment: ", StringComparison.Ordinal);
        if (prePaymentIndex >= 0)
            return trimmed[(prePaymentIndex + ": Prepayment: ".Length)..].Trim();

        return trimmed;
    }

    public static string StripOwnerMemoPrefixForDisplay(string? memo)
    {
        var trimmed = (memo ?? string.Empty).Trim();
        var ownerIndex = trimmed.IndexOf(": Owner: ", StringComparison.Ordinal);
        if (ownerIndex < 0)
            return trimmed;

        var detail = trimmed[(ownerIndex + ": Owner: ".Length)..].Trim();
        if (detail.StartsWith("Expected: ", StringComparison.Ordinal))
            return detail["Expected: ".Length..].Trim();
        if (detail.StartsWith("Actual: ", StringComparison.Ordinal))
            return detail["Actual: ".Length..].Trim();
        if (detail.StartsWith("Payment: ", StringComparison.Ordinal))
            return detail["Payment: ".Length..].Trim();

        return detail;
    }

    private static string ExtractMemoSourceCode(string? memo)
    {
        var normalizedMemo = (memo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMemo))
            return string.Empty;

        var separatorIndex = normalizedMemo.IndexOf(':');
        return separatorIndex > 0 ? normalizedMemo[..separatorIndex].Trim() : normalizedMemo;
    }

    #endregion
}
