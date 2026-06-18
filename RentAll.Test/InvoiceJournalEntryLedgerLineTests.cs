using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class InvoiceJournalEntryLedgerLineTests
{
    [Theory]
    [MemberData(nameof(JournalEntryLedgerLineScenarioMatrix))]
    public async Task CreateJournalEntryFromInvoice_LedgerLineMatrix_JournalEntriesBalanceInvoice(
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
    [MemberData(nameof(JournalEntryLeapYearScenarioMatrix))]
    public async Task CreateJournalEntryFromInvoice_LeapYearMatrix_JournalEntriesBalanceInvoice(
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
    [MemberData(nameof(JournalEntryCrossYearScenarioMatrix))]
    public async Task CreateJournalEntryFromInvoice_CrossYearMatrix_JournalEntriesBalanceInvoice(
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

    public static IEnumerable<object[]> JournalEntryLedgerLineScenarioMatrix() =>
        ProjectJournalEntryScenarioMatrix(AccountingManagerLedgerLineTests.LedgerLineScenarioMatrix());

    public static IEnumerable<object[]> JournalEntryLeapYearScenarioMatrix() =>
        ProjectJournalEntryScenarioMatrix(AccountingManagerLedgerLineTests.LeapYearScenarioMatrix());

    public static IEnumerable<object[]> JournalEntryCrossYearScenarioMatrix() =>
        ProjectJournalEntryScenarioMatrix(AccountingManagerLedgerLineTests.CrossYearScenarioMatrix());

    private static IEnumerable<object[]> ProjectJournalEntryScenarioMatrix(IEnumerable<object[]> source) =>
        source.Select(row => new object[]
        {
            row[0],
            row[1],
            row[2],
            row[3],
            row[4],
            row[5],
            row[6],
            row[7],
            row[9]
        });
}
