using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class SecurityDepositRePaymentTests
{
    private const string ReservationCode = "R-000001";
    private const int ArAccountId = AccountingManagerJournalEntryTestSupport.AccountsReceivableAccountId;
    private const int SdLiabilityAccountId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositLiabilityAccountId;
    private const int UndepositedFundsAccountId = AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId;

    [Fact]
    public async Task RePayment_DebitsSecurityDepositLiability_CreditsAccountsReceivablePerInvoice()
    {
        var (manager, context) = CreateManager();
        var september = TrackOutstandingInvoice(context, "R-000001-001", new DateOnly(2026, 9, 1), 82.58m, "Sewer");
        var october = TrackOutstandingInvoice(context, "R-000001-002", new DateOnly(2026, 10, 1), 100m, "Sewer");

        var payment = await ApplyRePaymentAsync(manager, 182.58m, new DateOnly(2026, 10, 20), october, september);

        var entry = GetPaymentJournalEntry(context, payment);
        Assert.Equal(182.58m, Sum(entry, SdLiabilityAccountId, line => line.Debit));
        Assert.Equal(0m, Sum(entry, SdLiabilityAccountId, line => line.Credit));
        Assert.DoesNotContain(entry.JournalEntryLines, line => line.ChartOfAccountId == UndepositedFundsAccountId);

        var arLines = entry.JournalEntryLines.Where(line => line.ChartOfAccountId == ArAccountId).ToList();
        Assert.Equal(2, arLines.Count);
        Assert.All(arLines, line => Assert.Equal(0m, line.Debit));
        Assert.Equal(82.58m, arLines.Single(line => line.Memo.Contains(september.InvoiceCode)).Credit);
        Assert.Equal(100m, arLines.Single(line => line.Memo.Contains(october.InvoiceCode)).Credit);
        Assert.Equal(entry.JournalEntryLines.Sum(line => line.Debit), entry.JournalEntryLines.Sum(line => line.Credit));
    }

    [Fact]
    public async Task RePayment_PaysOldestInvoiceFirst()
    {
        var (manager, context) = CreateManager();
        var september = TrackOutstandingInvoice(context, "R-000001-001", new DateOnly(2026, 9, 1), 82.58m, "Sewer");
        var october = TrackOutstandingInvoice(context, "R-000001-002", new DateOnly(2026, 10, 1), 100m, "Sewer");

        var payment = await ApplyRePaymentAsync(manager, 100m, new DateOnly(2026, 10, 20), october, september);

        Assert.Equal(82.58m, PaidByPayment(context, september, payment));
        Assert.Equal(17.42m, PaidByPayment(context, october, payment));

        var entry = GetPaymentJournalEntry(context, payment);
        Assert.Equal(100m, Sum(entry, SdLiabilityAccountId, line => line.Debit));
        Assert.Equal(100m, Sum(entry, ArAccountId, line => line.Credit));
    }

    [Fact]
    public async Task RePayment_SingleInvoice_PostsTwoLines()
    {
        var (manager, context) = CreateManager();
        var invoice = TrackOutstandingInvoice(context, "R-000001-001", new DateOnly(2026, 10, 1), 259.14m, "Cleaning");

        var payment = await ApplyRePaymentAsync(manager, 259.14m, new DateOnly(2026, 10, 20), invoice);

        var entry = GetPaymentJournalEntry(context, payment);
        Assert.Equal(2, entry.JournalEntryLines.Count);
        Assert.Equal(259.14m, Sum(entry, SdLiabilityAccountId, line => line.Debit));
        Assert.Equal(259.14m, Sum(entry, ArAccountId, line => line.Credit));
        Assert.Equal(259.14m, PaidByPayment(context, invoice, payment));
    }

    [Fact]
    public async Task RePayment_CreatesNoOwnerOrPrePaymentEntries()
    {
        var (manager, context) = CreateManager(enableOwnerShare: true);
        var invoice = TrackOutstandingInvoice(context, "R-000001-002", new DateOnly(2026, 10, 1), 1500m, "Rent");

        var payment = await ApplyRePaymentAsync(manager, 1500m, new DateOnly(2026, 9, 20), invoice);

        var entry = Assert.Single(context.ActiveJournalEntries);
        Assert.Equal((int)SourceType.InvoicePayment, entry.SourceTypeId);
        Assert.Equal(payment.PaymentId, entry.SourceId);
        Assert.DoesNotContain(context.ActiveJournalEntries, journalEntry =>
            journalEntry.JournalEntryKindId is JournalEntryKind.OwnerActual
                or JournalEntryKind.PrePaymentReceive
                or JournalEntryKind.PrePaymentApply);
    }

    private static (AccountingManager Manager, AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context) CreateManager(bool enableOwnerShare = false)
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            deposit: 2000m,
            hasPets: false);
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation, enableOwnerShare: enableOwnerShare);
        return (context.CreateManager(), context);
    }

    private static Invoice TrackOutstandingInvoice(
        AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context,
        string invoiceCode,
        DateOnly accountingPeriod,
        decimal amount,
        string description)
    {
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
            TotalAmount = amount,
            IsActive = true
        };
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 1,
            ReservationId = invoice.ReservationId,
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Amount = amount,
            Description = description,
            LedgerLineDate = accountingPeriod
        });
        context.TrackInvoice(invoice);
        return invoice;
    }

    private static Task<Payment> ApplyRePaymentAsync(AccountingManager manager, decimal amount, DateOnly paymentDate, params Invoice[] invoices)
    {
        var payment = new Payment
        {
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            PaymentCode = "PMT-SD-1",
            PaymentDate = paymentDate,
            Amount = amount,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.PaymentCostCodeId,
            Description = $"{ReservationCode}: Security Deposit Re-Payment",
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)PaymentType.SecurityDeposit,
            ChartOfAccountId = SdLiabilityAccountId,
            IsActive = true,
            CreatedBy = AccountingManagerJournalEntryTestSupport.CurrentUser
        };

        return manager.ApplyInvoicePaymentAsync(
            payment,
            invoices.Select(invoice => invoice.InvoiceId).ToList(),
            null,
            AccountingManagerJournalEntryTestSupport.OfficeId.ToString(),
            AccountingManagerJournalEntryTestSupport.CurrentUser);
    }

    private static JournalEntry GetPaymentJournalEntry(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, Payment payment)
        => Assert.Single(
            context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.InvoicePayment && entry.SourceId == payment.PaymentId);

    private static decimal PaidByPayment(AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, Invoice invoice, Payment payment)
        => context.GetInvoice(invoice.InvoiceId)!.LedgerLines
            .Where(line => line.PaymentId == payment.PaymentId)
            .Sum(line => line.Amount);

    private static decimal Sum(JournalEntry entry, int accountId, Func<JournalEntryLine, decimal> selector)
        => entry.JournalEntryLines.Where(line => line.ChartOfAccountId == accountId).Sum(selector);
}
