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
