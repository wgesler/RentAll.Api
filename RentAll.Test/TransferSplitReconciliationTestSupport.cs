using Moq;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

/// <summary>
/// DP-156 / PY-953 + R-000000093-005 shared $11,810 escrow line — mirrors production transfer-create rematch.
/// </summary>
internal static class TransferSplitReconciliationTestSupport
{
    internal const int OfficeId = 5;
    internal const int EscrowDepositAccountId = 9100;
    internal const int EscrowOwnersAccountId = 9101;
    internal const int UndepositedFundsAccountId = 9102;
    internal const int OwnerPayableAccountId = 9103;
    internal const int EscrowSdwAccountId = 9104;
    internal const int TenantIncomeAccountId = 9105;
    internal const int PrePayAccountId = 9106;
    internal const int ActRcvableAccountId = 9107;
    internal const int OwnerExpenseAccountId = 9108;
    internal const int EscrowSecDepAccountId = 9109;

    internal static readonly Guid OrganizationId = Guid.Parse("280cd8da-f1be-41f2-ae6e-b45008cf3896");
    internal static readonly Guid CurrentUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
    internal static readonly Guid Deposit156Id = Guid.Parse("00000001-0000-0000-0000-000000000156");
    internal static readonly Guid Payment953Id = Guid.Parse("00000002-0000-0000-0000-000000000953");
    internal static readonly Guid DepositJournalEntryId = Guid.Parse("00000003-0000-0000-0000-000075613000");
    internal static readonly Guid EscrowLineId = Guid.Parse("00000004-0000-0000-0000-000000000001");
    internal static readonly Guid Payment953UfLineId = Guid.Parse("00000005-0000-0000-0000-000000000953");
    internal static readonly Guid Payment953JeId = Guid.Parse("00000006-0000-0000-0000-000000000953");
    internal static readonly Guid Py953OwnerActualJeId = Guid.Parse("0000000a-0000-0000-0000-000000000953");
    internal static readonly Guid Py953SdwActualJeId = Guid.Parse("0000000b-0000-0000-0000-000000000953");
    internal static readonly Guid R093UfLineId = Guid.Parse("00000007-0000-0000-0000-000000000093");
    internal static readonly Guid R093PaymentJeId = Guid.Parse("00000008-0000-0000-0000-000000000093");
    internal static readonly Guid CreatedTransferId = Guid.Parse("00000009-0000-0000-0000-000000000001");
    internal static readonly Guid R093PropertyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid Py953PropertyId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    internal sealed class PostInsertProbe
    {
        public bool CreateTransferInvoked { get; set; }
        public bool SetDepositTransferIdInvoked { get; set; }
        public bool CreateJournalEntryInvoked { get; set; }
        public IReadOnlyList<Guid> PersistedSplitLineIds { get; set; } = [];
    }

    internal static Transfer BuildDp156TransferWithPy953AndR093Splits()
    {
        return new Transfer
        {
            TransferId = Guid.Empty,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TransferCode = "TR-TEST-DP156",
            TransferDate = new DateOnly(2026, 9, 20),
            AccountingPeriod = new DateOnly(2026, 9, 1),
            Amount = 11810m,
            BankAccountId = EscrowDepositAccountId,
            IsActive = true,
            Splits =
            [
                new TransferSplit
                {
                    Amount = 7750m,
                    Description = "Transfer to Escrow Accounts - R-000000093-005",
                    ChartOfAccountId = EscrowOwnersAccountId,
                    JournalEntryLineId = Payment953UfLineId,
                    PropertyId = R093PropertyId
                },
                new TransferSplit
                {
                    Amount = 4060m,
                    Description = "Transfer to Escrow Accounts - PY-000000953",
                    ChartOfAccountId = EscrowOwnersAccountId,
                    JournalEntryLineId = Payment953UfLineId,
                    PropertyId = Py953PropertyId
                }
            ]
        };
    }

    /// <summary>
    /// Distinct UF line ids per deposit split — closer to what the UI sends before rematch.
    /// </summary>
    internal static Transfer BuildDp156TransferWithDistinctUfLineIds()
    {
        return new Transfer
        {
            TransferId = Guid.Empty,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            TransferCode = "TR-TEST-DP156-DISTINCT",
            TransferDate = new DateOnly(2026, 9, 20),
            AccountingPeriod = new DateOnly(2026, 9, 1),
            Amount = 11810m,
            BankAccountId = EscrowDepositAccountId,
            IsActive = true,
            Splits =
            [
                new TransferSplit
                {
                    Amount = 7750m,
                    Description = "Transfer to Escrow Accounts - R-000000093-005",
                    ChartOfAccountId = EscrowOwnersAccountId,
                    JournalEntryLineId = R093UfLineId
                },
                new TransferSplit
                {
                    Amount = 4060m,
                    Description = "Transfer to Escrow Accounts - PY-000000953",
                    ChartOfAccountId = EscrowOwnersAccountId,
                    JournalEntryLineId = Payment953UfLineId
                }
            ]
        };
    }

    internal static AccountingManager CreateManager(out Mock<IAccountingRepository> accountingRepository)
    {
        accountingRepository = new Mock<IAccountingRepository>(MockBehavior.Strict);

        var organizationRepository = new Mock<IOrganizationRepository>(MockBehavior.Strict);
        organizationRepository
            .Setup(repo => repo.GetAccountingOfficeByIdAsync(OrganizationId, OfficeId))
            .ReturnsAsync(new AccountingOffice
            {
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                DefaultEscrowDepositAccountId = EscrowDepositAccountId,
                DefaultEscrowOwnersAccountId = EscrowOwnersAccountId,
                DefaultOwnActPayableAccountId = OwnerPayableAccountId,
                DefaultEscrowSdwAccountId = EscrowSdwAccountId,
                DefaultTenantIncAccountId = TenantIncomeAccountId,
                DefaultPrePayAccountId = PrePayAccountId,
                DefaultActRcvableAccountId = ActRcvableAccountId,
                DefaultOwnerExpAccountId = OwnerExpenseAccountId,
                DefaultEscrowSecDepAccountId = EscrowSecDepAccountId,
                DefaultBankAccountId = UndepositedFundsAccountId
            });

        accountingRepository
            .Setup(repo => repo.GetChartOfAccountsByOfficeIdAsync(OrganizationId, OfficeId))
            .ReturnsAsync(BuildChartOfAccounts());
        accountingRepository
            .Setup(repo => repo.GetBankCardsByOfficeIdAsync(OrganizationId, OfficeId))
            .ReturnsAsync([]);

        var deposit = BuildDeposit156();
        accountingRepository
            .Setup(repo => repo.GetDepositsByCriteriaAsync(It.IsAny<DepositGetCriteria>()))
            .ReturnsAsync([deposit]);
        accountingRepository
            .Setup(repo => repo.GetDepositByIdAsync(Deposit156Id, OrganizationId))
            .ReturnsAsync(deposit);
        accountingRepository
            .Setup(repo => repo.GetPaymentsByOfficeIdsAsync(OrganizationId, OfficeId.ToString(), (int)PaymentKind.Invoice))
            .ReturnsAsync(BuildPayments());
        accountingRepository
            .Setup(repo => repo.GetPaymentByIdAsync(Payment953Id, OrganizationId))
            .ReturnsAsync(BuildPayments()[0]);
        accountingRepository
            .Setup(repo => repo.GetTransfersByCriteriaAsync(It.IsAny<TransferGetCriteria>()))
            .ReturnsAsync([]);

        SetupPaymentLedgerLineMocks(accountingRepository);

        accountingRepository
            .Setup(repo => repo.CreateTransferAsync(It.IsAny<Transfer>()))
            .ThrowsAsync(new InvalidOperationException("VALIDATION_PASSED"));

        var journalEntryRepository = new Mock<IJournalEntryRepository>(MockBehavior.Strict);
        var depositJournalEntry = BuildDepositJournalEntry();
        var paymentJournalEntry = BuildPaymentJournalEntry();
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntriesAsync(It.IsAny<JournalEntryGetCriteria>()))
            .ReturnsAsync([depositJournalEntry]);
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntriesByDepositIdAsync(It.IsAny<JournalEntryGetByDepositIdCriteria>()))
            .ReturnsAsync([depositJournalEntry, paymentJournalEntry]);
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntriesByPaymentIdAsync(It.IsAny<JournalEntryGetByPaymentIdCriteria>()))
            .ReturnsAsync((JournalEntryGetByPaymentIdCriteria criteria) =>
                criteria.PaymentId == Payment953Id
                    ? BuildPy953PaymentJournalEntries()
                    : []);
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntryLineByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid lineId) => BuildLineById(lineId));
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntryByIdAsync(It.IsAny<Guid>(), OrganizationId))
            .ReturnsAsync((Guid journalEntryId, Guid _) =>
                journalEntryId == DepositJournalEntryId
                    ? depositJournalEntry
                    : BuildPaymentJournalEntry());

        return new AccountingManager(
            organizationRepository.Object,
            propertyRepository: null!,
            accountingRepository.Object,
            maintenanceRepository: null!,
            reservationRepository: null!,
            journalEntryRepository.Object,
            organizationManager: null!,
            contactRepository: null!,
            featureFlagService: new TestEnabledFeatureFlagService(),
            healthRepository: null!);
    }

    /// <summary>
    /// Full create path: validation/rematch runs, then repo persists UF line ids (production bug shape).
    /// </summary>
    internal static (AccountingManager Manager, Mock<IAccountingRepository> AccountingRepository, PostInsertProbe Probe)
        CreateManagerForPostInsertFailureProbe(bool paymentHasDepositStamp = false)
    {
        var probe = new PostInsertProbe();
        var accountingRepository = new Mock<IAccountingRepository>(MockBehavior.Strict);

        var organizationRepository = new Mock<IOrganizationRepository>(MockBehavior.Strict);
        organizationRepository
            .Setup(repo => repo.GetAccountingOfficeByIdAsync(OrganizationId, OfficeId))
            .ReturnsAsync(new AccountingOffice
            {
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                DefaultEscrowDepositAccountId = EscrowDepositAccountId,
                DefaultEscrowOwnersAccountId = EscrowOwnersAccountId,
                DefaultBankAccountId = UndepositedFundsAccountId
            });

        accountingRepository
            .Setup(repo => repo.GetChartOfAccountsByOfficeIdAsync(OrganizationId, OfficeId))
            .ReturnsAsync(BuildChartOfAccounts());
        accountingRepository
            .Setup(repo => repo.GetBankCardsByOfficeIdAsync(OrganizationId, OfficeId))
            .ReturnsAsync([]);

        var deposit = BuildDeposit156();
        accountingRepository
            .Setup(repo => repo.GetDepositsByCriteriaAsync(It.IsAny<DepositGetCriteria>()))
            .ReturnsAsync([deposit]);
        accountingRepository
            .Setup(repo => repo.GetDepositByIdAsync(Deposit156Id, OrganizationId))
            .ReturnsAsync(deposit);
        accountingRepository
            .Setup(repo => repo.GetPaymentsByOfficeIdsAsync(OrganizationId, OfficeId.ToString(), (int)PaymentKind.Invoice))
            .ReturnsAsync(BuildPayments(paymentHasDepositStamp));
        accountingRepository
            .Setup(repo => repo.GetTransfersByCriteriaAsync(It.IsAny<TransferGetCriteria>()))
            .ReturnsAsync([]);

        SetupPaymentLedgerLineMocks(accountingRepository);

        accountingRepository
            .Setup(repo => repo.GetPaymentByIdAsync(Payment953Id, OrganizationId))
            .ReturnsAsync(BuildPayments(paymentHasDepositStamp)[0]);

        accountingRepository
            .Setup(repo => repo.CreateTransferAsync(It.IsAny<Transfer>()))
            .ReturnsAsync((Transfer input) =>
            {
                probe.CreateTransferInvoked = true;

                // Production persisted payment/reservation UF lines even though rematch ran in-memory.
                probe.PersistedSplitLineIds =
                [
                    R093UfLineId,
                    Payment953UfLineId
                ];

                return new Transfer
                {
                    TransferId = CreatedTransferId,
                    OrganizationId = input.OrganizationId,
                    OfficeId = input.OfficeId,
                    TransferCode = input.TransferCode,
                    TransferDate = input.TransferDate,
                    AccountingPeriod = input.AccountingPeriod,
                    Amount = input.Amount,
                    BankAccountId = input.BankAccountId,
                    IsActive = true,
                    Splits =
                    [
                        new TransferSplit
                        {
                            TransferSplitId = 1,
                            Amount = 7750m,
                            Description = "Transfer to Escrow Accounts - R-000000093-005",
                            ChartOfAccountId = EscrowOwnersAccountId,
                            JournalEntryLineId = R093UfLineId
                        },
                        new TransferSplit
                        {
                            TransferSplitId = 2,
                            Amount = 4060m,
                            Description = "Transfer to Escrow Accounts - PY-000000953",
                            ChartOfAccountId = EscrowOwnersAccountId,
                            JournalEntryLineId = Payment953UfLineId
                        }
                    ]
                };
            });

        accountingRepository
            .Setup(repo => repo.ClearDepositTransferIdsByTransferIdAsync(OrganizationId, CreatedTransferId, CurrentUser))
            .Returns(Task.CompletedTask);
        accountingRepository
            .Setup(repo => repo.SetDepositTransferIdAsync(It.IsAny<Guid>(), OrganizationId, CreatedTransferId, CurrentUser))
            .Callback(() => probe.SetDepositTransferIdInvoked = true)
            .Returns(Task.CompletedTask);
        accountingRepository
            .Setup(repo => repo.GetTransferByIdAsync(CreatedTransferId, OrganizationId))
            .ReturnsAsync((Guid _, Guid _) => new Transfer
            {
                TransferId = CreatedTransferId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                TransferCode = "TR-TEST-DP156-DISTINCT",
                IsActive = true,
                Splits =
                [
                    new TransferSplit { JournalEntryLineId = R093UfLineId, Amount = 7750m },
                    new TransferSplit { JournalEntryLineId = Payment953UfLineId, Amount = 4060m }
                ]
            });

        var journalEntryRepository = new Mock<IJournalEntryRepository>(MockBehavior.Strict);
        var depositJournalEntry = BuildDepositJournalEntry();
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntriesAsync(It.IsAny<JournalEntryGetCriteria>()))
            .ReturnsAsync([depositJournalEntry]);
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntriesByDepositIdAsync(It.IsAny<JournalEntryGetByDepositIdCriteria>()))
            .ReturnsAsync([depositJournalEntry]);
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntriesByPaymentIdAsync(It.IsAny<JournalEntryGetByPaymentIdCriteria>()))
            .ReturnsAsync([]);
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntryLineByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid lineId) => BuildLineById(lineId));
        journalEntryRepository
            .Setup(repo => repo.GetJournalEntryByIdAsync(It.IsAny<Guid>(), OrganizationId))
            .ReturnsAsync((Guid journalEntryId, Guid _) =>
                journalEntryId switch
                {
                    var id when id == DepositJournalEntryId => depositJournalEntry,
                    var id when id == Payment953JeId => BuildPaymentJournalEntry(),
                    var id when id == R093PaymentJeId => BuildReservationPaymentJournalEntry(),
                    _ => throw new InvalidOperationException($"Unexpected journal entry id in post-insert probe: {journalEntryId}")
                });

        var organizationManager = new Mock<IOrganizationManager>(MockBehavior.Strict);
        organizationManager
            .Setup(manager => manager.GenerateEntityCodeAsync(OrganizationId, EntityType.JournalEntry))
            .ReturnsAsync("JE-TEST-POST-INSERT");
        journalEntryRepository
            .Setup(repo => repo.CreateJournalEntryAsync(It.IsAny<JournalEntry>()))
            .Callback(() => probe.CreateJournalEntryInvoked = true)
            .ReturnsAsync((JournalEntry entry) => entry);

        var manager = new AccountingManager(
            organizationRepository.Object,
            propertyRepository: null!,
            accountingRepository.Object,
            maintenanceRepository: null!,
            reservationRepository: null!,
            journalEntryRepository.Object,
            organizationManager.Object,
            contactRepository: null!,
            featureFlagService: new TestEnabledFeatureFlagService(),
            healthRepository: null!);

        return (manager, accountingRepository, probe);
    }

    private static List<ChartOfAccount> BuildChartOfAccounts()
        =>
        [
            new ChartOfAccount
            {
                AccountId = EscrowDepositAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Escrow Deposit",
                AccountType = AccountType.OtherCurrentLiability
            },
            new ChartOfAccount
            {
                AccountId = EscrowOwnersAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Escrow Owners",
                AccountType = AccountType.OtherCurrentLiability
            },
            new ChartOfAccount
            {
                AccountId = UndepositedFundsAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Undeposited Funds",
                AccountType = AccountType.OtherCurrentAsset
            },
            new ChartOfAccount
            {
                AccountId = OwnerPayableAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Owner Payable",
                AccountType = AccountType.AccountsPayable
            },
            new ChartOfAccount
            {
                AccountId = EscrowSdwAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Escrow SDW",
                AccountType = AccountType.OtherCurrentLiability
            },
            new ChartOfAccount
            {
                AccountId = TenantIncomeAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Tenant Income",
                AccountType = AccountType.Income
            },
            new ChartOfAccount
            {
                AccountId = PrePayAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Customer Prepayments",
                AccountType = AccountType.OtherCurrentLiability
            },
            new ChartOfAccount
            {
                AccountId = ActRcvableAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Accounts Receivable",
                AccountType = AccountType.AccountsReceivable
            },
            new ChartOfAccount
            {
                AccountId = OwnerExpenseAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Owner Expense",
                AccountType = AccountType.Expense
            },
            new ChartOfAccount
            {
                AccountId = EscrowSecDepAccountId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                Name = "Escrow Sec Dep",
                AccountType = AccountType.OtherCurrentLiability
            }
        ];

    private static Deposit BuildDeposit156()
    {
        return new Deposit
        {
            DepositId = Deposit156Id,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            DepositCode = "DP-000000156",
            DepositDate = new DateOnly(2026, 9, 11),
            AccountingPeriod = new DateOnly(2026, 9, 1),
            Amount = 11810m,
            IsActive = true,
            Splits =
            [
                new DepositSplit
                {
                    DepositSplitId = 1,
                    Amount = 4060m,
                    ChartOfAccountId = UndepositedFundsAccountId,
                    JournalEntryLineId = Payment953UfLineId,
                    Description = "Payment: Ck 96296943 - 09/07-10/06"
                },
                new DepositSplit
                {
                    DepositSplitId = 2,
                    Amount = 5310m,
                    ChartOfAccountId = UndepositedFundsAccountId,
                    JournalEntryLineId = R093UfLineId,
                    Description = "R-000000093-005: Payment: Winnie Costello"
                }
            ]
        };
    }

    private static List<Payment> BuildPayments(bool paymentHasDepositStamp = true)
        =>
        [
            new Payment
            {
                PaymentId = Payment953Id,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                PaymentCode = "PY-000000953",
                PaymentDate = new DateOnly(2026, 9, 10),
                Amount = 4060m,
                DepositId = paymentHasDepositStamp ? Deposit156Id : null,
                IsActive = true
            }
        ];

    private static JournalEntry BuildDepositJournalEntry()
    {
        return new JournalEntry
        {
            JournalEntryId = DepositJournalEntryId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            JournalEntryCode = "JE-000075613",
            SourceTypeId = (int)SourceType.Deposit,
            DepositId = Deposit156Id,
            TransactionDate = new DateOnly(2026, 9, 11),
            AccountingPeriod = new DateOnly(2026, 9, 1),
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    JournalEntryLineId = EscrowLineId,
                    JournalEntryId = DepositJournalEntryId,
                    ChartOfAccountId = EscrowDepositAccountId,
                    Debit = 11810m,
                    Credit = 0m
                }
            ]
        };
    }

    private static JournalEntry BuildPaymentJournalEntry()
    {
        return new JournalEntry
        {
            JournalEntryId = Payment953JeId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PaymentId = Payment953Id,
            DepositId = Deposit156Id,
            JournalEntryKindId = JournalEntryKind.Payment,
            SourceTypeId = (int)SourceType.InvoicePayment,
            SourceCode = "PY-000000953",
            Memo = "Payment: Ck 96296943 - 09/07-10/06",
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    JournalEntryLineId = Payment953UfLineId,
                    JournalEntryId = Payment953JeId,
                    ChartOfAccountId = UndepositedFundsAccountId,
                    Debit = 4060m,
                    Credit = 0m
                }
            ]
        };
    }

    private static List<JournalEntry> BuildPy953PaymentJournalEntries()
        =>
        [
            BuildPaymentJournalEntry(),
            new JournalEntry
            {
                JournalEntryId = Py953OwnerActualJeId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                PaymentId = Payment953Id,
                JournalEntryKindId = JournalEntryKind.OwnerActual,
                SourceTypeId = (int)SourceType.Invoice,
                SourceCode = "R-000000396-002",
                JournalEntryLines =
                [
                    new JournalEntryLine
                    {
                        JournalEntryLineId = Guid.Parse("0000000c-0000-0000-0000-000000000953"),
                        JournalEntryId = Py953OwnerActualJeId,
                        ChartOfAccountId = OwnerPayableAccountId,
                        Debit = 0m,
                        Credit = 2808.40m
                    },
                    new JournalEntryLine
                    {
                        JournalEntryLineId = Guid.Parse("0000000d-0000-0000-0000-000000000953"),
                        JournalEntryId = Py953OwnerActualJeId,
                        ChartOfAccountId = TenantIncomeAccountId,
                        Debit = 2808.40m,
                        Credit = 0m
                    }
                ]
            },
            new JournalEntry
            {
                JournalEntryId = Py953SdwActualJeId,
                OrganizationId = OrganizationId,
                OfficeId = OfficeId,
                PaymentId = Payment953Id,
                JournalEntryKindId = JournalEntryKind.SecurityDepositWaiverActual,
                SourceTypeId = (int)SourceType.Invoice,
                SourceCode = "R-000000396-002",
                JournalEntryLines =
                [
                    new JournalEntryLine
                    {
                        JournalEntryLineId = Guid.Parse("0000000e-0000-0000-0000-000000000953"),
                        JournalEntryId = Py953SdwActualJeId,
                        ChartOfAccountId = EscrowSdwAccountId,
                        Debit = 0m,
                        Credit = 48m
                    },
                    new JournalEntryLine
                    {
                        JournalEntryLineId = Guid.Parse("0000000f-0000-0000-0000-000000000953"),
                        JournalEntryId = Py953SdwActualJeId,
                        ChartOfAccountId = TenantIncomeAccountId,
                        Debit = 48m,
                        Credit = 0m
                    }
                ]
            }
        ];

    private static JournalEntry BuildReservationPaymentJournalEntry()
    {
        return new JournalEntry
        {
            JournalEntryId = R093PaymentJeId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            SourceTypeId = (int)SourceType.InvoicePayment,
            SourceCode = "R-000000093-005",
            JournalEntryLines =
            [
                new JournalEntryLine
                {
                    JournalEntryLineId = R093UfLineId,
                    JournalEntryId = R093PaymentJeId,
                    ChartOfAccountId = UndepositedFundsAccountId,
                    Debit = 7750m,
                    Credit = 0m
                }
            ]
        };
    }

    private static JournalEntryLine BuildLineById(Guid lineId)
    {
        if (lineId == EscrowLineId)
        {
            return new JournalEntryLine
            {
                JournalEntryLineId = EscrowLineId,
                JournalEntryId = DepositJournalEntryId,
                ChartOfAccountId = EscrowDepositAccountId,
                Debit = 11810m,
                Credit = 0m
            };
        }

        if (lineId == Payment953UfLineId)
        {
            return new JournalEntryLine
            {
                JournalEntryLineId = Payment953UfLineId,
                JournalEntryId = Payment953JeId,
                ChartOfAccountId = UndepositedFundsAccountId,
                Debit = 4060m,
                Credit = 0m
            };
        }

        if (lineId == R093UfLineId)
        {
            return new JournalEntryLine
            {
                JournalEntryLineId = R093UfLineId,
                JournalEntryId = R093PaymentJeId,
                ChartOfAccountId = UndepositedFundsAccountId,
                Debit = 7750m,
                Credit = 0m
            };
        }

        throw new InvalidOperationException($"Unexpected journal entry line id in test: {lineId}");
    }

    private static void SetupPaymentLedgerLineMocks(Mock<IAccountingRepository> accountingRepository)
    {
        accountingRepository
            .Setup(repo => repo.GetLedgerLinesByPaymentIdAsync(It.IsAny<Guid>(), OrganizationId))
            .ReturnsAsync((Guid paymentId, Guid _) =>
                paymentId == Payment953Id
                    ?
                    [
                        new PaymentLedgerLine
                        {
                            PaymentId = Payment953Id,
                            InvoiceCode = "R-000000396-002",
                            Amount = 3248m
                        },
                        new PaymentLedgerLine
                        {
                            PaymentId = Payment953Id,
                            InvoiceCode = "R-000000396-003",
                            Amount = 812m
                        }
                    ]
                    : []);
    }

    private sealed class TestEnabledFeatureFlagService : IFeatureFlagService
    {
        public IReadOnlyDictionary<string, bool> GetAll()
            => new Dictionary<string, bool> { [FeatureFlagKeys.Accounting] = true };

        public bool IsEnabled(string featureName) => true;

        public Task<bool> IsEnabledAsync(string featureName, Guid organizationId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public void Set(string featureName, bool enabled)
        {
        }
    }
}
