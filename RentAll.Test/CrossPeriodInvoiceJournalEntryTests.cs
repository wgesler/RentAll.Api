using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class CrossPeriodInvoiceJournalEntryTests
{
    private static readonly DateOnly FebArrival = new(2026, 2, 4);
    private static readonly DateOnly FebMaidStart = new(2026, 2, 25);
    private static readonly DateOnly FebSliceStart = new(2026, 2, 1);
    private static readonly DateOnly FebSliceEnd = new(2026, 2, 28);
    private static readonly DateOnly MarSliceStart = new(2026, 3, 1);
    private static readonly DateOnly MarSliceEnd = new(2026, 3, 31);
    private static readonly DateOnly MarSliceEndForCrossMonthRent = new(2026, 3, 14);

    private static readonly DateOnly JanArrival = new(2026, 1, 15);
    private static readonly DateOnly JanMaidStart = new(2026, 1, 22);
    private static readonly DateOnly JanSliceStart = new(2026, 1, 15);
    private static readonly DateOnly JanSliceEnd = new(2026, 1, 31);
    private static readonly DateOnly FebShortSliceStart = new(2026, 2, 1);
    private static readonly DateOnly FebShortSliceEnd = new(2026, 2, 14);
    private static readonly DateOnly FebFullSliceEnd = new(2026, 2, 28);
    private static readonly DateOnly JanFebPeriodStart = new(2026, 1, 15);
    private static readonly DateOnly JanFebPeriodEnd = new(2026, 2, 14);

    private static readonly DateOnly JuneArrival = new(2026, 6, 21);
    private static readonly DateOnly JunePeriodStart = new(2026, 6, 1);
    private static readonly DateOnly JunePeriodEnd = new(2026, 6, 30);

    #region Journal entry pipeline

    [Fact]
    public async Task CrossMonthRentOnly_CreatesTwoBalancedJournalEntries()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            3000m);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);
        var rental = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (02/04-03/05)", rental.Description);
        Assert.Equal(3000m, rental.Amount);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.CreatedJournalEntries.Count);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.CreatedJournalEntries, invoice);

        var firstPeriodAr = context.CreatedJournalEntries[0].JournalEntryLines
            .Single(line => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(line.Memo))
            .Debit;
        var secondPeriodAr = context.CreatedJournalEntries[1].JournalEntryLines
            .Single(line => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(line.Memo))
            .Debit;

        Assert.Equal(2500m, firstPeriodAr);
        Assert.Equal(500m, secondPeriodAr);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.All(chargeEntries, entry => Assert.Equal(invoice.InvoiceId, entry.SourceId));
        Assert.Equal(new DateOnly(2026, 2, 1), chargeEntries[0].TransactionDate);
        Assert.Equal(new DateOnly(2026, 3, 1), chargeEntries[1].TransactionDate);
    }

    [Fact]
    public async Task CrossPeriodWithFees_FebInvoice_SplitsMaidServiceByOccurrenceMonth()
    {
        var reservation = CreateFebReservationWithFees();
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);

        AssertMaidServiceCount(invoice.LedgerLines, expectedTimes: 2, feePerVisit: 100m);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));
        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.Contains(chargeEntries[0].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Maid Service (1 times)") && line.Credit == 100m);
        Assert.Contains(chargeEntries[1].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Maid Service (1 times)") && line.Credit == 100m);
        Assert.Contains(chargeEntries[0].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Security Deposit") && line.Credit == 500m);
        Assert.Contains(chargeEntries[0].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Pet Fee") && line.Credit == 250m);
        Assert.DoesNotContain(chargeEntries[1].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Security Deposit"));
        Assert.DoesNotContain(chargeEntries[1].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Pet Fee"));
    }

    [Fact]
    public async Task CrossPeriodWithFees_MarFirstMonthInvoice_CreatesTwoJournalEntries()
    {
        var marArrival = new DateOnly(2026, 3, 18);
        var marMaidStart = new DateOnly(2026, 3, 18);

        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            marArrival,
            new DateOnly(2026, 10, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            marMaidStart,
            deposit: 3000m,
            petFee: 500m,
            departureFee: 350m,
            maidServiceFee: 100m,
            billingRate: 2500m);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, MarSliceStart, MarSliceEnd);

        var rental = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.StartsWith("Rental Fee (03/18-04/1", rental.Description);

        invoice.TotalAmount = invoice.LedgerLines.Sum(line => line.Amount);
        context.TrackInvoice(invoice);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));
        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.TransactionDate)
            .ToList();

        Assert.Contains(chargeEntries[0].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Security Deposit") && line.Credit == 3000m);
        Assert.Contains(chargeEntries[1].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemoPrefix(line.Memo, "Rental Fee (04/01-04/1"));
    }

    [Fact]
    public async Task CrossPeriodWithFees_JanFebInvoicePeriod_WhenSplitFails_LogsAndReturnsNullWithoutWritingEntries()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            JanArrival,
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            JanMaidStart);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, JanFebPeriodStart, JanFebPeriodEnd);

        var result = await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.NotNull(result);
        Assert.Equal(1, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
    }

    [Fact]
    public async Task CrossMonthRentOnly_BlankInvoicePeriod_CreatesTwoBalancedJournalEntries()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            3000m);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);
        invoice.InvoicePeriod = null;

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.CreatedJournalEntries.Count);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.CreatedJournalEntries, invoice);
    }

    [Fact]
    public async Task SdwCrossMonthRental_FebInvoice_CrossPeriodSplitBalancesRentAndWaiver()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            hasPets: false);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);

        Assert.Contains(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee (02/04-03/"));
        var originalSdw = Assert.Single(invoice.LedgerLines, line => line.Description == "Security Deposit Waiver");
        Assert.Equal(500m, originalSdw.Amount);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));

        var sdwCredits = context.ActiveJournalEntries
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.CostCodeId == AccountingManagerJournalEntryFeeTestSupport.SdwCostCodeId)
            .Sum(line => line.Credit);

        Assert.Equal(originalSdw.Amount, sdwCredits);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task MonthlyExtraFeeCrossMonthRental_FebInvoice_CrossPeriodSplitApportionsFeeWithRent()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            depositType: DepositType.CLR,
            extraFeeLines:
            [
                new ExtraFeeLine
                {
                    FeeDescription = "Parking Fee",
                    FeeAmount = 300m,
                    FeeFrequency = FrequencyType.Monthly,
                    CostCodeId = AccountingManagerJournalEntryFeeTestSupport.ExtraFeeCostCodeId
                }
            ]);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);
        var originalParking = Assert.Single(invoice.LedgerLines, line => line.Description == "Parking Fee");

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));
        Assert.Equal(AccountingManagerJournalEntryTestSupport.JournalEntryInvoicePath.CrossPeriodSplit,
            AccountingManagerJournalEntryTestSupport.ClassifyJournalEntryPath(invoice, context.CreatedJournalEntries.Count));

        var parkingCredits = context.ActiveJournalEntries
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Parking Fee"))
            .Sum(line => line.Credit);

        Assert.Equal(originalParking.Amount, parkingCredits);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task AdHocLine_WithMatchingRentalPeriod_IsApportionedWithRent()
    {
        var reservation = CreateFebReservationWithFees();
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);

        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = invoice.LedgerLines.Max(l => l.LineNumber) + 1,
            ReservationId = invoice.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.DepartureFeeCostCodeId,
            Description = "Manual Utility (02/04-03/05)",
            Amount = 300m,
            LedgerLineDate = new DateOnly(2026, 2, 10)
        });
        invoice.TotalAmount = invoice.LedgerLines.Sum(l => l.Amount);
        context.TrackInvoice(invoice);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.Equal(2, chargeEntries.Count);
        Assert.Contains(chargeEntries[0].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Manual Utility (02/04-03/05)") && line.Credit == 250m);
        Assert.Contains(chargeEntries[1].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Manual Utility (02/04-03/05)") && line.Credit == 50m);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Fact]
    public async Task AdHocLine_WithoutMatchingRentalPeriod_PostsAsOneTimeByLedgerLineDate()
    {
        var reservation = CreateFebReservationWithFees();
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);

        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = invoice.LedgerLines.Max(l => l.LineNumber) + 1,
            ReservationId = invoice.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.DepartureFeeCostCodeId,
            Description = "Manual Utility One Time",
            Amount = 120m,
            LedgerLineDate = new DateOnly(2026, 3, 3)
        });
        invoice.TotalAmount = invoice.LedgerLines.Sum(l => l.Amount);
        context.TrackInvoice(invoice);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.Equal(2, chargeEntries.Count);
        Assert.DoesNotContain(chargeEntries[0].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Manual Utility One Time"));
        Assert.Contains(chargeEntries[1].JournalEntryLines, line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Manual Utility One Time") && line.Credit == 120m);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Fact]
    public async Task TaxLikeLine_WithMatchingRentalPeriod_IsSplitAcrossBothPeriods()
    {
        var reservation = CreateFebReservationWithFees();
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);

        var nextLineNumber = invoice.LedgerLines.Any() ? invoice.LedgerLines.Max(l => l.LineNumber) + 1 : 1;
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = nextLineNumber,
            ReservationId = invoice.ReservationId,
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Description = "Taxes - 16.75% (02/04-03/05)",
            Amount = 408.70m,
            LedgerLineDate = new DateOnly(2026, 2, 4)
        });
        invoice.TotalAmount = invoice.LedgerLines.Sum(l => l.Amount);
        context.TrackInvoice(invoice);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.Equal(2, chargeEntries.Count);
        var taxCreditsByPeriod = chargeEntries
            .Select(entry => entry.JournalEntryLines
                .Where(line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Taxes - 16.75% (02/04-03/05)"))
                .Sum(line => line.Credit))
            .ToList();

        Assert.True(taxCreditsByPeriod[0] > 0m);
        Assert.True(taxCreditsByPeriod[1] > 0m);
        Assert.Equal(408.70m, taxCreditsByPeriod.Sum());
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Fact]
    public async Task AdHocRentCodeLine_WithoutDateRange_PostsToSingleSliceByLedgerDate()
    {
        var reservation = CreateFebReservationWithFees();
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);

        var nextLineNumber = invoice.LedgerLines.Any() ? invoice.LedgerLines.Max(l => l.LineNumber) + 1 : 1;
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = nextLineNumber,
            ReservationId = invoice.ReservationId,
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Description = "Airport Pick UP",
            Amount = 100m,
            LedgerLineDate = new DateOnly(2026, 2, 20)
        });
        invoice.TotalAmount = invoice.LedgerLines.Sum(l => l.Amount);
        context.TrackInvoice(invoice);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntries = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(entry => entry.PostingDate)
            .ToList();

        Assert.Equal(2, chargeEntries.Count);
        var firstSliceAirport = chargeEntries[0].JournalEntryLines
            .Where(line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Airport Pick UP"))
            .Sum(line => line.Credit);
        var secondSliceAirport = chargeEntries[1].JournalEntryLines
            .Where(line => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(line.Memo, "Airport Pick UP"))
            .Sum(line => line.Credit);

        Assert.Equal(100m, firstSliceAirport);
        Assert.Equal(0m, secondSliceAirport);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Fact]
    public async Task ApportionmentRounding_NightlyCrossMonthSplit_RentCreditsSumExactlyToOriginal()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 4, 5),
            new DateOnly(2026, 6, 15),
            ProrateType.SecondMonth,
            BillingType.Nightly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            depositType: DepositType.CLR);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, periodStart, periodEnd);
        var originalRental = Assert.Single(invoice.LedgerLines, line => line.Description == "Rental Fee (04/05-05/04)");

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(entry => entry.SourceTypeId == (int)SourceType.Invoice));

        var rentalCredits = context.ActiveJournalEntries
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryTestSupport.TenantIncomeAccountId
                && AccountingManagerJournalEntryTestSupport.IsRentalFeeChargeMemo(line.Memo))
            .Sum(line => line.Credit);

        Assert.Equal(originalRental.Amount, rentalCredits);
        Assert.Equal(originalRental.Amount,
            context.ActiveJournalEntries.Sum(entry => entry.JournalEntryLines
                .Where(line => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(line.Memo))
                .Sum(line => line.Debit)));
    }

    [Fact]
    public async Task UpdateInvoice_CrossMonthRentOnly_RefreshRecreatesTwoChargeJournalEntries()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            3000m);

        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, FebSliceStart, FebSliceEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var originalEntryIds = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.Invoice)
            .Select(entry => entry.JournalEntryId)
            .ToHashSet();

        Assert.Equal(2, originalEntryIds.Count);

        var rentalLine = invoice.LedgerLines.Single(line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Amount += 30m;
        invoice.TotalAmount += 30m;
        invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        await manager.UpdateInvoiceAsync(invoice);

        var chargeEntries = context.ActiveJournalEntries.Where(entry => entry.SourceTypeId == (int)SourceType.Invoice).ToList();
        Assert.Equal(2, chargeEntries.Count);
        Assert.DoesNotContain(chargeEntries.Select(entry => entry.JournalEntryId), id => originalEntryIds.Contains(id));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Fact]
    public async Task June21ToJuly20_AmortizesMonthlyChargesAndKeepsDepartureOnFirstMonth()
    {
        var reservation = CreateJuneReservation();
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation, JunePeriodStart, JunePeriodEnd);

        await context.CreateManager().CreateJournalEntryFromInvoiceAsync(
            invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Equal(2, context.ActiveJournalEntries.Count(e => e.SourceTypeId == (int)SourceType.Invoice));
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);

        var chargeEntries = context.ActiveJournalEntries
            .Where(e => e.SourceTypeId == (int)SourceType.Invoice)
            .OrderBy(e => e.PostingDate)
            .ToList();

        var firstMonthTotal = chargeEntries[0].JournalEntryLines
            .Where(l => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(l.Memo))
            .Sum(l => l.Debit);
        var secondMonthTotal = chargeEntries[1].JournalEntryLines
            .Where(l => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(l.Memo))
            .Sum(l => l.Debit);

        Assert.Equal(3560m, firstMonthTotal);
        Assert.Equal(6000m, secondMonthTotal);

        var firstMonthIncome = chargeEntries[0].JournalEntryLines
            .Where(l => l.Credit > 0)
            .GroupBy(l => l.Memo ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Credit), StringComparer.Ordinal);

        Assert.Equal(3000m, firstMonthIncome.Single(kvp => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(kvp.Key, "Rental Fee (06/21-06/30)")).Value);
        // Security Deposit Waiver is a deposit-type charge, so the full amount stays on the first
        // accounting period (like the Security Deposit) instead of being split across periods.
        Assert.Equal(60m, firstMonthIncome.Single(kvp => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(kvp.Key, "Security Deposit Waiver")).Value);
        Assert.Equal(500m, firstMonthIncome.Single(kvp => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(kvp.Key, "Departure Fee")).Value);

        var secondMonthIncome = chargeEntries[1].JournalEntryLines
            .Where(l => l.Credit > 0)
            .GroupBy(l => l.Memo ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Credit), StringComparer.Ordinal);

        Assert.Equal(6000m, secondMonthIncome.Single(kvp => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(kvp.Key, "Rental Fee (07/01-07/20)")).Value);
        Assert.DoesNotContain(secondMonthIncome, kvp => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(kvp.Key, "Security Deposit Waiver"));
        Assert.DoesNotContain(secondMonthIncome, kvp => AccountingManagerJournalEntryTestSupport.MatchesChargeLineMemo(kvp.Key, "Departure Fee"));
    }

    #endregion

    #region Ledger line slice behavior

    [Fact]
    public void CrossPeriodSplit_SecondMonthProrate_FebInvoiceWithCrossMonthRent_TotalsDoNotReconcile()
    {
        var reservation = CreateFebReservationWithFees();

        AssertCrossPeriodSplitDoesNotReconcile(
            reservation,
            originalStart: FebSliceStart,
            originalEnd: FebSliceEnd,
            slice1Start: FebSliceStart,
            slice1End: FebSliceEnd,
            slice2Start: MarSliceStart,
            slice2End: MarSliceEndForCrossMonthRent);
    }

    [Fact]
    public void CrossPeriodSplit_InvoicePeriodSpansTwoMonths_TotalsDoNotReconcileDueToFirstMonthFees()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            JanArrival,
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            JanMaidStart);

        AssertCrossPeriodSplitDoesNotReconcile(
            reservation,
            originalStart: JanSliceStart,
            originalEnd: FebShortSliceEnd,
            slice1Start: JanSliceStart,
            slice1End: JanSliceEnd,
            slice2Start: FebShortSliceStart,
            slice2End: FebShortSliceEnd);
    }

    [Fact]
    public void CrossPeriodSplit_SecondMonthProrate_FirstMonthFeesAndMaid_OnlyOnFirstSlice()
    {
        var reservation = CreateFebReservationWithFees();
        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var slice1Lines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);
        var slice2Lines = GetLines(manager, reservation, MarSliceStart, MarSliceEnd);

        AssertLineAmount(slice1Lines, "Security Deposit", 500m);
        AssertLineAmount(slice1Lines, "Pet Fee", 250m);
        AssertMaidServiceCount(slice1Lines, expectedTimes: 2, feePerVisit: 100m);

        AssertDoesNotContainDescription(slice2Lines, "Security Deposit");
        AssertDoesNotContainDescription(slice2Lines, "Pet Fee");
        AssertMaidServiceCountInRange(slice2Lines, minTimes: 3, maxTimes: 4, feePerVisit: 100m);
    }

    [Fact]
    public void CrossPeriodSplit_InvoicePeriodSpansTwoMonths_FirstMonthFeesOnlyOnJanuarySlice()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            JanArrival,
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            JanMaidStart);

        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var slice1Lines = GetLines(manager, reservation, JanSliceStart, JanSliceEnd);
        var slice2Lines = GetLines(manager, reservation, FebShortSliceStart, FebFullSliceEnd);

        AssertLineAmount(slice1Lines, "Security Deposit", 500m);
        AssertLineAmount(slice1Lines, "Pet Fee", 250m);
        AssertMaidServiceCount(slice1Lines, expectedTimes: 2, feePerVisit: 100m);

        AssertDoesNotContainDescription(slice2Lines, "Security Deposit");
        AssertDoesNotContainDescription(slice2Lines, "Pet Fee");
        AssertMaidServiceCountInRange(slice2Lines, minTimes: 3, maxTimes: 4, feePerVisit: 100m);
    }

    [Fact]
    public void CrossPeriodSplit_SecondMonthProrate_OriginalInvoiceIncludesDepositPetAndMaid()
    {
        var reservation = CreateFebReservationWithFees();
        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var originalLines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);

        AssertLineAmount(originalLines, "Security Deposit", 500m);
        AssertLineAmount(originalLines, "Pet Fee", 250m);
        AssertMaidServiceCount(originalLines, expectedTimes: 2, feePerVisit: 100m);
        Assert.Contains(originalLines, line => line.Description.StartsWith("Rental Fee (02/04-03/"));
    }

    [Fact]
    public void OriginalInvoice_IncludesCrossMonthRentSdwAndDepartureFee()
    {
        var reservation = CreateJuneReservation();
        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var lines = GetLines(manager, reservation, JunePeriodStart, JunePeriodEnd);

        AssertLineAmount(lines, "Rental Fee (06/21-07/20)", 9000m);
        AssertLineAmount(lines, "Security Deposit Waiver", 60m);
        AssertLineAmount(lines, "Departure Fee", 500m);
        Assert.Equal(9560m, lines.Sum(l => l.Amount));
    }

    #endregion

    #region Apportionment math

    [Fact]
    public void CrossPeriodSplit_AportionedSdw_Feb4ToMar5_SplitsBySameDailyRateAsRent()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            hasPets: false);

        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var originalLines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);
        var originalRental = Assert.Single(originalLines, line => line.Description == "Rental Fee (02/04-03/05)");
        var originalSdw = Assert.Single(originalLines, line => line.Description == "Security Deposit Waiver");

        var (firstMonthRent, secondMonthRent) = ApportionCrossMonthRental(originalRental, reservation);
        var (firstMonthSdw, secondMonthSdw) = ApportionCrossMonthFee(originalSdw, originalRental, reservation);

        Assert.Equal(2500m, firstMonthRent);
        Assert.Equal(500m, secondMonthRent);
        Assert.Equal(originalSdw.Amount * firstMonthRent / originalRental.Amount, firstMonthSdw);
        Assert.Equal(originalSdw.Amount * secondMonthRent / originalRental.Amount, secondMonthSdw);
        Assert.Equal(originalSdw.Amount, firstMonthSdw + secondMonthSdw);
    }

    [Fact]
    public void CrossPeriodSplit_AportionedRental_Feb4ToMar5_SplitsByDailyRate()
    {
        var reservation = CreateFebReservationWithFees();
        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var originalLines = GetLines(manager, reservation, FebSliceStart, FebSliceEnd);
        var originalRental = Assert.Single(originalLines, line => line.Description == "Rental Fee (02/04-03/05)");
        Assert.Equal(3000m, originalRental.Amount);

        var (firstMonthRent, secondMonthRent) = ApportionCrossMonthRental(originalRental, reservation);

        Assert.Equal(2500m, firstMonthRent);
        Assert.Equal(500m, secondMonthRent);
        Assert.Equal(originalRental.Amount, firstMonthRent + secondMonthRent);
    }

    [Fact]
    public void PooledMonthlyRecurring_June21ToJuly20_Amortizes9060OverThirtyDays()
    {
        var reservation = CreateJuneReservation();
        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();
        var originalLines = GetLines(manager, reservation, JunePeriodStart, JunePeriodEnd);

        var rental = Assert.Single(originalLines, l => l.Description == "Rental Fee (06/21-07/20)");
        var sdw = Assert.Single(originalLines, l => l.Description == "Security Deposit Waiver");
        var monthlyTotal = rental.Amount + sdw.Amount;

        Assert.Equal(9060m, monthlyTotal);

        const int totalDays = 30;
        const int juneDays = 10;
        const int julyDays = 20;

        Assert.Equal(3020m, monthlyTotal / totalDays * juneDays);
        Assert.Equal(6040m, monthlyTotal / totalDays * julyDays);
        Assert.Equal(3520m, monthlyTotal / totalDays * juneDays + 500m);
    }

    #endregion

    private static Reservation CreateFebReservationWithFees()
        => AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            FebArrival,
            new DateOnly(2026, 3, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            FebMaidStart);

    private static Reservation CreateJuneReservation()
        => AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            JuneArrival,
            new DateOnly(2026, 12, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            deposit: 60m,
            hasPets: false,
            departureFee: 500m,
            billingRate: 9000m);

    private static (decimal FirstMonthRent, decimal SecondMonthRent) ApportionCrossMonthRental(
        LedgerLine originalRental,
        Reservation reservation)
    {
        const int referenceYear = 2026;
        var match = System.Text.RegularExpressions.Regex.Match(
            originalRental.Description,
            @"^Rental Fee \((?<start>\d{2}/\d{2})-(?<end>\d{2}/\d{2})\)$");
        Assert.True(match.Success);

        var rentalStart = ParseMonthDay(match.Groups["start"].Value, referenceYear);
        var rentalEnd = ParseMonthDay(match.Groups["end"].Value, referenceYear);
        if (rentalEnd < rentalStart)
            rentalEnd = rentalEnd.AddYears(1);

        var departureDate = reservation.DepartureDate;
        var totalDays = CalculateBillingDays(
            rentalStart,
            rentalEnd,
            reservation.BillingType,
            IsDepartureMonthYear(rentalEnd, departureDate),
            IsLastDayOfMonth(rentalEnd));

        var firstMonthEnd = new DateOnly(rentalStart.Year, rentalStart.Month, DateTime.DaysInMonth(rentalStart.Year, rentalStart.Month));
        var firstPeriodEnd = rentalEnd < firstMonthEnd ? rentalEnd : firstMonthEnd;

        var firstDays = CalculateBillingDays(
            rentalStart,
            firstPeriodEnd,
            reservation.BillingType,
            IsDepartureMonthYear(firstPeriodEnd, departureDate),
            IsLastDayOfMonth(firstPeriodEnd));

        var dailyRate = originalRental.Amount / totalDays;
        var firstMonthRent = dailyRate * firstDays;
        var secondMonthRent = originalRental.Amount - firstMonthRent;
        return (firstMonthRent, secondMonthRent);
    }

    private static (decimal FirstMonthFee, decimal SecondMonthFee) ApportionCrossMonthFee(
        LedgerLine originalFee,
        LedgerLine originalRental,
        Reservation reservation)
    {
        var (firstMonthRent, _) = ApportionCrossMonthRental(originalRental, reservation);
        var firstMonthFee = originalFee.Amount * firstMonthRent / originalRental.Amount;
        var secondMonthFee = originalFee.Amount - firstMonthFee;
        return (firstMonthFee, secondMonthFee);
    }

    private static DateOnly ParseMonthDay(string monthDay, int referenceYear)
    {
        var parts = monthDay.Split('/');
        return new DateOnly(referenceYear, int.Parse(parts[0]), int.Parse(parts[1]));
    }

    private static bool IsDepartureMonthYear(DateOnly date, DateOnly departureDate)
        => date.Year == departureDate.Year && date.Month == departureDate.Month;

    private static bool IsLastDayOfMonth(DateOnly date)
        => date.Day == DateTime.DaysInMonth(date.Year, date.Month);

    private static int CalculateBillingDays(
        DateOnly startDate,
        DateOnly endDate,
        BillingType billingType,
        bool isDepartureMonthYear,
        bool isLastDayOfMonth)
    {
        if (endDate < startDate)
            return 0;
        if (endDate == startDate)
            return 1;

        var days = endDate.DayNumber - startDate.DayNumber;
        if (billingType != BillingType.Nightly ||
            (billingType == BillingType.Nightly && !isDepartureMonthYear && isLastDayOfMonth))
            days++;

        return days;
    }

    private static void AssertCrossPeriodSplitDoesNotReconcile(
        Reservation reservation,
        DateOnly originalStart,
        DateOnly originalEnd,
        DateOnly slice1Start,
        DateOnly slice1End,
        DateOnly slice2Start,
        DateOnly slice2End)
    {
        var manager = AccountingManagerJournalEntryTestSupport.CreateLedgerLineManager();

        var originalLines = GetLines(manager, reservation, originalStart, originalEnd);
        var slice1Lines = GetLines(manager, reservation, slice1Start, slice1End);
        var slice2Lines = GetLines(manager, reservation, slice2Start, slice2End);

        var originalTotal = originalLines.Sum(l => l.Amount);
        var splitTotal = slice1Lines.Sum(l => l.Amount) + slice2Lines.Sum(l => l.Amount);

        Assert.Contains(originalLines, line => line.Description == "Security Deposit");
        Assert.Contains(originalLines, line => line.Description == "Pet Fee");
        Assert.Contains(originalLines, line => line.Description.StartsWith("Maid Service"));

        Assert.NotEqual(originalTotal, splitTotal);
        Assert.True(splitTotal > originalTotal);
    }

    private static List<LedgerLine> GetLines(AccountingManager manager, Reservation reservation, DateOnly start, DateOnly end)
        => manager.GetLedgerLinesByReservationIdAsync(reservation, start, end, rentalCostCodeId: 77);

    private static void AssertLineAmount(IEnumerable<LedgerLine> lines, string description, decimal expectedAmount)
    {
        var line = Assert.Single(lines, l => l.Description == description);
        Assert.Equal(expectedAmount, line.Amount);
    }

    private static void AssertDoesNotContainDescription(IEnumerable<LedgerLine> lines, string description)
        => Assert.DoesNotContain(lines, line => line.Description == description);

    private static void AssertMaidServiceCount(IEnumerable<LedgerLine> lines, int expectedTimes, decimal feePerVisit)
    {
        var maidLine = Assert.Single(lines, line => line.Description.StartsWith("Maid Service"));
        Assert.Equal($"Maid Service ({expectedTimes} times)", maidLine.Description);
        Assert.Equal(expectedTimes * feePerVisit, maidLine.Amount);
    }

    private static void AssertMaidServiceCountInRange(
        IEnumerable<LedgerLine> lines,
        int minTimes,
        int maxTimes,
        decimal feePerVisit)
    {
        var maidLine = Assert.Single(lines, line => line.Description.StartsWith("Maid Service"));
        var times = ExtractMaidServiceTimes(maidLine.Description);
        Assert.InRange(times, minTimes, maxTimes);
        Assert.Equal(times * feePerVisit, maidLine.Amount);
    }

    private static int ExtractMaidServiceTimes(string description)
    {
        var open = description.IndexOf('(');
        var close = description.IndexOf(" times)", StringComparison.Ordinal);
        if (open < 0 || close <= open)
            throw new InvalidOperationException($"Unexpected maid service description: {description}");

        return int.Parse(description.Substring(open + 1, close - open - 1));
    }
}
