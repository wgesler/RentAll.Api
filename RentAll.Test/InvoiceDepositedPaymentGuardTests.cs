using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class InvoiceDepositedPaymentGuardTests
{
    [Fact]
    public async Task UpdateInvoice_ChargeOnlyChange_AllowsDepositedPaymentLine()
    {
        var scenario = await SetupDepositedPartialPaymentInvoiceAsync();
        var chargeLine = scenario.Invoice.LedgerLines.First(line => line.CostCodeId != AccountingManagerJournalEntryFeeTestSupport.PaymentCostCodeId);
        chargeLine.Amount = 1800m;
        scenario.Invoice.TotalAmount = scenario.Invoice.LedgerLines.Where(line => line.CostCodeId != AccountingManagerJournalEntryFeeTestSupport.PaymentCostCodeId).Sum(line => line.Amount);
        scenario.Invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        var updated = await scenario.Context.CreateManager().UpdateInvoiceAsync(scenario.Invoice);

        Assert.Contains(updated.LedgerLines, line => line.LedgerLineId == scenario.PaymentLineId && line.PaymentId == scenario.PaymentId);
    }

    [Fact]
    public async Task UpdateInvoice_ChangingDepositedPaymentAmount_Throws()
    {
        var scenario = await SetupDepositedPartialPaymentInvoiceAsync();
        var paymentLine = scenario.Invoice.LedgerLines.Single(line => line.LedgerLineId == scenario.PaymentLineId);
        paymentLine.Amount = 200m;
        scenario.Invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Context.CreateManager().UpdateInvoiceAsync(scenario.Invoice));
        Assert.Contains("cannot be changed on the invoice after deposit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateInvoice_OmittingDepositedPaymentLine_Throws()
    {
        var scenario = await SetupDepositedPartialPaymentInvoiceAsync();
        scenario.Invoice.LedgerLines = scenario.Invoice.LedgerLines
            .Where(line => line.LedgerLineId != scenario.PaymentLineId)
            .ToList();
        scenario.Invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Context.CreateManager().UpdateInvoiceAsync(scenario.Invoice));
        Assert.Contains("must remain on the invoice", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateInvoice_DuplicateDepositedPaymentLine_Throws()
    {
        var scenario = await SetupDepositedPartialPaymentInvoiceAsync();
        var depositedLine = scenario.Invoice.LedgerLines.Single(line => line.LedgerLineId == scenario.PaymentLineId);
        scenario.Invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.Empty,
            InvoiceId = scenario.Invoice.InvoiceId,
            ReservationId = scenario.Invoice.ReservationId,
            CostCodeId = depositedLine.CostCodeId,
            Amount = depositedLine.Amount,
            Description = "Duplicate payment",
            LedgerLineDate = depositedLine.LedgerLineDate
        });
        scenario.Invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Context.CreateManager().UpdateInvoiceAsync(scenario.Invoice));
        Assert.Contains("duplicate an already-deposited payment", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<DepositedPartialPaymentScenario> SetupDepositedPartialPaymentInvoiceAsync()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 14),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            departureFee: 470.91m);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 14));
        var paymentLine = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, 193.15m, new DateOnly(2026, 9, 2), "ACH 09/01-10/01");
        var paymentId = Guid.NewGuid();
        var depositId = Guid.NewGuid();
        context.TrackPayment(new Payment
        {
            PaymentId = paymentId,
            OrganizationId = invoice.OrganizationId,
            OfficeId = invoice.OfficeId,
            PaymentCode = "PY-000001",
            PaymentDate = paymentLine.LedgerLineDate,
            Amount = paymentLine.Amount,
            CostCodeId = paymentLine.CostCodeId,
            Description = paymentLine.Description,
            PaymentKindId = (int)PaymentKind.Invoice,
            DepositId = depositId,
            DepositCode = "DP-000001",
            IsActive = true
        });
        context.TrackDeposit(new Deposit
        {
            DepositId = depositId,
            DepositCode = "DP-000001",
            OrganizationId = invoice.OrganizationId,
            OfficeId = invoice.OfficeId,
            TransferId = Guid.NewGuid(),
            TransferCode = "TR-000001",
            IsActive = true
        });
        paymentLine.PaymentId = paymentId;
        invoice.LedgerLines.Add(paymentLine);
        invoice.PaidAmount = 193.15m;
        context.TrackInvoice(invoice);

        return new DepositedPartialPaymentScenario(context, invoice, paymentLine.LedgerLineId, paymentId);
    }

    private sealed record DepositedPartialPaymentScenario(
        AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context,
        Invoice Invoice,
        Guid PaymentLineId,
        Guid PaymentId);
}
