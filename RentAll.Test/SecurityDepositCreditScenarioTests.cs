using System.Collections;
using System.Reflection;
using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;
using Xunit.Abstractions;

namespace RentAll.Test;

public class SecurityDepositCreditScenarioTests
{
    private const string ReservationCode = "R-000001";
    private const decimal Collected = 2100m;
    private const int ArAccountId = AccountingManagerJournalEntryTestSupport.AccountsReceivableAccountId;
    private const int IncomeAccountId = AccountingManagerJournalEntryTestSupport.TenantIncomeAccountId;
    private const int SdLiabilityAccountId = AccountingManagerJournalEntryFeeTestSupport.SecurityDepositLiabilityAccountId;

    private readonly ITestOutputHelper _output;

    public SecurityDepositCreditScenarioTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task ChargesScenario_CreditPaysOutstandingRent_RefundLeavesInvoiceSettled()
    {
        var (manager, context, costCodeById) = CreateManager();
        var sdInvoice = BuildSecurityDepositInvoice();
        var rentInvoice = BuildInvoice("R-000001-002", new DateOnly(2026, 10, 1), [ChargeLine(AccountingManagerJournalEntryTestSupport.RentalCostCodeId, 2077m, "Rent")]);
        var invoicesBefore = new List<Invoice> { sdInvoice, rentInvoice };

        var beforeOutstanding = SumAmounts(InvokeStatic("BuildOutstandingChargeLines", invoicesBefore, costCodeById, null));
        Assert.Equal(2077m, beforeOutstanding);

        var refundPaymentId = Guid.NewGuid();
        AddLine(rentInvoice, CreditLine(-Collected));
        AddLine(rentInvoice, RefundLine(-23m, refundPaymentId));
        var invoices = new List<Invoice> { sdInvoice, rentInvoice };
        var ignored = new HashSet<Guid> { refundPaymentId };

        var figures = CalculateFigures(invoices, costCodeById, ignored);
        var journalEntry = await CreateInvoiceJournalEntryAsync(manager, context, rentInvoice);

        Print("Charges + refund 23", rentInvoice, figures, journalEntry);

        Assert.Equal(-23m, rentInvoice.TotalAmount);
        Assert.Equal(-23m, rentInvoice.PaidAmount);
        Assert.Equal(0m, rentInvoice.TotalAmount - rentInvoice.PaidAmount);
        Assert.Equal(0m, figures.NetDueAfter);
        Assert.Equal(0m, figures.Owed);
        Assert.Equal(2077m, figures.ChargesCoveredByDeposit);
        Assert.Equal(2100m, figures.Deposit);
        Assert.Equal(2100m, figures.Collected);
        Assert.Equal(23m, figures.Balance);
        Assert.Equal(2077m, figures.ToBeTransferred);
        Assert.Equal(0m, figures.OwnerWaterfallSecurityDepositBase);
        Assert.Single(figures.SecurityDepositInvoiceCodes);

        AssertLine(journalEntry, ArAccountId, debit: 0m, credit: 23m);
        AssertLine(journalEntry, IncomeAccountId, debit: 0m, credit: 2077m);
        AssertLine(journalEntry, SdLiabilityAccountId, debit: 2100m, credit: 0m);
    }

    [Fact]
    public async Task NoChargesScenario_RefundInvoiceShowsNegativeTotalAndPaid()
    {
        var (manager, context, costCodeById) = CreateManager();
        var sdInvoice = BuildSecurityDepositInvoice();
        var refundPaymentId = Guid.NewGuid();
        var refundInvoice = BuildInvoice("R-000001-002", new DateOnly(2026, 11, 1), [CreditLine(-Collected)]);
        AddLine(refundInvoice, RefundLine(-Collected, refundPaymentId));
        var invoices = new List<Invoice> { sdInvoice, refundInvoice };

        var figures = CalculateFigures(invoices, costCodeById, new HashSet<Guid> { refundPaymentId });
        var journalEntry = await CreateInvoiceJournalEntryAsync(manager, context, refundInvoice);

        Print("No charges, full refund", refundInvoice, figures, journalEntry);

        Assert.Equal(-2100m, refundInvoice.TotalAmount);
        Assert.Equal(-2100m, refundInvoice.PaidAmount);
        Assert.Equal(0m, figures.Owed);
        Assert.Equal(0m, figures.ChargesCoveredByDeposit);
        Assert.Equal(2100m, figures.Deposit);
        Assert.Equal(2100m, figures.Collected);
        Assert.Equal(2100m, figures.Balance);
        Assert.Equal(0m, figures.ToBeTransferred);

        AssertLine(journalEntry, ArAccountId, debit: 0m, credit: 2100m);
        AssertLine(journalEntry, SdLiabilityAccountId, debit: 2100m, credit: 0m);
    }

    [Fact]
    public void ChargesScenario_PartialTenantPayment_CreditCoversRemainder()
    {
        var (_, _, costCodeById) = CreateManager();
        var sdInvoice = BuildSecurityDepositInvoice();
        var rentInvoice = BuildInvoice("R-000001-002", new DateOnly(2026, 10, 1), [ChargeLine(AccountingManagerJournalEntryTestSupport.RentalCostCodeId, 2500m, "Rent")]);
        AddLine(rentInvoice, PaymentLine(423m, Guid.NewGuid(), "Tenant payment"));
        var refundPaymentId = Guid.NewGuid();
        AddLine(rentInvoice, CreditLine(-Collected));
        AddLine(rentInvoice, RefundLine(-23m, refundPaymentId));

        var figures = CalculateFigures([sdInvoice, rentInvoice], costCodeById, new HashSet<Guid> { refundPaymentId });
        Print("Partial tenant payment 423 + charges 2500", rentInvoice, figures, null);

        Assert.Equal(0m, figures.NetDueAfter);
        Assert.Equal(0m, figures.Owed);
        Assert.Equal(2077m, figures.ChargesCoveredByDeposit);
        Assert.Equal(23m, figures.Balance);
    }

    private sealed record Figures(
        decimal NetDueAfter,
        decimal Owed,
        decimal ChargesCoveredByDeposit,
        decimal Deposit,
        decimal Collected,
        decimal Balance,
        decimal ToBeTransferred,
        decimal OwnerWaterfallSecurityDepositBase,
        List<string> SecurityDepositInvoiceCodes);

    private Figures CalculateFigures(List<Invoice> invoices, IReadOnlyDictionary<int, CostCode> costCodeById, IReadOnlySet<Guid> ignored)
    {
        var returnInvoice = invoices[^1];
        var sdInvoices = (List<Invoice>)InvokeStatic("FindSecurityDepositInvoicesForReservation", invoices, costCodeById)!;
        var deposit = sdInvoices.Sum(invoice => (decimal)InvokeStatic("CalculateSecurityDepositChargeAmount", invoice, costCodeById)!);
        var collected = sdInvoices.Sum(invoice =>
        {
            var allocation = InvokeStatic("CalculateSecurityDepositPaymentAllocation", invoice, costCodeById, ignored)!;
            return (decimal)allocation.GetType().GetProperty("PaidAmount")!.GetValue(allocation)!;
        });
        var chargesCovered = SumAmounts(InvokeStatic("BuildOutstandingChargeLines", invoices, costCodeById, ignored));
        var owed = SumAmounts(InvokeStatic("BuildOutstandingChargeLines", invoices, costCodeById, null));
        var netDueAfter = (decimal)InvokeStatic("CalculateInvoiceNonSecurityDepositNetDue", returnInvoice, costCodeById, null)!;
        var creditAmount = (decimal)InvokeStatic("CalculateSecurityDepositCreditAmount", invoices, costCodeById)!;
        var balance = Math.Max(0m, collected - chargesCovered);
        var toBeTransferred = Math.Max(0m, creditAmount - balance);
        var waterfallBase = returnInvoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line => (bool)InvokeStatic("IsRefundableSecurityDepositCharge", line, costCodeById)!)
            .Sum(line => line.Amount);

        return new Figures(netDueAfter, owed, chargesCovered, deposit, collected, balance, toBeTransferred, waterfallBase,
            sdInvoices.Select(invoice => invoice.InvoiceCode).ToList());
    }

    private static (AccountingManager Manager, AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext Context, IReadOnlyDictionary<int, CostCode> CostCodeById) CreateManager()
    {
        var reservation = AccountingManagerJournalEntryFeeTestSupport.CreateReservationWithFees(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 31),
            ProrateType.FirstMonth,
            BillingType.Monthly,
            maidStartDate: new DateOnly(2100, 1, 1),
            deposit: Collected,
            hasPets: false);
        var context = AccountingManagerJournalEntryFeeTestSupport.CreateFeeJournalEntryTestContext(reservation);
        var manager = context.CreateManager();
        var loader = typeof(AccountingManager).GetMethod("LoadCostCodeByOfficeIdAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)loader.Invoke(manager, [AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId])!;
        task.GetAwaiter().GetResult();
        var costCodeById = (IReadOnlyDictionary<int, CostCode>)task.GetType().GetProperty("Result")!.GetValue(task)!;
        return (manager, context, costCodeById);
    }

    private static async Task<JournalEntry> CreateInvoiceJournalEntryAsync(AccountingManager manager, AccountingManagerJournalEntryFeeTestSupport.FeeJournalEntryTestContext context, Invoice invoice)
    {
        context.TrackInvoice(invoice);
        var journalEntry = await manager.CreateJournalEntryFromInvoiceAsync(invoice, AccountingManagerJournalEntryTestSupport.CurrentUser);
        Assert.NotNull(journalEntry);
        Assert.Equal(journalEntry!.JournalEntryLines.Sum(line => line.Debit), journalEntry.JournalEntryLines.Sum(line => line.Credit));
        return journalEntry;
    }

    private static Invoice BuildSecurityDepositInvoice()
    {
        var invoice = BuildInvoice("R-000001-001", new DateOnly(2026, 9, 1), [ChargeLine(AccountingManagerJournalEntryFeeTestSupport.SecurityDepositCostCodeId, Collected, "Security Deposit")]);
        AddLine(invoice, PaymentLine(Collected, Guid.NewGuid(), "Tenant payment"));
        return invoice;
    }

    private static Invoice BuildInvoice(string code, DateOnly date, List<LedgerLine> lines)
    {
        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            InvoiceCode = code,
            ReservationId = AccountingManagerJournalEntryTestSupport.ReservationId,
            ReservationCode = ReservationCode,
            PropertyId = AccountingManagerJournalEntryTestSupport.PropertyId,
            InvoiceDate = date,
            AccountingPeriod = new DateOnly(date.Year, date.Month, 1),
            IsActive = true
        };
        foreach (var line in lines)
            AddLine(invoice, line);
        return invoice;
    }

    private static void AddLine(Invoice invoice, LedgerLine line)
    {
        line.LedgerLineId = Guid.NewGuid();
        line.InvoiceId = invoice.InvoiceId;
        line.ReservationId = invoice.ReservationId;
        line.LineNumber = invoice.LedgerLines.Count + 1;
        if (line.LedgerLineDate == default)
            line.LedgerLineDate = invoice.InvoiceDate;
        invoice.LedgerLines.Add(line);
        if (line.CostCodeId == AccountingManagerJournalEntryFeeTestSupport.PaymentCostCodeId)
            invoice.PaidAmount += line.Amount;
        else
            invoice.TotalAmount += line.Amount;
    }

    private static LedgerLine ChargeLine(int costCodeId, decimal amount, string description)
        => new() { CostCodeId = costCodeId, Amount = amount, Description = description };

    private static LedgerLine CreditLine(decimal amount)
        => ChargeLine(AccountingManagerJournalEntryFeeTestSupport.SecurityDepositCostCodeId, amount, $"{ReservationCode}: Security Deposit Credit");

    private static LedgerLine PaymentLine(decimal amount, Guid paymentId, string description)
        => new() { CostCodeId = AccountingManagerJournalEntryFeeTestSupport.PaymentCostCodeId, Amount = amount, Description = description, PaymentId = paymentId };

    private static LedgerLine RefundLine(decimal amount, Guid paymentId)
        => PaymentLine(amount, paymentId, $"{ReservationCode}: Security Deposit Refund");

    private static object? InvokeStatic(string name, params object?[] args)
        => typeof(AccountingManager).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);

    private static decimal SumAmounts(object? lines)
        => ((IEnumerable)lines!).Cast<object>().Sum(line => (decimal)line.GetType().GetProperty("Amount")!.GetValue(line)!);

    private static void AssertLine(JournalEntry journalEntry, int accountId, decimal debit, decimal credit)
    {
        var lines = journalEntry.JournalEntryLines.Where(line => line.ChartOfAccountId == accountId).ToList();
        Assert.Equal(debit, lines.Sum(line => line.Debit));
        Assert.Equal(credit, lines.Sum(line => line.Credit));
    }

    private void Print(string scenario, Invoice invoice, Figures figures, JournalEntry? journalEntry)
    {
        _output.WriteLine($"=== {scenario} ===");
        _output.WriteLine($"Invoice {invoice.InvoiceCode}: Total {invoice.TotalAmount:0.00}  Paid {invoice.PaidAmount:0.00}  Due {invoice.TotalAmount - invoice.PaidAmount:0.00}");
        foreach (var line in invoice.LedgerLines)
            _output.WriteLine($"  line {line.LineNumber}: {line.Description,-40} {line.Amount,10:0.00}");
        _output.WriteLine($"SD list: Deposit {figures.Deposit:0.00}  Collected {figures.Collected:0.00}  Owed {figures.Owed:0.00}  Balance {figures.Balance:0.00}  TBT {figures.ToBeTransferred:0.00}");
        if (journalEntry == null)
            return;

        _output.WriteLine("Invoice JE:");
        foreach (var line in journalEntry.JournalEntryLines)
        {
            var account = line.ChartOfAccountId switch
            {
                ArAccountId => "A/R",
                IncomeAccountId => "Tenant Income",
                SdLiabilityAccountId => "SD Liability",
                _ => line.ChartOfAccountId.ToString()
            };
            _output.WriteLine($"  {account,-15} Dr {line.Debit,10:0.00}  Cr {line.Credit,10:0.00}");
        }
    }
}
