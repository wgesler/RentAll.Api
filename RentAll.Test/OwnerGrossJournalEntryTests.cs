using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class OwnerGrossJournalEntryTests
{
    [Fact]
    public async Task OwnerExpected_PostsFullRentAndMovesFeeToOwnerIncome()
    {
        var (invoice, context, rent) = await CreateGrossRentInvoiceAsync();
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerExpected = Assert.Single(
            context.ActiveJournalEntries,
            entry => entry.JournalEntryKindId == JournalEntryKind.OwnerExpected);
        AssertGrossOwnerLines(ownerExpected, rent, OwnerShare(rent));
    }

    [Fact]
    public async Task OwnerActual_FullRentPayment_MatchesOwnerExpected()
    {
        var (invoice, context, rent) = await CreateGrossRentInvoiceAsync();
        var manager = context.CreateManager();
        var paymentDate = new DateOnly(2026, 4, 15);

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, rent, paymentDate);
        invoice.LedgerLines.Add(payment);
        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActual = Assert.Single(
            context.ActiveJournalEntries,
            entry => entry.JournalEntryKindId == JournalEntryKind.OwnerActual);
        Assert.Equal(paymentDate, ownerActual.TransactionDate);
        AssertGrossOwnerLines(ownerActual, rent, OwnerShare(rent));
    }

    [Fact]
    public async Task OwnerActual_PartialPayments_AddUpToFullRentAndFee()
    {
        var (invoice, context, rent) = await CreateGrossRentInvoiceAsync();
        var manager = context.CreateManager();
        var firstAmount = Math.Round(rent / 2m, 2, MidpointRounding.AwayFromZero);
        var secondAmount = rent - firstAmount;

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var firstPayment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, firstAmount, new DateOnly(2026, 4, 10), "Check #1001");
        invoice.LedgerLines.Add(firstPayment);
        await manager.CreateJournalEntryFromPaymentAsync(invoice, firstPayment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var secondPayment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, secondAmount, new DateOnly(2026, 4, 20), "Check #1002");
        invoice.LedgerLines.Add(secondPayment);
        await manager.CreateJournalEntryFromPaymentAsync(invoice, secondPayment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActuals = context.ActiveJournalEntries
            .Where(entry => entry.JournalEntryKindId == JournalEntryKind.OwnerActual)
            .OrderBy(entry => entry.TransactionDate)
            .ToList();
        Assert.Equal(2, ownerActuals.Count);
        AssertGrossOwnerLines(ownerActuals[0], firstAmount, OwnerShare(firstAmount));
        AssertGrossOwnerLines(ownerActuals[1], secondAmount, OwnerShare(rent) - OwnerShare(firstAmount));

        var ownerPayableNet = ownerActuals.Sum(entry =>
            Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId, line => line.Credit - line.Debit));
        var remainder = ownerActuals.Sum(entry =>
            Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerRentExpenseAccountId, line => line.Debit));
        Assert.Equal(OwnerShare(rent), ownerPayableNet);
        Assert.Equal(OwnerShare(rent), remainder);
    }

    [Fact]
    public async Task PrePayment_OwnerActual_PostsCollectedRentAndFee()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            enableOwnerShare: true,
            revenueSplitOwner: 70m,
            irsMethodType: IRSMethodType.IrsGrossMethod);
        var manager = context.CreateManager();
        const decimal collectedRent = 800m;

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, collectedRent, new DateOnly(2026, 3, 20));
        invoice.LedgerLines.Add(payment);
        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerExpected = Assert.Single(
            context.ActiveJournalEntries,
            entry => entry.JournalEntryKindId == JournalEntryKind.OwnerExpected);
        var ownerActual = Assert.Single(
            context.ActiveJournalEntries,
            entry => entry.JournalEntryKindId == JournalEntryKind.OwnerActual);
        AssertGrossOwnerLines(ownerExpected, 3000m, 2100m);
        AssertGrossOwnerLines(ownerActual, collectedRent, OwnerShare(collectedRent));
    }

    private static async Task<(Invoice Invoice, AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context, decimal Rent)> CreateGrossRentInvoiceAsync()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            enableOwnerShare: true,
            revenueSplitOwner: 70m,
            irsMethodType: IRSMethodType.IrsGrossMethod);
        var rent = invoice.LedgerLines
            .Where(line => line.CostCodeId == AccountingManagerJournalEntryTestSupport.RentalCostCodeId)
            .Sum(line => line.Amount);
        Assert.Equal(3000m, rent);
        return (invoice, context, rent);
    }

    private static decimal OwnerShare(decimal rent)
        => Math.Round(rent * 70m / 100m, 2, MidpointRounding.AwayFromZero);

    private static void AssertGrossOwnerLines(JournalEntry entry, decimal rent, decimal ownerShare)
    {
        var fee = Math.Round(rent - ownerShare, 2, MidpointRounding.AwayFromZero);
        var debits = entry.JournalEntryLines.Sum(line => line.Debit);
        var credits = entry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(debits, credits);
        Assert.Equal(rent, Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId, line => line.Credit));
        Assert.Equal(fee, Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId, line => line.Debit));
        Assert.Equal(ownerShare, Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerRentExpenseAccountId, line => line.Debit));
        Assert.Equal(0m, Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerIncomeAccountId, line => line.Debit + line.Credit));
        Assert.Equal(ownerShare, Sum(entry, AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId, line => line.Credit - line.Debit));
        Assert.All(
            entry.JournalEntryLines.Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId),
            line => Assert.Equal(AccountingManagerJournalEntryFeeTestSupport.OwnerContactId, line.ContactId));
    }

    private static decimal Sum(JournalEntry entry, int accountId, Func<JournalEntryLine, decimal> amount)
        => entry.JournalEntryLines.Where(line => line.ChartOfAccountId == accountId).Sum(amount);
}
