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
    public void Classify_OwnerExpected_UsesKind()
    {
        var line = BuildLine(
            kind: JournalEntryKind.OwnerExpected,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            credit: 49.70m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("OwnerRent", result.RecapCategory);
        Assert.Equal(49.70m, result.Amount);
    }

    [Fact]
    public void Classify_OwnerActual_UsesKind()
    {
        var line = BuildLine(
            kind: JournalEntryKind.OwnerActual,
            sourceTypeId: (int)SourceType.InvoicePayment,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            credit: 49.70m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("OwnerRentActual", result.RecapCategory);
        Assert.Equal(49.70m, result.Amount);
    }

    [Fact]
    public void Classify_Payment_UsesKind()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Payment,
            sourceTypeId: (int)SourceType.InvoicePayment,
            chartOfAccountId: UndepFunds,
            undepFundsAccountId: UndepFunds,
            debit: 2130m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("Payment", result.RecapCategory);
        Assert.Equal(2130m, result.Amount);
    }

    [Fact]
    public void Classify_Manual_OwnerAp_IsExpense()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Manual,
            sourceTypeId: (int)SourceType.Journal,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            debit: 250m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("Expense", result.RecapCategory);
        Assert.Equal(250m, result.Amount);
    }

    [Fact]
    public void Classify_Manual_OwnerAp_OwnerStartingBalanceMemo_IsExcluded()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Manual,
            sourceTypeId: (int)SourceType.Journal,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            credit: 500m,
            memo: "BUCK805: Owner: BAL-07-2026");

        Assert.False(JournalEntryRecapLineClassifier.TryClassify(line, out _));
    }

    [Fact]
    public void Classify_Manual_OwnerAp_OwnerPaymentMemo_IsOwnerPayment()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Manual,
            sourceTypeId: (int)SourceType.Journal,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            memo: "DRE105: Owner: Payment: ACH",
            credit: 2649.32m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("OwnerPayment", result.RecapCategory);
        Assert.Equal(2649.32m, result.Amount);
    }

    [Fact]
    public void Classify_Manual_OwnerAp_VendorCredit_IsExpense()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Manual,
            sourceTypeId: (int)SourceType.Receipt,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            memo: "RC-000001: Owner: City of Littleton - Water (credit)",
            credit: 35m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("Expense", result.RecapCategory);
        Assert.Equal(-35m, result.Amount);
    }

    [Fact]
    public void Classify_OwnerPayment_BillPayment_UsesKind()
    {
        var line = BuildLine(
            kind: JournalEntryKind.BillPayment,
            sourceTypeId: (int)SourceType.OwnerDistribution,
            chartOfAccountId: OwnerAp,
            ownerApAccountId: OwnerAp,
            memo: "P-001053: Owner: Payment: ACH",
            debit: 4119.99m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("OwnerPayment", result.RecapCategory);
        Assert.Equal(4119.99m, result.Amount);
    }

    [Fact]
    public void Classify_SecurityDepositWaiver_UsesExactChargeMemoSuffix()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Charge,
            chartOfAccountId: 999,
            tenantIncomeAccountId: TenantIncome,
            memo: "R-000177-001: Security Deposit Waiver",
            credit: 60m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("SDW", result.RecapCategory);
        Assert.Equal(60m, result.Amount);
    }

    [Fact]
    public void Classify_SecurityDepositActual_UsesKind()
    {
        const int escrowSecDep = 910;
        var line = BuildLine(
            kind: JournalEntryKind.SecurityDepositActual,
            chartOfAccountId: escrowSecDep,
            escrowSecDepAccountId: escrowSecDep,
            memo: "R-000177-001: Security Deposit Actual: Security Deposit (Check #1234)",
            credit: 1500m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("SecurityDeposit", result.RecapCategory);
        Assert.Equal(1500m, result.Amount);
    }

    [Fact]
    public void Classify_SecurityDepositWaiverActual_UsesKind()
    {
        const int escrowSdw = 920;
        var line = BuildLine(
            kind: JournalEntryKind.SecurityDepositWaiverActual,
            chartOfAccountId: escrowSdw,
            escrowSdwAccountId: escrowSdw,
            memo: "R-000177-001: Security Deposit Waiver Actual: Security Deposit Waiver (Check #1234)",
            credit: 60m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("SDW", result.RecapCategory);
        Assert.Equal(60m, result.Amount);
    }

    [Fact]
    public void Classify_FeesActual_UsesKind()
    {
        const int escrowDeposit = 930;
        var line = BuildLine(
            kind: JournalEntryKind.FeesActual,
            chartOfAccountId: escrowDeposit,
            escrowDepositAccountId: escrowDeposit,
            memo: "R-000177-001: Fees Actual: Fees (Check #1234)",
            credit: 250m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("BusinessEscrow", result.RecapCategory);
        Assert.Equal(250m, result.Amount);
    }

    [Fact]
    public void Classify_SecurityDeposit_UsesExactChargeMemoSuffix()
    {
        var line = BuildLine(
            kind: JournalEntryKind.Charge,
            chartOfAccountId: 999,
            tenantIncomeAccountId: TenantIncome,
            memo: "R-000177-001: Security Deposit",
            credit: 1500m);

        Assert.True(JournalEntryRecapLineClassifier.TryClassify(line, out var result));
        Assert.Equal("SecurityDeposit", result.RecapCategory);
        Assert.Equal(1500m, result.Amount);
    }

    [Fact]
    public void ExtractReachBackInvoiceCodes_UsesKindAndSourceDocumentCode()
    {
        var lines = new[]
        {
            BuildLine(
                kind: JournalEntryKind.Payment,
                sourceTypeId: (int)SourceType.InvoicePayment,
                chartOfAccountId: UndepFunds,
                undepFundsAccountId: UndepFunds,
                sourceDocumentCode: "R-000177-001",
                debit: 100m,
                isInDateRange: true)
        };

        var codes = JournalEntryRecapLineClassifier.ExtractReachBackInvoiceCodes(lines).ToList();

        Assert.Single(codes);
        Assert.Equal("R-000177-001", codes[0]);
    }

    private static JournalEntryRecapClassificationLine BuildLine(
        JournalEntryKind kind = JournalEntryKind.Charge,
        int sourceTypeId = (int)SourceType.Invoice,
        int chartOfAccountId = TenantIncome,
        int? ownerApAccountId = null,
        int? undepFundsAccountId = null,
        int? prepayAccountId = null,
        int? accountsReceivableAccountId = null,
        int? tenantIncomeAccountId = null,
        int? escrowSecDepAccountId = null,
        int? escrowSdwAccountId = null,
        int? escrowDepositAccountId = null,
        string memo = "",
        string sourceDocumentCode = "",
        decimal debit = 0m,
        decimal credit = 0m,
        bool isInDateRange = true,
        bool isRentalIncomeAccount = false,
        bool isCashOnly = false)
    {
        return new JournalEntryRecapClassificationLine
        {
            SourceTypeId = sourceTypeId,
            JournalEntryKindId = (int)kind,
            SourceDocumentCode = sourceDocumentCode,
            ChartOfAccountId = chartOfAccountId,
            Debit = debit,
            Credit = credit,
            LineMemo = memo,
            DefaultOwnActPayableAccountId = ownerApAccountId,
            DefaultUndepFundsAccountId = undepFundsAccountId,
            DefaultPrePayAccountId = prepayAccountId,
            DefaultActRcvableAccountId = accountsReceivableAccountId ?? AccountsReceivable,
            DefaultTenantIncAccountId = tenantIncomeAccountId ?? TenantIncome,
            DefaultEscrowSecDepAccountId = escrowSecDepAccountId,
            DefaultEscrowSdwAccountId = escrowSdwAccountId,
            DefaultEscrowDepositAccountId = escrowDepositAccountId,
            IsRentalIncomeAccount = isRentalIncomeAccount,
            IsCashOnly = isCashOnly,
            IsInDateRange = isInDateRange
        };
    }
}
