using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class InvoiceJournalEntryLedgerLineTests
{
    [Theory]
    [MemberData(
        nameof(AccountingManagerLedgerLineTests.LedgerLineScenarioMatrix),
        MemberType = typeof(AccountingManagerLedgerLineTests))]
    public async Task CreateJournalEntryFromInvoice_LedgerLineMatrix_JournalEntriesBalanceInvoice(
        string caseId,
        ProrateType prorateType,
        BillingType billingType,
        DateOnly arrival,
        DateOnly departure,
        DateOnly startDate,
        DateOnly endDate,
        string expectedDescription,
        int _expectedDays,
        decimal expectedAmount,
        int _expectedCostCodeId)
    {
        await AssertJournalEntriesBalanceForScenarioAsync(
            "Ledger",
            caseId,
            prorateType,
            billingType,
            arrival,
            departure,
            startDate,
            endDate,
            expectedDescription,
            expectedAmount);
    }

    [Theory]
    [MemberData(
        nameof(AccountingManagerLedgerLineTests.LeapYearScenarioMatrix),
        MemberType = typeof(AccountingManagerLedgerLineTests))]
    public async Task CreateJournalEntryFromInvoice_LeapYearMatrix_JournalEntriesBalanceInvoice(
        string caseId,
        ProrateType prorateType,
        BillingType billingType,
        DateOnly arrival,
        DateOnly departure,
        DateOnly startDate,
        DateOnly endDate,
        string expectedDescription,
        int _expectedDays,
        decimal expectedAmount,
        int _expectedCostCodeId)
    {
        await AssertJournalEntriesBalanceForScenarioAsync(
            "LeapYear",
            caseId,
            prorateType,
            billingType,
            arrival,
            departure,
            startDate,
            endDate,
            expectedDescription,
            expectedAmount);
    }

    [Theory]
    [MemberData(
        nameof(AccountingManagerLedgerLineTests.CrossYearScenarioMatrix),
        MemberType = typeof(AccountingManagerLedgerLineTests))]
    public async Task CreateJournalEntryFromInvoice_CrossYearMatrix_JournalEntriesBalanceInvoice(
        string caseId,
        ProrateType prorateType,
        BillingType billingType,
        DateOnly arrival,
        DateOnly departure,
        DateOnly startDate,
        DateOnly endDate,
        string expectedDescription,
        int _expectedDays,
        decimal expectedAmount,
        int _expectedCostCodeId)
    {
        await AssertJournalEntriesBalanceForScenarioAsync(
            "CrossYear",
            caseId,
            prorateType,
            billingType,
            arrival,
            departure,
            startDate,
            endDate,
            expectedDescription,
            expectedAmount);
    }

    [Fact]
    public async Task CreateJournalEntryFromInvoice_CrossMonthRentOnly_CreatesTwoBalancedJournalEntries()
    {
        var arrival = new DateOnly(2026, 2, 4);
        var departure = new DateOnly(2026, 3, 31);
        var periodStart = new DateOnly(2026, 2, 1);
        var periodEnd = new DateOnly(2026, 2, 28);

        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            arrival,
            departure,
            ProrateType.SecondMonth,
            BillingType.Monthly,
            3000m);

        var ledgerManager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var ledgerLines = ledgerManager.GetLedgerLinesByReservationIdAsync(
            reservation,
            periodStart,
            periodEnd,
            AccountingManagerJournalEntryTestSupport.RentalCostCodeId);

        var rental = Assert.Single(ledgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (02/04-03/05)", rental.Description);
        Assert.Equal(3000m, rental.Amount);

        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(reservation, periodStart, periodEnd, ledgerLines);
        var testContext = AccountingManagerJournalEntryTestSupport.CreateJournalEntryTestContext(reservation);
        var journalEntryManager = testContext.CreateManager();

        await journalEntryManager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        AccountingManagerJournalEntryTestSupport.PrintJournalEntryPath(
            "Focused CrossMonthRentOnly",
            invoice,
            testContext.CreatedJournalEntries,
            rental.Description,
            rental.Amount);

        Assert.Equal(2, testContext.CreatedJournalEntries.Count);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(testContext.CreatedJournalEntries, invoice);

        var firstPeriodAr = testContext.CreatedJournalEntries[0].JournalEntryLines
            .Single(line => line.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
            .Debit;
        var secondPeriodAr = testContext.CreatedJournalEntries[1].JournalEntryLines
            .Single(line => line.Memo!.StartsWith("Accounts Receivable", StringComparison.Ordinal))
            .Debit;

        Assert.Equal(2500m, firstPeriodAr);
        Assert.Equal(500m, secondPeriodAr);
    }

    private static async Task AssertJournalEntriesBalanceForScenarioAsync(
        string matrixName,
        string caseId,
        ProrateType prorateType,
        BillingType billingType,
        DateOnly arrival,
        DateOnly departure,
        DateOnly startDate,
        DateOnly endDate,
        string expectedDescription,
        decimal expectedAmount)
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            arrival,
            departure,
            prorateType,
            billingType,
            AccountingManagerJournalEntryTestSupport.GetBillingRate(billingType));

        var ledgerManager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var ledgerLines = ledgerManager.GetLedgerLinesByReservationIdAsync(
            reservation,
            startDate,
            endDate,
            AccountingManagerJournalEntryTestSupport.RentalCostCodeId);

        var rental = Assert.Single(ledgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.Equal(expectedDescription, rental.Description);
        Assert.Equal(expectedAmount, rental.Amount);

        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(reservation, startDate, endDate, ledgerLines);
        var testContext = AccountingManagerJournalEntryTestSupport.CreateJournalEntryTestContext(reservation);
        var journalEntryManager = testContext.CreateManager();

        await journalEntryManager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        AccountingManagerJournalEntryTestSupport.PrintJournalEntryPath(
            $"{matrixName} Case {caseId}",
            invoice,
            testContext.CreatedJournalEntries,
            rental.Description,
            rental.Amount);

        Assert.NotEmpty(testContext.CreatedJournalEntries);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(
            testContext.CreatedJournalEntries,
            invoice,
            $"{matrixName}-{caseId}");
    }
}
