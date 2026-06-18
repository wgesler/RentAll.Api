using Moq;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

internal static class AccountingManagerJournalEntryFeeTestSupport
{
    internal const int SecurityDepositCostCodeId = 78;
    internal const int PetFeeCostCodeId = 79;
    internal const int MaidServiceCostCodeId = 80;
    internal const int PaymentCostCodeId = 81;
    internal const int SdwCostCodeId = 82;
    internal const int DepartureFeeCostCodeId = 83;
    internal const int ExtraFeeCostCodeId = 84;
    internal const int UndepositedFundsAccountId = 300;
    internal const int PrePaymentAccountId = 400;

    internal static Reservation CreateReservationWithFees(
        DateOnly arrival,
        DateOnly departure,
        ProrateType prorateType,
        BillingType billingType,
        DateOnly maidStartDate,
        DepositType depositType = DepositType.Deposit,
        decimal deposit = 500m,
        bool hasPets = true,
        decimal petFee = 250m,
        decimal departureFee = -1m,
        decimal maidServiceFee = 100m,
        decimal? billingRate = null,
        IReadOnlyList<ExtraFeeLine>? extraFeeLines = null)
    {
        return new Reservation
        {
            ReservationId = AccountingManagerJournalEntryTestSupport.ReservationId,
            OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
            OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
            PropertyId = AccountingManagerJournalEntryTestSupport.PropertyId,
            ArrivalDate = arrival,
            DepartureDate = departure,
            BillingType = billingType,
            ProrateType = prorateType,
            BillingRate = billingRate ?? (billingType == BillingType.Monthly ? 3000m : 100m),
            Deposit = deposit,
            DepositType = depositType,
            HasPets = hasPets,
            PetFee = petFee,
            DepartureFee = departureFee,
            MaidServiceFee = maidServiceFee,
            Frequency = FrequencyType.Weekly,
            MaidStartDate = maidStartDate,
            ExtraFeeLines = extraFeeLines?.ToList() ?? []
        };
    }

    internal static async Task<List<LedgerLine>> GetInvoiceLedgerLinesAsync(
        AccountingManager manager,
        Reservation reservation,
        DateOnly periodStart,
        DateOnly periodEnd)
        => await manager.CreateLedgerLinesForReservationIdAsync(reservation, periodStart, periodStart, periodEnd);

    internal static LedgerLine CreatePaymentLedgerLine(
        Invoice invoice,
        decimal amount,
        DateOnly paymentDate,
        string description = "Payment")
    {
        return new LedgerLine
        {
            LedgerLineId = Guid.NewGuid(),
            InvoiceId = invoice.InvoiceId,
            ReservationId = invoice.ReservationId,
            CostCodeId = PaymentCostCodeId,
            Amount = amount,
            Description = description,
            LedgerLineDate = paymentDate
        };
    }

    internal static FeeJournalEntryTestContext CreateFeeJournalEntryTestContext(Reservation reservation)
        => new(reservation);

    internal sealed class FeeJournalEntryTestContext
    {
        private readonly Reservation _reservation;
        private readonly List<JournalEntry> _journalEntries = [];
        private readonly Dictionary<Guid, Invoice> _invoices = [];
        private int _journalEntryCodeSequence;

        internal FeeJournalEntryTestContext(Reservation reservation)
        {
            _reservation = reservation;
        }

        internal IReadOnlyList<JournalEntry> CreatedJournalEntries => _journalEntries;

        internal IReadOnlyList<JournalEntry> ActiveJournalEntries
            => _journalEntries.Where(entry => !entry.IsVoided).ToList();

        internal void TrackInvoice(Invoice invoice)
            => _invoices[invoice.InvoiceId] = CloneInvoice(invoice);

        internal AccountingManager CreateManager()
        {
            var chartOfAccounts = new List<ChartOfAccount>
            {
                new()
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    AccountId = AccountingManagerJournalEntryTestSupport.AccountsReceivableAccountId,
                    AccountType = AccountType.AccountsReceivable,
                    Name = "Accounts Receivable",
                    AccountNo = "1200"
                },
                new()
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    AccountId = AccountingManagerJournalEntryTestSupport.TenantIncomeAccountId,
                    AccountType = AccountType.Income,
                    Name = "Tenant Income",
                    AccountNo = "4000"
                },
                new()
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    AccountId = UndepositedFundsAccountId,
                    AccountType = AccountType.OtherCurrentAsset,
                    Name = "Undeposited Funds",
                    AccountNo = "1250"
                },
                new()
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    AccountId = PrePaymentAccountId,
                    AccountType = AccountType.OtherCurrentLiability,
                    Name = "Pre-Payment",
                    AccountNo = "2100"
                }
            };

            var costCodes = new List<CostCode>
            {
                ChargeCostCode(AccountingManagerJournalEntryTestSupport.RentalCostCodeId, "4000", "Rent"),
                ChargeCostCode(SecurityDepositCostCodeId, "4000", "Security Deposit", TransactionType.SecurityDeposit),
                ChargeCostCode(PetFeeCostCodeId, "4000", "Pet Fee"),
                ChargeCostCode(MaidServiceCostCodeId, "4000", "Maid Service"),
                ChargeCostCode(SdwCostCodeId, "4000", "Security Deposit Waiver", TransactionType.SecurityDepositWaiver),
                ChargeCostCode(DepartureFeeCostCodeId, "4000", "Departure Fee"),
                ChargeCostCode(ExtraFeeCostCodeId, "4000", "Extra Fee"),
                new()
                {
                    CostCodeId = PaymentCostCodeId,
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    Code = "1250",
                    Description = "Payment",
                    TransactionType = TransactionType.Payment,
                    IsActive = true
                }
            };

            var accountingRepository = new Mock<IAccountingRepository>();
            accountingRepository
                .Setup(r => r.GetCostCodesByOfficeIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
                .ReturnsAsync(costCodes);
            accountingRepository
                .Setup(r => r.GetChartOfAccountsByOfficeIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
                .ReturnsAsync(chartOfAccounts);
            accountingRepository
                .Setup(r => r.GetBankCardsByOfficeIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
                .ReturnsAsync([]);
            accountingRepository
                .Setup(r => r.GetInvoiceByIdAsync(It.IsAny<Guid>(), AccountingManagerJournalEntryTestSupport.OrganizationId))
                .ReturnsAsync((Guid invoiceId, Guid _) =>
                    _invoices.TryGetValue(invoiceId, out var invoice) ? CloneInvoice(invoice) : null);
            accountingRepository
                .Setup(r => r.UpdateByIdAsync(It.IsAny<Invoice>()))
                .ReturnsAsync((Invoice invoice) =>
                {
                    _invoices[invoice.InvoiceId] = CloneInvoice(invoice);
                    return invoice;
                });

            var organizationRepository = new Mock<IOrganizationRepository>();
            organizationRepository
                .Setup(r => r.GetAccountingOfficeByIdAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, AccountingManagerJournalEntryTestSupport.OfficeId))
                .ReturnsAsync(new AccountingOffice
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    DefaultActRecvAccountId = AccountingManagerJournalEntryTestSupport.AccountsReceivableAccountId,
                    DefaultTenantIncAccountId = AccountingManagerJournalEntryTestSupport.TenantIncomeAccountId,
                    DefaultUndepFundsAccountId = UndepositedFundsAccountId,
                    DefaultPrePayAccountId = PrePaymentAccountId
                });
            organizationRepository
                .Setup(r => r.GetOfficeByIdAsync(AccountingManagerJournalEntryTestSupport.OfficeId, AccountingManagerJournalEntryTestSupport.OrganizationId))
                .ReturnsAsync(new Office
                {
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                    FurnishedRentChargeCcId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
                    UnfurnishedRentChargeCcId = AccountingManagerJournalEntryTestSupport.RentalCostCodeId,
                    SecurityDepositCcId = SecurityDepositCostCodeId,
                    SecurityDepositWaiverCcId = SdwCostCodeId,
                    DepartureFeeCcId = DepartureFeeCostCodeId,
                    MaidServiceChargeCcId = MaidServiceCostCodeId,
                    PetFeeCcId = PetFeeCostCodeId
                });

            var propertyRepository = new Mock<IPropertyRepository>();
            propertyRepository
                .Setup(r => r.GetPropertyByIdAsync(AccountingManagerJournalEntryTestSupport.PropertyId, AccountingManagerJournalEntryTestSupport.OrganizationId))
                .ReturnsAsync(new Property
                {
                    PropertyId = AccountingManagerJournalEntryTestSupport.PropertyId,
                    OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                    Unfurnished = false
                });

            var journalEntryRepository = new Mock<IJournalEntryRepository>();
            journalEntryRepository
                .Setup(r => r.GetJournalEntriesAsync(It.IsAny<JournalEntryGetCriteria>()))
                .ReturnsAsync((JournalEntryGetCriteria criteria) =>
                {
                    return _journalEntries.Where(entry =>
                        entry.OrganizationId == criteria.OrganizationId
                        && (criteria.IncludeVoided || !entry.IsVoided)
                        && (criteria.SourceTypeId == null || entry.SourceTypeId == criteria.SourceTypeId)
                        && (criteria.SourceId == null || entry.SourceId == criteria.SourceId)).ToList();
                });
            journalEntryRepository
                .Setup(r => r.ExistsByJournalEntryCodeAsync(It.IsAny<string>(), AccountingManagerJournalEntryTestSupport.OrganizationId))
                .ReturnsAsync(false);
            journalEntryRepository
                .Setup(r => r.CreateJournalEntryAsync(It.IsAny<JournalEntry>()))
                .ReturnsAsync((JournalEntry entry) =>
                {
                    entry.JournalEntryId = Guid.NewGuid();
                    foreach (var line in entry.JournalEntryLines)
                        line.JournalEntryId = entry.JournalEntryId;
                    _journalEntries.Add(CloneJournalEntry(entry));
                    return entry;
                });
            journalEntryRepository
                .Setup(r => r.GetJournalEntryByIdAsync(It.IsAny<Guid>(), AccountingManagerJournalEntryTestSupport.OrganizationId))
                .ReturnsAsync((Guid journalEntryId, Guid _) =>
                    _journalEntries.FirstOrDefault(entry => entry.JournalEntryId == journalEntryId) is { } found
                        ? CloneJournalEntry(found)
                        : null);
            journalEntryRepository
                .Setup(r => r.UpdateJournalEntryByIdAsync(It.IsAny<JournalEntry>()))
                .ReturnsAsync((JournalEntry entry) =>
                {
                    var index = _journalEntries.FindIndex(existing => existing.JournalEntryId == entry.JournalEntryId);
                    if (index >= 0)
                        _journalEntries[index] = CloneJournalEntry(entry);
                    return entry;
                });
            journalEntryRepository
                .Setup(r => r.DeleteJournalEntryByIdAsync(It.IsAny<Guid>(), AccountingManagerJournalEntryTestSupport.OrganizationId))
                .Returns((Guid journalEntryId, Guid _) =>
                {
                    _journalEntries.RemoveAll(entry => entry.JournalEntryId == journalEntryId);
                    return Task.CompletedTask;
                });

            var reservationRepository = new Mock<IReservationRepository>();
            reservationRepository
                .Setup(r => r.GetReservationByIdAsync(_reservation.ReservationId, AccountingManagerJournalEntryTestSupport.OrganizationId))
                .ReturnsAsync(_reservation);

            var organizationManager = new Mock<IOrganizationManager>();
            organizationManager
                .Setup(m => m.GenerateEntityCodeAsync(AccountingManagerJournalEntryTestSupport.OrganizationId, EntityType.JournalEntry))
                .ReturnsAsync(() => $"JE-{Interlocked.Increment(ref _journalEntryCodeSequence):D4}");

            return new AccountingManager(
                organizationRepository.Object,
                propertyRepository.Object,
                accountingRepository.Object,
                maintenanceRepository: null!,
                reservationRepository.Object,
                journalEntryRepository.Object,
                organizationManager.Object,
                new AccountingManagerJournalEntryTestSupport.EnabledFeatureFlagService());
        }

        private static CostCode ChargeCostCode(
            int costCodeId,
            string accountCode,
            string description,
            TransactionType transactionType = TransactionType.Charge)
            => new()
            {
                CostCodeId = costCodeId,
                OrganizationId = AccountingManagerJournalEntryTestSupport.OrganizationId,
                OfficeId = AccountingManagerJournalEntryTestSupport.OfficeId,
                Code = accountCode,
                Description = description,
                TransactionType = transactionType,
                IsActive = true
            };

        private static Invoice CloneInvoice(Invoice invoice)
            => new()
            {
                InvoiceId = invoice.InvoiceId,
                OrganizationId = invoice.OrganizationId,
                OfficeId = invoice.OfficeId,
                InvoiceCode = invoice.InvoiceCode,
                ReservationId = invoice.ReservationId,
                PropertyId = invoice.PropertyId,
                AccountingPeriod = invoice.AccountingPeriod,
                InvoiceDate = invoice.InvoiceDate,
                InvoicePeriod = invoice.InvoicePeriod,
                TotalAmount = invoice.TotalAmount,
                ModifiedBy = invoice.ModifiedBy,
                LedgerLines = invoice.LedgerLines.Select(line => new LedgerLine
                {
                    LedgerLineId = line.LedgerLineId,
                    InvoiceId = line.InvoiceId,
                    LineNumber = line.LineNumber,
                    ReservationId = line.ReservationId,
                    CostCodeId = line.CostCodeId,
                    Amount = line.Amount,
                    Description = line.Description,
                    LedgerLineDate = line.LedgerLineDate
                }).ToList()
            };

        private static JournalEntry CloneJournalEntry(JournalEntry entry)
            => new()
            {
                JournalEntryId = entry.JournalEntryId,
                OrganizationId = entry.OrganizationId,
                OfficeId = entry.OfficeId,
                JournalEntryCode = entry.JournalEntryCode,
                TransactionDate = entry.TransactionDate,
                PostingDate = entry.PostingDate,
                SourceTypeId = entry.SourceTypeId,
                SourceId = entry.SourceId,
                Memo = entry.Memo,
                IsPosted = entry.IsPosted,
                IsVoided = entry.IsVoided,
                CreatedBy = entry.CreatedBy,
                ModifiedBy = entry.ModifiedBy,
                JournalEntryLines = entry.JournalEntryLines.Select(line => new JournalEntryLine
                {
                    JournalEntryLineId = line.JournalEntryLineId,
                    JournalEntryId = line.JournalEntryId,
                    ChartOfAccountId = line.ChartOfAccountId,
                    CostCodeId = line.CostCodeId,
                    ReservationId = line.ReservationId,
                    PropertyId = line.PropertyId,
                    ContactId = line.ContactId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Memo = line.Memo,
                    CreatedBy = line.CreatedBy
                }).ToList()
            };
    }
}
