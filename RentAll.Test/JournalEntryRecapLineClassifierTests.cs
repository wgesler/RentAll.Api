using RentAll.Domain.Accounting;
using RentAll.Domain.Enums;

namespace RentAll.Test;

public class JournalEntryRecapLineClassifierTests
{
    private const int OwnerAp = 500;
    private const int UndepFunds = 100;
    private const int PrePay = 110;
    private const int AccountsReceivable = 120;
    private const int TenantIncome = 130;

    [Fact]
    public void Classify_OwnerExpected_MatchesAccountingManagerMemo()
    {
        var line = BuildLine(
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            memo: "R-000177-001: Owner: Expected: Rent",
            credit: 49.70m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("OwnerRent", result.RecapCategory);
        Assert.Equal(49.70m, result.Amount);
    }

    [Fact]
    public void Classify_OwnerActual_MirrorsOwnRentSign()
    {
        var line = BuildLine(
            sourceTypeId: (int)SourceType.InvoicePayment,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            memo: "R-000177-001: Owner: Actual: Rent",
            credit: 49.70m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("OwnerRentActual", result.RecapCategory);
        Assert.Equal(49.70m, result.Amount);
    }

    [Fact]
    public void Classify_Payment_MatchesAccountingManagerMemo()
    {
        var line = BuildLine(
            sourceTypeId: (int)SourceType.InvoicePayment,
            chartOfAccountId: UndepFunds,
            undepFundsAccountId: UndepFunds,
            memo: "R-000177-001: Payment: Check #123",
            debit: 2130m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("Payment", result.RecapCategory);
        Assert.Equal(2130m, result.Amount);
    }

    [Fact]
    public void Classify_SecurityDepositWaiver_UsesExactChargeMemoSuffix()
    {
        var line = BuildLine(
            chartOfAccountId: 999,
            tenantIncomeAccountId: TenantIncome,
            memo: "R-000177-001: Security Deposit Waiver",
            credit: 60m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("SDW", result.RecapCategory);
        Assert.Equal(60m, result.Amount);
    }

    [Fact]
    public void ExtractReachBackInvoiceCodes_UsesPaymentMemoInvoiceCode()
    {
        var lines = new[]
        {
            BuildLine(
                sourceTypeId: (int)SourceType.InvoicePayment,
                chartOfAccountId: UndepFunds,
                undepFundsAccountId: UndepFunds,
                memo: "R-000177-001: Payment: Check #123",
                debit: 100m,
                isInDateRange: true)
        };

        var codes = JournalEntryRecapLineClassifier.ExtractReachBackInvoiceCodes(lines).ToList();

        Assert.Single(codes);
        Assert.Equal("R-000177-001", codes[0]);
    }

    private static JournalEntryRecapClassificationLine BuildLine(
        int sourceTypeId = (int)SourceType.Invoice,
        int chartOfAccountId = TenantIncome,
        int? ownerApAccountId = null,
        int? undepFundsAccountId = null,
        int? prepayAccountId = null,
        int? accountsReceivableAccountId = null,
        int? tenantIncomeAccountId = null,
        string memo = "",
        decimal debit = 0m,
        decimal credit = 0m,
        bool isInDateRange = true,
        bool isRentalIncomeAccount = false,
        bool isCashOnly = false)
    {
        return new JournalEntryRecapClassificationLine
        {
            SourceTypeId = sourceTypeId,
            ChartOfAccountId = chartOfAccountId,
            Debit = debit,
            Credit = credit,
            LineMemo = memo,
            DefaultOwnActPayableAccountId = ownerApAccountId,
            DefaultUndepFundsAccountId = undepFundsAccountId,
            DefaultPrePayAccountId = prepayAccountId,
            DefaultActRcvableAccountId = accountsReceivableAccountId ?? AccountsReceivable,
            DefaultTenantIncAccountId = tenantIncomeAccountId ?? TenantIncome,
            IsRentalIncomeAccount = isRentalIncomeAccount,
            IsCashOnly = isCashOnly,
            IsInDateRange = isInDateRange
        };
    }
}
