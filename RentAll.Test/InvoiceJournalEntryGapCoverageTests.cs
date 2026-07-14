using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class InvoiceJournalEntryGapCoverageTests
{
    [Fact]
    public async Task DepartureFeeAndExtraFee_FirstMonthInvoice_JeBalancesAllChargeLines()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 7, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            hasPets: false,
            departureFee: 175m,
            extraFeeLines:
            [
                new ExtraFeeLine
                {
                    FeeDescription = "Admin Fee",
                    FeeAmount = 95m,
                    FeeFrequency = FrequencyType.OneTime,
                    CostCodeId = AccountingManagerJournalEntryFeeTestSupport.ExtraFeeCostCodeId
                }
            ]);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.Contains(invoice.LedgerLines, line => line.Description == "Departure Fee");
        Assert.Contains(invoice.LedgerLines, line => line.Description == "Admin Fee");
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task StandardPayment_OnInvoice_CreatesBalancedPaymentJe()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: 1500m,
            paymentDate: new DateOnly(2026, 4, 15));
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var paymentEntry = Assert.Single(context.ActiveJournalEntries, entry => entry.SourceTypeId == (int)SourceType.InvoicePayment);
        Assert.Equal(payment.LedgerLineId, paymentEntry.SourceId);
        AssertBalancedJournalEntry(paymentEntry);
        Assert.Equal(1500m, paymentEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId).Debit);
        Assert.Equal(1500m, paymentEntry.JournalEntryLines.Single(line => AccountingManagerJournalEntryTestSupport.IsAccountsReceivableMemo(line.Memo)).Credit);
    }

    [Fact]
    public async Task PrePayment_BeforeAccountingPeriod_CreatesReceivedAndApplyJes()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 2, 1);
        var periodEnd = new DateOnly(2026, 2, 28);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: 800m,
            paymentDate: new DateOnly(2026, 1, 25));
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        // Pre-payments produce three journal entries: the standard payment JE that keeps the cash in
        // Undeposited Funds, the "received" JE that moves it into the Pre-Payment liability on the
        // payment date, and the future-dated "apply" JE that reverses the liability on the accounting
        // period date. The standard payment and received entries share the InvoicePayment source, so
        // they are distinguished by the account they touch.
        var standardPaymentEntry = Assert.Single(context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
                && entry.SourceId == payment.LedgerLineId
                && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId));
        var receivedEntry = Assert.Single(context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
                && entry.SourceId == payment.LedgerLineId
                && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId));
        var applyEntry = Assert.Single(context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.Invoice && entry.SourceId == payment.LedgerLineId);

        AssertBalancedJournalEntry(standardPaymentEntry);
        AssertBalancedJournalEntry(receivedEntry);
        AssertBalancedJournalEntry(applyEntry);
        Assert.Equal(new DateOnly(2026, 1, 25), receivedEntry.TransactionDate);
        Assert.Equal(new DateOnly(2026, 2, 1), applyEntry.TransactionDate);
        Assert.Equal(800m, receivedEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId).Credit);
        Assert.Equal(800m, applyEntry.JournalEntryLines.Single(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId).Debit);
    }

    [Fact]
    public async Task UpdateInvoice_RefreshRecreatesSingleChargeJournalEntry()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, periodStart, periodEnd);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        var originalEntryId = context.ActiveJournalEntries.Single(entry => entry.SourceTypeId == (int)SourceType.Invoice).JournalEntryId;

        var rentalLine = invoice.LedgerLines.Single(line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Amount += 100m;
        invoice.TotalAmount += 100m;
        invoice.ModifiedBy = AccountingManagerJournalEntryTestSupport.CurrentUser;

        await manager.UpdateInvoiceAsync(invoice);

        var chargeEntries = context.ActiveJournalEntries.Where(entry => entry.SourceTypeId == (int)SourceType.Invoice).ToList();
        Assert.Single(chargeEntries);
        Assert.NotEqual(originalEntryId, chargeEntries[0].JournalEntryId);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(chargeEntries, invoice);
    }

    [Theory]
    [InlineData("2026-04-30", "2026-05-31", "2026-04-01", "2026-04-30", "Rental Fee (04/30-04/30)", 100)]
    [InlineData("2026-04-01", "2026-05-31", "2026-04-01", "2026-04-30", "Rental Fee (04/01-04/30)", 3000)]
    [InlineData("2026-04-15", "2026-07-31", "2026-04-01", "2026-04-30", "Rental Fee (04/15-04/30)", 1600)]
    [InlineData("2026-04-10", "2026-04-20", "2026-04-01", "2026-04-30", "Rental Fee (04/10-04/20)", 1100)]
    public async Task FocusedEdgeCases_ThroughJePipeline_BalanceInvoice(
        string arrival,
        string departure,
        string periodStart,
        string periodEnd,
        string expectedRentalDescription,
        decimal expectedRentalAmount)
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            DateOnly.Parse(arrival),
            DateOnly.Parse(departure),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var start = DateOnly.Parse(periodStart);
        var end = DateOnly.Parse(periodEnd);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(reservation, start, end);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var rental = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.Equal(expectedRentalDescription, rental.Description);
        Assert.Equal(expectedRentalAmount, rental.Amount);
        AccountingManagerJournalEntryTestSupport.AssertJournalEntriesBalanceInvoice(context.ActiveJournalEntries, invoice);
    }

    [Fact]
    public async Task ZeroTotalInvoice_CreatesJournalEntryWithoutActiveLines()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var ledgerLines = new List<LedgerLine>
        {
            new()
            {
                Description = "Rental Fee (04/01-04/30)",
                Amount = 0,
                CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId
            }
        };
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(reservation, periodStart, periodEnd, ledgerLines);
        invoice.TotalAmount = 0;
        context.TrackInvoice(invoice);
        var manager = context.CreateManager();

        var journalEntry = await manager.CreateJournalEntryFromInvoiceAsync(
            invoice,
            AccountingManagerJournalEntryTestSupport.CurrentUser);

        Assert.NotNull(journalEntry);
        Assert.NotEqual(Guid.Empty, journalEntry.JournalEntryId);
        Assert.Equal((int)SourceType.Invoice, journalEntry.SourceTypeId);
        Assert.Equal(invoice.InvoiceId, journalEntry.SourceId);
        Assert.DoesNotContain(journalEntry.JournalEntryLines, line => line.Debit != 0 || line.Credit != 0);
    }

    [Fact]
    public async Task NetZeroInvoice_WithOffsettingChargeLines_CreatesBalancedJournalEntry()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);

        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var invoice = AccountingManagerJournalEntryTestSupport.BuildInvoice(
            reservation,
            periodStart,
            periodEnd,
            [
                new LedgerLine { Description = "Rental Fee (04/01-04/30)", Amount = 100m, CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId },
                new LedgerLine { Description = "Rent Credit", Amount = -100m, CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId }
            ]);
        invoice.TotalAmount = 0;
        context.TrackInvoice(invoice);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var chargeEntry = Assert.Single(context.ActiveJournalEntries, entry => entry.SourceTypeId == (int)SourceType.Invoice);
        var totalDebit = chargeEntry.JournalEntryLines.Sum(line => line.Debit);
        var totalCredit = chargeEntry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(totalDebit, totalCredit);
        Assert.Equal(100m, totalDebit);
        Assert.Equal(3, chargeEntry.JournalEntryLines.Count);
    }

    [Fact]
    public async Task PrePayment_CreatesOwnerActualJournalEntryOnApplyOnly()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 4, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        var accountingPeriod = new DateOnly(2026, 2, 1);
        var paymentDate = new DateOnly(2026, 1, 25);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            accountingPeriod,
            new DateOnly(2026, 2, 28),
            enableOwnerShare: true);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, amount: 800m, paymentDate: paymentDate);
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActualEntries = context.ActiveJournalEntries
            .Where(entry => AccountingManager.MatchOwnerActualRentMemo(entry.Memo).IsMatch)
            .ToList();

        var ownerActualEntry = Assert.Single(ownerActualEntries);
        Assert.Equal(payment.LedgerLineId, ownerActualEntry.SourceId);
        Assert.Equal(new DateOnly(2026, 2, 1), ownerActualEntry.TransactionDate);
        AssertBalancedJournalEntry(ownerActualEntry);

        const decimal expectedOwnerActual = 640m;
        Assert.Equal(
            expectedOwnerActual,
            ownerActualEntry.JournalEntryLines
                .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
                .Sum(line => line.Credit));
    }

    [Fact]
    public async Task StandardPayment_CreatesOwnerActualJournalEntry()
    {
        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        var accountingPeriod = new DateOnly(2026, 4, 1);
        var paymentDate = new DateOnly(2026, 4, 15);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            accountingPeriod,
            new DateOnly(2026, 4, 30),
            enableOwnerShare: true);
        var manager = context.CreateManager();

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(invoice, amount: 1500m, paymentDate: paymentDate);
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActualEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerActualRentMemo(entry.Memo).IsMatch);

        Assert.Equal(payment.LedgerLineId, ownerActualEntry.SourceId);
        Assert.Equal(paymentDate, ownerActualEntry.TransactionDate);
        AssertBalancedJournalEntry(ownerActualEntry);

        const decimal expectedOwnerActual = 1200m;
        Assert.Equal(
            expectedOwnerActual,
            ownerActualEntry.JournalEntryLines
                .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
                .Sum(line => line.Credit));
    }

    [Fact]
    public async Task CrossMonthPayment_OnlyFutureAccountingPeriodSliceUsesPrePaymentPath()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 6, 21),
            new DateOnly(2026, 12, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.SDW,
            deposit: 60m,
            hasPets: false,
            departureFee: 500m,
            billingRate: 9000m);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30));
        invoice.AccountingPeriod = new DateOnly(2026, 7, 1);

        var rentalLine = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        Assert.Equal("Rental Fee (06/21-07/20)", rentalLine.Description);

        var manager = context.CreateManager();
        var paymentDate = new DateOnly(2026, 6, 23);
        var paymentAmount = 2640m;
        rentalLine.Amount = paymentAmount;
        invoice.LedgerLines = [rentalLine];
        invoice.TotalAmount = paymentAmount;
        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: paymentAmount,
            paymentDate: paymentDate,
            description: "ACH - 06/21-07/20, Dept");
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var juneChargeCap = Math.Round(paymentAmount * (10m / 30m), 2, MidpointRounding.AwayFromZero);
        var julyChargeCap = paymentAmount - juneChargeCap;
        var expectedFutureSlicePrePayment = Math.Min(paymentAmount - juneChargeCap, julyChargeCap);
        var prepaymentCredits = context.ActiveJournalEntries
            .Where(entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
                && entry.SourceId == payment.LedgerLineId
                && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId))
            .SelectMany(entry => entry.JournalEntryLines)
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.PrePaymentAccountId)
            .Sum(line => line.Credit);

        Assert.Equal(expectedFutureSlicePrePayment, prepaymentCredits);
        Assert.NotEqual(paymentAmount, prepaymentCredits);

        var paymentEntry = Assert.Single(context.ActiveJournalEntries,
            entry => entry.SourceTypeId == (int)SourceType.InvoicePayment
                && entry.SourceId == payment.LedgerLineId
                && entry.TransactionDate == paymentDate
                && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.UndepositedFundsAccountId
                    && line.Debit == paymentAmount));
        AssertBalancedJournalEntry(paymentEntry);
    }

    [Fact]
    public async Task OwnerActualPayment_SubtractsSecDepAndFeesBeforeApplyingOwnerShareToRent()
    {
        const decimal rentAmount = 4033.33m;
        const decimal securityDepositAmount = 3000m;
        const decimal feeAmount = 600m;
        const decimal invoiceTotal = rentAmount + securityDepositAmount + feeAmount;
        const decimal ownerSharePercent = 80m;
        var expectedOwnerRent = rentAmount * ownerSharePercent / 100m;

        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        var accountingPeriod = new DateOnly(2026, 6, 1);
        var paymentDate = new DateOnly(2026, 6, 1);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            accountingPeriod,
            new DateOnly(2026, 6, 30),
            enableOwnerShare: true);
        var manager = context.CreateManager();

        invoice.LedgerLines.Clear();
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 1,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Amount = rentAmount,
            Description = "Rental Fee (06/01-06/30)",
            LedgerLineDate = accountingPeriod
        });
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 2,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositCostCodeId,
            Amount = securityDepositAmount,
            Description = "Security Deposit",
            LedgerLineDate = accountingPeriod
        });
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 3,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.DepartureFeeCostCodeId,
            Amount = feeAmount,
            Description = "Departure Fee",
            LedgerLineDate = accountingPeriod
        });
        invoice.TotalAmount = invoiceTotal;

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: invoiceTotal,
            paymentDate: paymentDate);
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActualEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerActualRentMemo(entry.Memo).IsMatch);

        Assert.Equal(
            expectedOwnerRent,
            ownerActualEntry.JournalEntryLines
                .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
                .Sum(line => line.Credit));
    }

    [Fact]
    public async Task OwnerActualPayment_PartialPaymentPaysNonRentBeforeRent()
    {
        const decimal rentAmount = 4033.33m;
        const decimal securityDepositAmount = 3000m;
        const decimal feeAmount = 600m;
        const decimal invoiceTotal = rentAmount + securityDepositAmount + feeAmount;
        const decimal partialPaymentAmount = 5000m;
        const decimal ownerSharePercent = 80m;
        var rentPaymentAmount = partialPaymentAmount - securityDepositAmount - feeAmount;
        var expectedOwnerActual = rentPaymentAmount * ownerSharePercent / 100m;

        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            3000m);
        var accountingPeriod = new DateOnly(2026, 6, 1);
        var paymentDate = new DateOnly(2026, 6, 15);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            accountingPeriod,
            new DateOnly(2026, 6, 30),
            enableOwnerShare: true);
        var manager = context.CreateManager();

        invoice.LedgerLines.Clear();
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 1,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Amount = rentAmount,
            Description = "Rental Fee (06/01-06/30)",
            LedgerLineDate = accountingPeriod
        });
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 2,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositCostCodeId,
            Amount = securityDepositAmount,
            Description = "Security Deposit",
            LedgerLineDate = accountingPeriod
        });
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 3,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.DepartureFeeCostCodeId,
            Amount = feeAmount,
            Description = "Departure Fee",
            LedgerLineDate = accountingPeriod
        });
        invoice.TotalAmount = invoiceTotal;

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: partialPaymentAmount,
            paymentDate: paymentDate);
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActualEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerActualRentMemo(entry.Memo).IsMatch);

        Assert.Equal(
            expectedOwnerActual,
            ownerActualEntry.JournalEntryLines
                .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
                .Sum(line => line.Credit));
    }

    [Fact]
    public async Task CrossPeriodOwnerActual_OnFullPayment_MatchesOwnerExpectedForSlice()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 6, 26),
            new DateOnly(2026, 12, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            depositType: DepositType.CLR,
            hasPets: false,
            departureFee: 600m,
            billingRate: 3000m);
        var accountingPeriod = new DateOnly(2026, 6, 1);
        var paymentDate = new DateOnly(2026, 6, 1);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            accountingPeriod,
            new DateOnly(2026, 6, 30),
            enableOwnerShare: true);
        var manager = context.CreateManager();

        var rentalLine = Assert.Single(invoice.LedgerLines, line => line.Description.StartsWith("Rental Fee"));
        rentalLine.Description = "Rental Fee (06/26-07/15)";
        rentalLine.Amount = 5500m;
        invoice.TotalAmount = invoice.LedgerLines.Where(line => line.Amount != 0).Sum(line => line.Amount);

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerExpectedEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerExpectedRentMemo(entry.Memo).IsMatch
                && entry.TransactionDate == accountingPeriod);

        var expectedOwnerRent = ownerExpectedEntry.JournalEntryLines
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
            .Sum(line => line.Credit);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: invoice.TotalAmount,
            paymentDate: paymentDate);
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActualEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerActualRentMemo(entry.Memo).IsMatch
                && entry.TransactionDate == accountingPeriod);

        var actualOwnerRent = ownerActualEntry.JournalEntryLines
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
            .Sum(line => line.Credit);

        Assert.Equal(expectedOwnerRent, actualOwnerRent);
    }

    [Fact]
    public async Task Bar505_CrossSplitJuneSlice_OwnerActualMatchesOwnerExpected()
    {
        const decimal fullRentAmount = 5500m;
        const decimal securityDepositAmount = 3000m;
        const decimal feeAmount = 600m;
        const decimal fullInvoiceTotal = fullRentAmount + securityDepositAmount + feeAmount;
        const decimal ownerSharePercent = 70m;

        var reservation = AccountingManagerJournalEntryTestSupport.CreateReservation(
            new DateOnly(2026, 6, 9),
            new DateOnly(2026, 12, 31),
            ProrateType.SecondMonth,
            BillingType.Monthly,
            3000m);
        var accountingPeriod = new DateOnly(2026, 6, 1);
        var paymentDate = new DateOnly(2026, 3, 1);
        var (invoice, context) = await AccountingManagerJournalEntryFeeTestSupport.BuildTrackedFeeInvoiceAsync(
            reservation,
            accountingPeriod,
            new DateOnly(2026, 6, 30),
            enableOwnerShare: true,
            revenueSplitOwner: ownerSharePercent);
        var manager = context.CreateManager();

        invoice.LedgerLines.Clear();
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 1,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
            Amount = fullRentAmount,
            Description = "Rental Fee (06/09-07/08)",
            LedgerLineDate = accountingPeriod
        });
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 2,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositCostCodeId,
            Amount = securityDepositAmount,
            Description = "Security Deposit",
            LedgerLineDate = accountingPeriod
        });
        invoice.LedgerLines.Add(new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            LineNumber = 3,
            ReservationId = reservation.ReservationId,
            CostCodeId = AccountingManagerJournalEntryFeeTestSupport.DepartureFeeCostCodeId,
            Amount = feeAmount,
            Description = "Departure Fee",
            LedgerLineDate = accountingPeriod
        });
        invoice.TotalAmount = fullInvoiceTotal;

        await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerExpectedEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerExpectedRentMemo(entry.Memo).IsMatch
                && entry.TransactionDate == accountingPeriod);

        var expectedOwnerRent = ownerExpectedEntry.JournalEntryLines
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
            .Sum(line => line.Credit);

        var payment = AccountingManagerJournalEntryFeeTestSupport.CreatePaymentLedgerLine(
            invoice,
            amount: fullInvoiceTotal,
            paymentDate: paymentDate);
        invoice.LedgerLines.Add(payment);

        await manager.CreateJournalEntryFromPaymentAsync(invoice, payment, AccountingManagerJournalEntryTestSupport.CurrentUser);

        var ownerActualEntry = Assert.Single(
            context.ActiveJournalEntries,
            entry => AccountingManager.MatchOwnerActualRentMemo(entry.Memo).IsMatch
                && entry.TransactionDate == accountingPeriod);

        var actualOwnerRent = ownerActualEntry.JournalEntryLines
            .Where(line => line.ChartOfAccountId == AccountingManagerJournalEntryFeeTestSupport.OwnerAccountsPayableAccountId)
            .Sum(line => line.Credit);

        Assert.Equal(expectedOwnerRent, actualOwnerRent);
    }

    private static void AssertBalancedJournalEntry(JournalEntry journalEntry)
    {
        var totalDebit = journalEntry.JournalEntryLines.Sum(line => line.Debit);
        var totalCredit = journalEntry.JournalEntryLines.Sum(line => line.Credit);
        Assert.Equal(totalDebit, totalCredit);
    }
}
