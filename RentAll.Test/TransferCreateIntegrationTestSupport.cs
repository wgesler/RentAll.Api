using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Repositories.Accounting;
using RentAll.Infrastructure.Repositories.Common;
using RentAll.Infrastructure.Repositories.Contacts;
using RentAll.Infrastructure.Repositories.Health;
using RentAll.Infrastructure.Repositories.Maintenances;
using RentAll.Infrastructure.Repositories.Organizations;
using RentAll.Infrastructure.Repositories.Properties;
using RentAll.Infrastructure.Repositories.Reservations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace RentAll.Test;

/// <summary>
/// Full integration harness for GL Make Transfer against the local GESLER database.
/// Gathers eligible deposits at run time (not on a transfer, escrow JE, linked splits).
/// Override via RENTALL_TRANSFER_TEST_OFFICE_ID / RENTALL_TRANSFER_TEST_DEPOSITS when needed.
/// </summary>
internal static class TransferCreateIntegrationTestSupport
{
    internal const int MinDepositsPerIntegrationRun = 1;
    internal const int TargetDepositsPerIntegrationRun = 2;
    internal static readonly Guid KnownOrganizationId = Guid.Parse("280cd8da-f1be-41f2-ae6e-b45008cf3896");

    internal static int[] SelectedDepositInts { get; private set; } = [];

    internal static int? OptionalConfiguredOfficeId =>
        int.TryParse(Environment.GetEnvironmentVariable("RENTALL_TRANSFER_TEST_OFFICE_ID"), out var officeId) && officeId > 0
            ? officeId
            : null;

    internal static int[]? OptionalConfiguredDepositInts =>
        ParseOptionalDepositIntList(Environment.GetEnvironmentVariable("RENTALL_TRANSFER_TEST_DEPOSITS"));

    internal static Guid ConfiguredOrganizationId =>
        Guid.TryParse(Environment.GetEnvironmentVariable("RENTALL_TRANSFER_TEST_ORG_ID"), out var organizationId) && organizationId != Guid.Empty
            ? organizationId
            : KnownOrganizationId;
    internal static readonly Guid IntegrationTestUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    internal static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("RENTALL_INTEGRATION_TESTS"), "1", StringComparison.OrdinalIgnoreCase);

    internal static bool EnsureEnabled()
    {
        return IsEnabled;
    }

    internal sealed class TransferEscrowLineContext
    {
        public required Deposit Deposit { get; init; }
        public required Guid JournalEntryLineId { get; init; }
        public required Guid JournalEntryId { get; init; }
        public required decimal EscrowAmount { get; init; }
    }

    internal sealed class TransferCreateIntegrationScenario
    {
        public required Guid OrganizationId { get; init; }
        public required int OfficeId { get; init; }
        public required AccountingOffice AccountingOffice { get; init; }
        public required IReadOnlyList<Deposit> Deposits { get; init; }
        public required IReadOnlyList<TransferEscrowLineContext> EscrowLines { get; init; }
    }

    internal sealed class IntegrationServices
    {
        public required AccountingManager AccountingManager { get; init; }
        public required IAccountingRepository AccountingRepository { get; init; }
        public required IOrganizationRepository OrganizationRepository { get; init; }
        public required IJournalEntryRepository JournalEntryRepository { get; init; }
        public required IHealthRepository HealthRepository { get; init; }
        public required IOrganizationManager OrganizationManager { get; init; }
    }

    internal static IntegrationServices CreateServices()
    {
        var appSettings = Options.Create(LoadAppSettings());
        var accountingRepository = new AccountingRepository(appSettings, NullLogger<AccountingRepository>.Instance);
        var organizationRepository = new OrganizationRepository(appSettings);
        var journalEntryRepository = new JournalEntryRepository(appSettings, NullLogger<JournalEntryRepository>.Instance);
        var commonRepository = new CommonRepository(appSettings);
        var organizationManager = new OrganizationManager(commonRepository, organizationRepository);
        var healthRepository = new HealthRepository(appSettings);

        var manager = new AccountingManager(
            organizationRepository,
            new PropertyRepository(appSettings),
            accountingRepository,
            new MaintenanceRepository(appSettings),
            new ReservationRepository(appSettings),
            journalEntryRepository,
            organizationManager,
            new ContactRepository(appSettings),
            new IntegrationEnabledFeatureFlagService(),
            healthRepository);

        return new IntegrationServices
        {
            AccountingManager = manager,
            AccountingRepository = accountingRepository,
            OrganizationRepository = organizationRepository,
            JournalEntryRepository = journalEntryRepository,
            HealthRepository = healthRepository,
            OrganizationManager = organizationManager
        };
    }

    internal static async Task AssertScenarioDepositsUnlinkedAfterTransferDeleteAsync(
        IAccountingRepository accountingRepository,
        TransferCreateIntegrationScenario scenario)
    {
        foreach (var deposit in scenario.Deposits)
        {
            var reloaded = await accountingRepository.GetDepositByIdAsync(deposit.DepositId, scenario.OrganizationId)
                ?? throw new InvalidOperationException($"Deposit {deposit.DepositCode} was not found after transfer delete.");

            if (reloaded.TransferId is { } transferId && transferId != Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Deposit {reloaded.DepositCode} is still linked to transfer {reloaded.TransferCode} after DeleteTransferAsync.");
            }
        }
    }

    internal static async Task<TransferCreateIntegrationScenario> TryLoadScenarioAsync(IntegrationServices services)
    {
        var scenario = await GatherIntegrationScenarioAsync(services);
        if (scenario == null)
        {
            throw SkipException.ForSkip(
                "No eligible deposits for transfer integration (not on a transfer, escrow JE, splits linked, allocations resolvable). "
                + "Clear transfers or fix deposit links, or set RENTALL_TRANSFER_TEST_OFFICE_ID / RENTALL_TRANSFER_TEST_DEPOSITS.");
        }

        SelectedDepositInts = scenario.Deposits
            .Select(deposit => ParseDocumentCodeInt(deposit.DepositCode) ?? 0)
            .Where(depositInt => depositInt > 0)
            .ToArray();

        return scenario;
    }

    private static async Task<TransferCreateIntegrationScenario?> GatherIntegrationScenarioAsync(IntegrationServices services)
    {
        var organizationId = ConfiguredOrganizationId;
        var explicitDepositInts = OptionalConfiguredDepositInts;
        if (explicitDepositInts is { Length: > 0 })
        {
            var pinnedOfficeId = OptionalConfiguredOfficeId
                ?? throw new InvalidOperationException(
                    "RENTALL_TRANSFER_TEST_DEPOSITS requires RENTALL_TRANSFER_TEST_OFFICE_ID when pinning deposit codes.");

            var pinned = await LoadDepositsForScenarioAsync(
                services.AccountingRepository,
                organizationId,
                pinnedOfficeId,
                explicitDepositInts);
            if (pinned.Count != explicitDepositInts.Length)
                return null;

            var pinnedScenario = await TryBuildScenarioFromDepositsAsync(services, organizationId, pinnedOfficeId, pinned);
            if (pinnedScenario == null || !await TryValidateScenarioAllocationsAsync(services.AccountingManager, pinnedScenario))
                return null;

            return pinnedScenario;
        }

        var candidates = await DiscoverIntegrationDepositCandidatesAsync(organizationId, OptionalConfiguredOfficeId);
        var candidatesByOffice = candidates
            .GroupBy(candidate => candidate.OfficeId)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key);

        foreach (var officeGroup in candidatesByOffice)
        {
            var officeId = officeGroup.Key;
            var selectedDeposits = new List<Deposit>();

            foreach (var candidate in officeGroup.OrderByDescending(item => item.DepositDate))
            {
                var deposit = await services.AccountingRepository.GetDepositByIdAsync(candidate.DepositId, organizationId);
                if (deposit == null || deposit.IsActive == false)
                    continue;

                if (deposit.TransferId is { } transferId && transferId != Guid.Empty)
                    continue;

                var trialDeposits = selectedDeposits.Append(deposit).ToList();
                var trialScenario = await TryBuildScenarioFromDepositsAsync(services, organizationId, officeId, trialDeposits);
                if (trialScenario == null)
                    continue;

                if (!await TryValidateScenarioAllocationsAsync(services.AccountingManager, trialScenario))
                    continue;

                selectedDeposits.Add(deposit);
                if (selectedDeposits.Count >= TargetDepositsPerIntegrationRun)
                    return trialScenario;
            }

            if (selectedDeposits.Count >= MinDepositsPerIntegrationRun)
            {
                return await TryBuildScenarioFromDepositsAsync(services, organizationId, officeId, selectedDeposits);
            }
        }

        return null;
    }

    private static async Task<TransferCreateIntegrationScenario?> TryBuildScenarioFromDepositsAsync(
        IntegrationServices services,
        Guid organizationId,
        int officeId,
        IReadOnlyList<Deposit> deposits)
    {
        if (deposits.Count < MinDepositsPerIntegrationRun)
            return null;

        var accountingOffice = await services.OrganizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
        var configuredEscrowAccountId = accountingOffice?.DefaultEscrowDepositAccountId;
        if (configuredEscrowAccountId is not > 0)
            return null;

        var escrowAccountId = configuredEscrowAccountId.Value;

        foreach (var deposit in deposits)
        {
            if (deposit.TransferId is { } transferId && transferId != Guid.Empty)
                return null;
        }

        var escrowLines = new List<TransferEscrowLineContext>();
        foreach (var deposit in deposits)
        {
            var depositJournalEntries = (await services.JournalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria
            {
                OrganizationId = organizationId,
                DepositId = deposit.DepositId
            })).ToList();

            var depositJournalEntry = depositJournalEntries
                .FirstOrDefault(entry => entry.SourceTypeId == (int)SourceType.Deposit && entry.DepositId == deposit.DepositId)
                ?? depositJournalEntries.FirstOrDefault(entry => entry.SourceTypeId == (int)SourceType.Deposit);
            if (depositJournalEntry == null)
                return null;

            var escrowLine = (depositJournalEntry.JournalEntryLines ?? [])
                .FirstOrDefault(line => line.ChartOfAccountId == escrowAccountId && Math.Abs(line.Debit - line.Credit) > 0.005m);
            if (escrowLine == null)
                return null;

            escrowLines.Add(new TransferEscrowLineContext
            {
                Deposit = deposit,
                JournalEntryLineId = escrowLine.JournalEntryLineId,
                JournalEntryId = depositJournalEntry.JournalEntryId,
                EscrowAmount = RoundCurrency(escrowLine.Debit - escrowLine.Credit)
            });
        }

        try
        {
            await AssertScenarioPaymentRematchPathsReadyAsync(organizationId, officeId, escrowAccountId, deposits);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return new TransferCreateIntegrationScenario
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountingOffice = accountingOffice,
            Deposits = deposits,
            EscrowLines = escrowLines
        };
    }

    private static async Task<bool> TryValidateScenarioAllocationsAsync(
        AccountingManager accountingManager,
        TransferCreateIntegrationScenario scenario)
    {
        try
        {
            var workItems = BuildTransferAllocationWorkItems(scenario.EscrowLines);
            if (workItems.Count == 0)
                return false;

            var allocationItems = workItems
                .Select(workItem => new TransferDepositAllocationRequestItem
                {
                    DepositId = workItem.ContextLine.Deposit.DepositId,
                    EscrowAmount = workItem.EscrowAmount,
                    JournalEntryLineId = workItem.AllocationJournalEntryLineId
                })
                .ToList();

            _ = await accountingManager.ResolveTransferDepositAllocationsAsync(
                scenario.OrganizationId,
                scenario.OfficeId,
                allocationItems);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static async Task<string> DescribeScenarioAvailabilityAsync(Guid organizationId, int officeId)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                CASE WHEN EXISTS (SELECT 1 FROM Organization.Office WHERE OfficeId = @OfficeId) THEN 1 ELSE 0 END AS OfficeExists,
                (SELECT COUNT(*) FROM Accounting.Deposit WHERE OrganizationId = @OrganizationId AND OfficeId = @OfficeId AND IsActive = 1) AS ActiveDeposits,
                (SELECT STRING_AGG(o.OfficeId, ', ') WITHIN GROUP (ORDER BY o.OfficeId)
                 FROM (
                    SELECT DISTINCT TOP 10 d.OfficeId
                    FROM Accounting.Deposit AS d
                    WHERE d.OrganizationId = @OrganizationId AND d.IsActive = 1
                    ORDER BY d.OfficeId
                 ) AS o) AS OfficesWithDeposits
            """;

        command.Parameters.AddWithValue("@OrganizationId", organizationId);
        command.Parameters.AddWithValue("@OfficeId", officeId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return "Could not inspect local database scenario availability.";

        var officeExists = reader.GetInt32(0) == 1;
        var activeDeposits = reader.GetInt32(1);
        var officesWithDeposits = reader.IsDBNull(2) ? "(none)" : reader.GetString(2);

        return " Local DB preflight:"
            + $" office {officeId} exists={officeExists}, activeDeposits={activeDeposits},"
            + $" officesWithDeposits={officesWithDeposits}."
            + " Restore/sync production data to GESLER or set RENTALL_DB_CONNECTION_STRING to a database that contains this scenario.";
    }

    private static int[]? ParseOptionalDepositIntList(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var parsed = rawValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var depositInt) ? depositInt : (int?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        return parsed.Length > 0 ? parsed : null;
    }

    private sealed record IntegrationDepositCandidate(Guid DepositId, int OfficeId, DateOnly DepositDate);

    internal static async Task<Transfer> BuildTransferModelAsync(
        AccountingManager accountingManager,
        IOrganizationManager organizationManager,
        TransferCreateIntegrationScenario scenario,
        Guid currentUser)
    {
        var escrowDepositAccountId = scenario.AccountingOffice.DefaultEscrowDepositAccountId
            ?? throw new InvalidOperationException("Escrow deposit account is not configured.");
        var allocationAccountIds = ResolveTransferAllocationAccountIds(scenario.AccountingOffice);
        if (allocationAccountIds.Owners is null or <= 0 || allocationAccountIds.Bank is null or <= 0)
            throw new InvalidOperationException("Owner escrow and bank accounts must be configured for this office.");

        var workItems = BuildTransferAllocationWorkItems(scenario.EscrowLines);
        var allocationItems = workItems
            .Select(workItem => new TransferDepositAllocationRequestItem
            {
                DepositId = workItem.ContextLine.Deposit.DepositId,
                EscrowAmount = workItem.EscrowAmount,
                JournalEntryLineId = workItem.AllocationJournalEntryLineId
            })
            .ToList();

        var allocations = await accountingManager.ResolveTransferDepositAllocationsAsync(
            scenario.OrganizationId,
            scenario.OfficeId,
            allocationItems);

        var splits = BuildTransferSplitsFromDepositAllocations(workItems, allocations, allocationAccountIds);
        var validationMessage = ValidateBuiltTransferSplits(splits, allocationAccountIds);
        if (!string.IsNullOrWhiteSpace(validationMessage))
            throw new InvalidOperationException(validationMessage);

        var splitTotal = RoundCurrency(splits.Sum(split => split.Amount));
        var transferDate = DateOnly.FromDateTime(DateTime.Today);
        var transferCode = await organizationManager.GenerateEntityCodeAsync(scenario.OrganizationId, EntityType.Transfer);

        return new Transfer
        {
            OrganizationId = scenario.OrganizationId,
            OfficeId = scenario.OfficeId,
            TransferCode = transferCode,
            TransferDate = transferDate,
            AccountingPeriod = transferDate,
            Amount = splitTotal,
            Description = "Transfer to Escrow Accounts",
            BankAccountId = escrowDepositAccountId,
            PropertyId = splits.FirstOrDefault(split => split.PropertyId is { } propertyId && propertyId != Guid.Empty)?.PropertyId,
            Splits = splits,
            IsActive = true,
            CreatedBy = currentUser
        };
    }

    internal static async Task AssertScenarioPaymentRematchPathsReadyAsync(
        Guid organizationId,
        int officeId,
        int escrowAccountId,
        IReadOnlyList<Deposit> deposits)
    {
        _ = organizationId;
        if (deposits.Count == 0)
            throw new InvalidOperationException("Scenario preflight: no deposits supplied.");

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();

        var readyCount = 0;
        foreach (var deposit in deposits)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TOP 1 CASE
                    WHEN p.PaymentId IS NULL THEN N'NO PAYMENT ROW'
                    WHEN p.DepositId IS NULL OR p.DepositId = '00000000-0000-0000-0000-000000000000' THEN N'NO DEPOSIT STAMP'
                    WHEN je.JournalEntryId IS NULL THEN N'NO DEPOSIT JE'
                    WHEN jel.JournalEntryLineId IS NULL THEN N'NO ESCROW LINE'
                    ELSE N'OK for payment rematch'
                END
                FROM Accounting.Payment AS p
                INNER JOIN Accounting.Deposit AS d ON d.DepositId = p.DepositId AND d.DepositId = @DepositId
                LEFT JOIN Accounting.JournalEntry AS je ON je.DepositId = d.DepositId AND je.SourceTypeId = 1
                LEFT JOIN Accounting.JournalEntryLine AS jel
                    ON jel.JournalEntryId = je.JournalEntryId
                    AND jel.ChartOfAccountId = @EscrowAccountId
                    AND ABS(jel.Debit - jel.Credit) > 0.005
                WHERE p.IsActive = 1
                    AND p.OfficeId = @OfficeId
                ORDER BY p.PaymentDate DESC
                """;

            command.Parameters.AddWithValue("@OfficeId", officeId);
            command.Parameters.AddWithValue("@EscrowAccountId", escrowAccountId);
            command.Parameters.AddWithValue("@DepositId", deposit.DepositId);

            var status = (string?)await command.ExecuteScalarAsync();
            if (string.Equals(status, "OK for payment rematch", StringComparison.Ordinal))
                readyCount++;
        }

        if (readyCount == 0)
        {
            throw new InvalidOperationException(
                $"Scenario preflight: none of the selected deposits on office {officeId} have a stamped payment with a deposit escrow JE line. "
                + $"Deposits: {string.Join(", ", deposits.Select(deposit => deposit.DepositCode))}.");
        }
    }

    private static async Task<List<Deposit>> LoadDepositsForScenarioAsync(
        IAccountingRepository accountingRepository,
        Guid organizationId,
        int officeId,
        IReadOnlyList<int> depositInts)
    {
        return (await accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            IsActive = true
        }))
            .Where(deposit => deposit.OfficeId == officeId)
            .Where(deposit => ParseDocumentCodeInt(deposit.DepositCode) is int depositInt && depositInts.Contains(depositInt))
            .OrderBy(deposit => ParseDocumentCodeInt(deposit.DepositCode))
            .ToList();
    }

    private static async Task<IReadOnlyList<IntegrationDepositCandidate>> DiscoverIntegrationDepositCandidatesAsync(
        Guid organizationId,
        int? officeIdFilter)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.DepositId, d.OfficeId, d.DepositDate
            FROM Accounting.Deposit AS d
            INNER JOIN Organization.AccountingOffice AS ao
                ON ao.OrganizationId = d.OrganizationId AND ao.OfficeId = d.OfficeId
            WHERE d.OrganizationId = @OrganizationId
                AND d.IsActive = 1
                AND d.Amount > 0.005
                AND (@OfficeId IS NULL OR d.OfficeId = @OfficeId)
                AND (d.TransferId IS NULL OR d.TransferId = '00000000-0000-0000-0000-000000000000')
                AND EXISTS (
                    SELECT 1
                    FROM Accounting.JournalEntry AS je
                    INNER JOIN Accounting.JournalEntryLine AS jel ON jel.JournalEntryId = je.JournalEntryId
                    WHERE je.DepositId = d.DepositId
                        AND jel.ChartOfAccountId = ao.DefaultEscrowDepositAccountId
                        AND ABS(jel.Debit - jel.Credit) > 0.005)
                AND NOT EXISTS (
                    SELECT 1 FROM Accounting.DepositSplit AS ds
                    WHERE ds.DepositId = d.DepositId
                        AND ABS(ds.Amount) > 0.005
                        AND (ds.JournalEntryLineId IS NULL OR ds.JournalEntryLineId = '00000000-0000-0000-0000-000000000000'))
            ORDER BY
                (SELECT COUNT(*) FROM Accounting.DepositSplit AS ds2 WHERE ds2.DepositId = d.DepositId AND ABS(ds2.Amount) > 0.005),
                d.DepositDate DESC,
                d.DepositCode
            """;

        command.Parameters.AddWithValue("@OrganizationId", organizationId);
        command.Parameters.AddWithValue("@OfficeId", (object?)officeIdFilter ?? DBNull.Value);

        var candidates = new List<IntegrationDepositCandidate>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var depositDate = reader.GetFieldValue<DateTime>(2);
            candidates.Add(new IntegrationDepositCandidate(
                reader.GetGuid(0),
                reader.GetInt32(1),
                DateOnly.FromDateTime(depositDate)));
        }

        return candidates;
    }

    private static List<TransferAllocationWorkItem> BuildTransferAllocationWorkItems(IReadOnlyList<TransferEscrowLineContext> escrowLines)
    {
        var workItems = new List<TransferAllocationWorkItem>();

        foreach (var contextLine in escrowLines)
        {
            var deposit = contextLine.Deposit;
            var paymentSplits = (deposit.Splits ?? [])
                .Where(split => Math.Abs(RoundCurrency(split.Amount)) > 0.005m)
                .ToList();

            var lineAmount = RoundCurrency(contextLine.EscrowAmount);
            var depositAmount = RoundCurrency(deposit.Amount);
            var splitTotal = paymentSplits.Count > 0
                ? RoundCurrency(paymentSplits.Sum(split => split.Amount))
                : 0m;

            var shouldExpandToPaymentSplits = paymentSplits.Count > 1 && (
                Math.Abs(lineAmount - depositAmount) <= 0.005m
                || Math.Abs(lineAmount - splitTotal) <= 0.005m);

            if (shouldExpandToPaymentSplits)
            {
                foreach (var split in paymentSplits)
                {
                    var escrowAmount = RoundTransferEscrowAmount(split.Amount);
                    if (Math.Abs(escrowAmount) <= 0.005m)
                        continue;

                    workItems.Add(new TransferAllocationWorkItem
                    {
                        ContextLine = contextLine,
                        AllocationJournalEntryLineId = split.JournalEntryLineId ?? Guid.Empty,
                        EscrowAmount = escrowAmount,
                        DepositSplit = split
                    });
                }

                continue;
            }

            var matchingSplit = paymentSplits.FirstOrDefault(split =>
                Math.Abs(RoundCurrency(split.Amount) - lineAmount) <= 0.005m
                || split.JournalEntryLineId == contextLine.JournalEntryLineId);

            workItems.Add(new TransferAllocationWorkItem
            {
                ContextLine = contextLine,
                AllocationJournalEntryLineId = matchingSplit?.JournalEntryLineId ?? contextLine.JournalEntryLineId,
                EscrowAmount = matchingSplit != null ? RoundCurrency(matchingSplit.Amount) : lineAmount,
                DepositSplit = matchingSplit
            });
        }

        return workItems;
    }

    private static List<TransferSplit> BuildTransferSplitsFromDepositAllocations(
        IReadOnlyList<TransferAllocationWorkItem> workItems,
        IReadOnlyList<TransferDepositAllocationResult> allocations,
        TransferAllocationAccountIds accountIds)
    {
        var allocationByKey = allocations.ToDictionary(
            allocation => BuildTransferAllocationMatchKey(allocation.DepositId, allocation.EscrowAmount, allocation.JournalEntryLineId),
            allocation => allocation);

        var splits = new List<TransferSplit>();
        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var workItem in workItems)
        {
            var depositId = workItem.ContextLine.Deposit.DepositId;
            var resolvedAllocations = ResolveTransferDepositAllocationsForWorkItem(workItem, depositId, allocationByKey, allocations);
            if (resolvedAllocations.Count == 0)
                throw new InvalidOperationException($"Unable to resolve deposit allocation for {workItem.ContextLine.Deposit.DepositCode}.");

            foreach (var resolved in resolvedAllocations)
            {
                var matchKey = BuildTransferAllocationMatchKey(
                    depositId,
                    resolved.EscrowAmount,
                    resolved.Allocation.JournalEntryLineId);
                var contextKey = $"{workItem.ContextLine.JournalEntryLineId}|{matchKey}";
                if (string.IsNullOrWhiteSpace(matchKey) || processedKeys.Contains(contextKey))
                    continue;

                processedKeys.Add(contextKey);
                splits.AddRange(BuildTransferSplitsFromAllocation(
                    resolved.Allocation,
                    resolved.EscrowAmount,
                    workItem.ContextLine,
                    accountIds,
                    resolved.DepositSplit));
            }
        }

        return splits;
    }

    private static List<ResolvedTransferAllocation> ResolveTransferDepositAllocationsForWorkItem(
        TransferAllocationWorkItem workItem,
        Guid depositId,
        IReadOnlyDictionary<string, TransferDepositAllocationResult> allocationByKey,
        IReadOnlyList<TransferDepositAllocationResult> allocations)
    {
        var directMatchKey = BuildTransferAllocationMatchKey(
            depositId,
            workItem.EscrowAmount,
            workItem.AllocationJournalEntryLineId);
        if (allocationByKey.TryGetValue(directMatchKey, out var directMatch))
        {
            return
            [
                new ResolvedTransferAllocation(directMatch, workItem.DepositSplit, workItem.EscrowAmount)
            ];
        }

        var amountOnlyKey = BuildTransferAllocationMatchKey(depositId, workItem.EscrowAmount, null);
        if (allocationByKey.TryGetValue(amountOnlyKey, out var amountOnlyMatch))
        {
            return
            [
                new ResolvedTransferAllocation(amountOnlyMatch, workItem.DepositSplit, workItem.EscrowAmount)
            ];
        }

        var workAmount = RoundCurrency(workItem.EscrowAmount);
        var amountMatch = allocations.FirstOrDefault(item =>
            item.DepositId == depositId
            && Math.Abs(RoundCurrency(item.EscrowAmount) - workAmount) <= 0.005m);
        if (amountMatch != null)
        {
            return
            [
                new ResolvedTransferAllocation(amountMatch, workItem.DepositSplit, workItem.EscrowAmount)
            ];
        }

        var depositAllocations = allocations.Where(item => item.DepositId == depositId).ToList();
        if (depositAllocations.Count <= 1)
            return [];

        var allocationTotal = RoundCurrency(depositAllocations.Sum(item => item.EscrowAmount));
        if (Math.Abs(workAmount - allocationTotal) > 0.005m)
            return [];

        return depositAllocations
            .Select(allocation =>
            {
                var escrowAmount = RoundCurrency(allocation.EscrowAmount);
                var depositSplit = (workItem.ContextLine.Deposit.Splits ?? []).FirstOrDefault(split =>
                    Math.Abs(RoundCurrency(split.Amount) - escrowAmount) <= 0.005m
                    || split.JournalEntryLineId == allocation.JournalEntryLineId);

                return new ResolvedTransferAllocation(allocation, depositSplit, escrowAmount);
            })
            .ToList();
    }

    private static List<TransferSplit> BuildTransferSplitsFromAllocation(
        TransferDepositAllocationResult allocation,
        decimal baseAmount,
        TransferEscrowLineContext contextLine,
        TransferAllocationAccountIds accountIds,
        DepositSplit? depositSplit)
    {
        _ = baseAmount;

        var ownerEscrow = RoundCurrency(allocation.OwnerEscrow);
        var secDep = RoundCurrency(allocation.SecDep);
        var sdw = RoundCurrency(allocation.Sdw);
        var business = RoundCurrency(allocation.Business);

        var propertyId = depositSplit?.PropertyId ?? allocation.PropertyId;
        var reservationId = depositSplit?.ReservationId ?? allocation.ReservationId;
        var contactId = depositSplit?.ContactId ?? allocation.ContactId;
        var journalEntryLineId = depositSplit?.JournalEntryLineId
            ?? allocation.JournalEntryLineId
            ?? contextLine.JournalEntryLineId;

        var source = (allocation.Description ?? ExtractTransferSourceLabel(depositSplit?.Description) ?? depositSplit?.Description ?? string.Empty).Trim();
        var description = source.Length > 0 ? $"Transfer to Escrow Accounts - {source}" : "Transfer to Escrow Accounts";

        var splitDefinitions = new (decimal Amount, int? AccountId)[]
        {
            (ownerEscrow, accountIds.Owners),
            (secDep, accountIds.SecDep),
            (sdw, accountIds.Sdw),
            (business, accountIds.Bank)
        };

        var splits = new List<TransferSplit>();
        foreach (var (amount, accountId) in splitDefinitions)
        {
            var normalizedAmount = RoundCurrency(amount);
            if (Math.Abs(normalizedAmount) <= 0.005m || accountId is null or <= 0)
                continue;

            splits.Add(new TransferSplit
            {
                Amount = normalizedAmount,
                Description = description,
                PropertyId = propertyId,
                ReservationId = reservationId,
                ContactId = contactId,
                JournalEntryLineId = journalEntryLineId,
                ChartOfAccountId = accountId
            });
        }

        return splits;
    }

    private static string? ValidateBuiltTransferSplits(IReadOnlyList<TransferSplit> splits, TransferAllocationAccountIds accountIds)
    {
        if (splits.Count == 0)
            return "Transfer splits could not be built from the selected deposits.";

        if (accountIds.Owners is null or <= 0 || accountIds.Bank is null or <= 0)
            return "Owner escrow and bank accounts must be configured for this office.";

        if (splits.Any(split => split.JournalEntryLineId is null || split.JournalEntryLineId == Guid.Empty))
            return "Each transfer split must include a journal entry line id before create.";

        return null;
    }

    private static TransferAllocationAccountIds ResolveTransferAllocationAccountIds(AccountingOffice accountingOffice)
        => new(
            accountingOffice.DefaultEscrowOwnersAccountId,
            accountingOffice.DefaultEscrowSecDepAccountId,
            accountingOffice.DefaultEscrowSdwAccountId,
            accountingOffice.DefaultBankAccountId);

    private static string BuildTransferAllocationMatchKey(Guid depositId, decimal escrowAmount, Guid? journalEntryLineId)
    {
        var roundedAmount = RoundTransferEscrowAmount(escrowAmount);
        return journalEntryLineId is { } lineId && lineId != Guid.Empty
            ? $"{depositId:N}|{roundedAmount}|{lineId:N}"
            : $"{depositId:N}|{roundedAmount}";
    }

    private static string ExtractTransferSourceLabel(string? description)
    {
        var normalized = (description ?? string.Empty).Trim();
        if (normalized.Length == 0)
            return string.Empty;

        var colonIndex = normalized.IndexOf(':');
        return colonIndex > 0 ? normalized[..colonIndex].Trim() : normalized;
    }

    private static int? ParseDocumentCodeInt(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var match = Regex.Match(code, @"\d+");
        return match.Success && int.TryParse(match.Value, out var value) ? value : null;
    }

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundTransferEscrowAmount(decimal escrowAmount)
        => RoundCurrency(escrowAmount);

    private static AppSettings LoadAppSettings()
    {
        var connectionString = Environment.GetEnvironmentVariable("RENTALL_DB_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new AppSettings
            {
                DbConnections =
                [
                    new DbConnection { DbName = "RentAll", ConnectionString = connectionString.Trim() }
                ]
            };
        }

        foreach (var path in GetAppsettingsCandidates())
        {
            if (!File.Exists(path))
                continue;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("AppSettings", out var appSettingsElement))
                continue;

            if (!appSettingsElement.TryGetProperty("DbConnections", out var connectionsElement)
                || connectionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var connections = new List<DbConnection>();
            foreach (var connectionElement in connectionsElement.EnumerateArray())
            {
                var dbName = connectionElement.TryGetProperty("DbName", out var dbNameElement)
                    ? dbNameElement.GetString()
                    : null;
                var configuredConnectionString = connectionElement.TryGetProperty("ConnectionString", out var connectionElementValue)
                    ? connectionElementValue.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(configuredConnectionString))
                    continue;

                connections.Add(new DbConnection
                {
                    DbName = dbName,
                    ConnectionString = configuredConnectionString
                });
            }

            if (connections.Count > 0)
                return new AppSettings { DbConnections = connections };
        }

        throw new InvalidOperationException(
            "Could not load RentAll DB connection. Set RENTALL_DB_CONNECTION_STRING or ensure RentAll.Api/appsettings.json is present.");
    }

    private static IEnumerable<string> GetAppsettingsCandidates()
    {
        var startDirectory = AppContext.BaseDirectory;
        var current = new DirectoryInfo(startDirectory);
        for (var depth = 0; depth < 8 && current != null; depth++, current = current.Parent)
        {
            yield return Path.Combine(current.FullName, "appsettings.json");
            yield return Path.Combine(current.FullName, "RentAll.Api", "appsettings.json");
            yield return Path.Combine(current.FullName, "RentAll.Api", "RentAll.Api", "appsettings.json");
        }
    }

    private static string GetConnectionString()
    {
        var settings = LoadAppSettings();
        return settings.DbConnections
            .First(connection => connection.DbName.Equals("rentall", StringComparison.OrdinalIgnoreCase))
            .ConnectionString;
    }

    private sealed record TransferAllocationWorkItem
    {
        public required TransferEscrowLineContext ContextLine { get; init; }
        public required Guid AllocationJournalEntryLineId { get; init; }
        public required decimal EscrowAmount { get; init; }
        public DepositSplit? DepositSplit { get; init; }
    }

    private sealed record ResolvedTransferAllocation(
        TransferDepositAllocationResult Allocation,
        DepositSplit? DepositSplit,
        decimal EscrowAmount);

    private sealed record TransferAllocationAccountIds(int? Owners, int? SecDep, int? Sdw, int? Bank);

    private sealed class IntegrationEnabledFeatureFlagService : IFeatureFlagService
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
