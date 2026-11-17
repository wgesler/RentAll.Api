using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;
using System.Reflection;

namespace RentAll.Test;

/// <summary>
/// The four security deposit return scenarios: charges or no charges, refunded by card or by check/ACH.
/// </summary>
public class SecurityDepositRefundScenarioTests
{
    private const string ReservationCode = "R-000001";
    private const int ArAccountId = AccountingManagerJournalEntryTestSupport.AccountsReceivableAccountId;
    private const int SdLiabilityAccountId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositLiabilityAccountId;
    private const int UndepositedFundsAccountId = AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId;
    private const int BusinessBankAccountId = 9001;
    private const int EscrowDepositAccountId = 9002;
    private const int EscrowSecurityDepositAccountId = 9005;
    private const int EscrowSdwAccountId = 9008;
    private const int EscrowOwnersAccountId = 9003;

    [Fact]
    public async Task ChargesRefundedByCard()
    {
        var (manager, context) = CreateManager();
        var invoice = TrackChargeInvoice(context, 2077m);

        var rePayment = await ApplyRePaymentAsync(manager, invoice, 2100m);
        var refund = await ApplyRefundAsync(manager, invoice, 23m, PaymentType.CreditCardRefund, UndepositedFundsAccountId);
        var transfer = BuildTransferJournalEntry(manager, invoice.InvoiceCode, businessAmount: 2077m, refundAccountId: EscrowDepositAccountId, refundAmount: 23m);

        AssertTwoLineEntry(GetPaymentEntry(context, rePayment), debitAccountId: SdLiabilityAccountId, creditAccountId: ArAccountId, 2100m);
        AssertTwoLineEntry(GetPaymentEntry(context, refund), debitAccountId: ArAccountId, creditAccountId: UndepositedFundsAccountId, 23m);
        AssertTransferLines(transfer,
            (EscrowSecurityDepositAccountId, 0m, 2100m),
            (EscrowDepositAccountId, 23m, 0m),
            (BusinessBankAccountId, 2077m, 0m));
        Assert.Equal(0m, Due(context, invoice));
    }

    [Fact]
    public async Task ChargesRefundedByCheckOrAch()
    {
        var (manager, context) = CreateManager();
        var invoice = TrackChargeInvoice(context, 2077m);

        var rePayment = await ApplyRePaymentAsync(manager, invoice, 2100m);
        var refund = await ApplyRefundAsync(manager, invoice, 23m, PaymentType.Check, EscrowSecurityDepositAccountId);
        var transfer = BuildTransferJournalEntry(manager, invoice.InvoiceCode, businessAmount: 2077m, refundAccountId: EscrowSecurityDepositAccountId, refundAmount: 23m);

        AssertTwoLineEntry(GetPaymentEntry(context, rePayment), debitAccountId: SdLiabilityAccountId, creditAccountId: ArAccountId, 2100m);
        AssertTwoLineEntry(GetPaymentEntry(context, refund), debitAccountId: ArAccountId, creditAccountId: EscrowSecurityDepositAccountId, 23m);
        AssertTransferLines(transfer,
            (EscrowSecurityDepositAccountId, 0m, 2100m),
            (EscrowSecurityDepositAccountId, 23m, 0m),
            (BusinessBankAccountId, 2077m, 0m));
        Assert.Equal(0m, Due(context, invoice));
    }

    [Fact]
    public async Task NoChargesRefundedByCard()
    {
        var (manager, context) = CreateManager();
        var refundInvoice = TrackRefundInvoice(context, 3000m);

        var creditEntry = await manager.CreateJournalEntryFromInvoiceAsync(context.GetInvoice(refundInvoice.InvoiceId)!, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var refund = await ApplyRefundAsync(manager, refundInvoice, 3000m, PaymentType.CreditCardRefund, UndepositedFundsAccountId);
        var transfer = BuildTransferJournalEntry(manager, refundInvoice.InvoiceCode, businessAmount: 0m, refundAccountId: EscrowDepositAccountId, refundAmount: 3000m);

        AssertTwoLineEntry(creditEntry!, debitAccountId: SdLiabilityAccountId, creditAccountId: ArAccountId, 3000m);
        AssertTwoLineEntry(GetPaymentEntry(context, refund), debitAccountId: ArAccountId, creditAccountId: UndepositedFundsAccountId, 3000m);
        AssertTransferLines(transfer,
            (EscrowSecurityDepositAccountId, 0m, 3000m),
            (EscrowDepositAccountId, 3000m, 0m));
        Assert.Equal(0m, Due(context, refundInvoice));
    }

    [Fact]
    public async Task NoChargesRefundedByCheckOrAch()
    {
        var (manager, context) = CreateManager();
        var refundInvoice = TrackRefundInvoice(context, 3000m);

        var creditEntry = await manager.CreateJournalEntryFromInvoiceAsync(context.GetInvoice(refundInvoice.InvoiceId)!, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var refund = await ApplyRefundAsync(manager, refundInvoice, 3000m, PaymentType.Ach, EscrowSecurityDepositAccountId);

        AssertTwoLineEntry(creditEntry!, debitAccountId: SdLiabilityAccountId, creditAccountId: ArAccountId, 3000m);
        AssertTwoLineEntry(GetPaymentEntry(context, refund), debitAccountId: ArAccountId, creditAccountId: EscrowSecurityDepositAccountId, 3000m);
        Assert.Equal(0m, Due(context, refundInvoice));
    }

    private static (AccountingManager Manager, AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context) CreateManager()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false);
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        return (context.CreateManager(), context);
    }

    private static Invoice TrackChargeInvoice(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, decimal amount)
        => TrackInvoice(context, "R-000001-002", new LedgerLine
        {
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Amount = amount,
            Description = "Cleaning"
        });

    private static Invoice TrackRefundInvoice(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, decimal collected)
        => TrackInvoice(context, "R-000001-003", new LedgerLine
        {
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositCostCodeId,
            Amount = -collected,
            Description = $"{ReservationCode}: Security Deposit Credit"
        });

    private static Invoice TrackInvoice(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, string invoiceCode, LedgerLine line)
    {
        var accountingPeriod = new DateOnly(2026, 10, 1);
        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            InvoiceCode = invoiceCode,
            ReservationId = AccountingManagerJournalEntryTestSupport.ReservationId,
            ReservationCode = ReservationCode,
            PropertyId = AccountingManagerJournalEntryTestSupport.PropertyId,
            InvoiceDate = accountingPeriod,
            AccountingPeriod = accountingPeriod,
            TotalAmount = line.Amount,
            IsActive = true
        };
        line.LedgerLineId = Guid.NewGuid();
        line.InvoiceId = invoice.InvoiceId;
        line.LineNumber = 1;
        line.ReservationId = invoice.ReservationId;
        line.LedgerLineDate = accountingPeriod;
        invoice.LedgerLines.Add(line);
        context.TrackInvoice(invoice);
        return invoice;
    }

    private static Task<Payment> ApplyRePaymentAsync(AccountingManager manager, Invoice invoice, decimal collected)
        => ApplyPaymentAsync(manager, invoice, new Payment
        {
            Amount = collected,
            Description = $"{ReservationCode}: Security Deposit Re-Payment",
            PaymentTypeId = (int)PaymentType.SecurityDeposit,
            ChartOfAccountId = SdLiabilityAccountId
        });

    private static Task<Payment> ApplyRefundAsync(AccountingManager manager, Invoice invoice, decimal refundAmount, PaymentType paymentType, int refundAccountId)
        => ApplyPaymentAsync(manager, invoice, new Payment
        {
            Amount = -refundAmount,
            Description = $"{ReservationCode}: Security Deposit Refund",
            PaymentTypeId = (int)paymentType,
            ChartOfAccountId = refundAccountId
        });

    private static Task<Payment> ApplyPaymentAsync(AccountingManager manager, Invoice invoice, Payment payment)
    {
        payment.OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId;
        payment.OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId;
        payment.PaymentDate = new DateOnly(2026, 10, 20);
        payment.CostCodeId = AccountingManagerJournalEntryFeeTestSupport.PaymentCostCodeId;
        payment.PaymentKindId = (int)PaymentKind.Invoice;
        payment.IsActive = true;
        payment.CreatedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        return manager.ApplyInvoicePaymentAsync(
            payment,
            [invoice.InvoiceId],
            null,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString(),
            AccountingManagerJournalEntryTestSupport.CurrentUser);
    }

    private static JournalEntry BuildTransferJournalEntry(AccountingManager manager, string invoiceCode, decimal businessAmount, int refundAccountId, decimal refundAmount)
    {
        var description = $"{invoiceCode}: Transfer: Security Deposit Transfer";
        var splits = new List<TransferSplit>();
        if (businessAmount != 0m)
            splits.Add(new TransferSplit { Amount = businessAmount, ChartOfAccountId = BusinessBankAccountId, Description = description });
        if (refundAmount != 0m)
            splits.Add(new TransferSplit { Amount = refundAmount, ChartOfAccountId = refundAccountId, Description = $"{description} TBR" });

        var transfer = new Transfer
        {
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            TransferCode = "TR-000001",
            TransferDate = new DateOnly(2026, 10, 21),
            AccountingPeriod = new DateOnly(2026, 10, 1),
            Amount = businessAmount + refundAmount,
            Description = description,
            BankAccountId = EscrowSecurityDepositAccountId,
            IsActive = true,
            Splits = splits
        };

        var chartOfAccounts = new List<ChartOfAccount>
        {
            BankAccount(BusinessBankAccountId, "1001", "Business Bank"),
            BankAccount(EscrowDepositAccountId, "1002", "Escrow Deposit"),
            BankAccount(EscrowSecurityDepositAccountId, "1005", "Escrow Security Deposit"),
            BankAccount(EscrowSdwAccountId, "1008", "Escrow SDW"),
            BankAccount(EscrowOwnersAccountId, "1003", "Escrow Owners")
        };
        var accountingOffice = new AccountingOffice
        {
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            DefaultBankAccountId = BusinessBankAccountId,
            DefaultEscrowDepositAccountId = EscrowDepositAccountId,
            DefaultEscrowSecDepAccountId = EscrowSecurityDepositAccountId,
            DefaultEscrowSdwAccountId = EscrowSdwAccountId,
            DefaultEscrowOwnersAccountId = EscrowOwnersAccountId
        };

        var builder = typeof(AccountingManager).GetMethod("BuildJournalEntryFromTransferRecord", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (JournalEntry)builder.Invoke(manager, [transfer, chartOfAccounts, accountingOffice, AccountingManagerJournalEntryTestSupport.CurrentUser])!;
    }

    private static ChartOfAccount BankAccount(int accountId, string accountNo, string name)
        => new()
        {
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            AccountId = accountId,
            AccountNo = accountNo,
            Name = name,
            AccountType = AccountType.Bank
        };

    private static JournalEntry GetPaymentEntry(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, Payment payment)
        => Assert.Single(
            context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.InvoicePayment && entry.SourceId == payment.PaymentId);

    private static decimal Due(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, Invoice invoice)
    {
        var stored = context.GetInvoice(invoice.InvoiceId)!;
        return stored.TotalAmount - stored.PaidAmount;
    }

    private static void AssertTwoLineEntry(JournalEntry entry, int debitAccountId, int creditAccountId, decimal amount)
    {
        Assert.Equal(2, entry.JournalEntryLines.Count(line => line.Debit != 0 || line.Credit != 0));
        var debit = Assert.Single(entry.JournalEntryLines, line => line.Debit != 0);
        var credit = Assert.Single(entry.JournalEntryLines, line => line.Credit != 0);
        Assert.Equal(debitAccountId, debit.ChartOfAccountId);
        Assert.Equal(amount, debit.Debit);
        Assert.Equal(creditAccountId, credit.ChartOfAccountId);
        Assert.Equal(amount, credit.Credit);
    }

    private static void AssertTransferLines(JournalEntry entry, params (int AccountId, decimal Debit, decimal Credit)[] expected)
    {
        var actual = entry.JournalEntryLines
            .Select(line => (line.ChartOfAccountId, line.Debit, line.Credit))
            .ToArray();
        Assert.Equal(expected, actual);
    }
}
