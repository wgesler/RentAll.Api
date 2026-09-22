using RentAll.Domain.Models;

namespace RentAll.Test;

/// <summary>
/// Full integration test for GL Make Transfer using real stored procedures on the local GESLER database.
/// Run: set RENTALL_INTEGRATION_TESTS=1 && dotnet test --filter AccountingManagerTransferCreateIntegrationTests
/// </summary>
public class AccountingManagerTransferCreateIntegrationTests
{
    [Fact]
    public async Task CreateTransfer_SelectedDeposits137156142141_SucceedsViaStoredProcs()
    {
        TransferCreateIntegrationTestSupport.EnsureEnabled();

        var services = TransferCreateIntegrationTestSupport.CreateServices();
        var scenario = await TransferCreateIntegrationTestSupport.LoadScenarioAsync(
            services.AccountingRepository,
            services.OrganizationRepository,
            services.JournalEntryRepository);

        var currentUser = TransferCreateIntegrationTestSupport.IntegrationTestUserId;
        var transfer = await TransferCreateIntegrationTestSupport.BuildTransferModelAsync(
            services.AccountingManager,
            services.OrganizationManager,
            scenario,
            currentUser);

        Assert.NotEmpty(transfer.Splits);
        Assert.True(transfer.Amount > 0m);

        Transfer? created = null;
        try
        {
            created = await services.AccountingManager.CreateTransferAsync(transfer, currentUser);

            Assert.NotEqual(Guid.Empty, created.TransferId);
            Assert.False(string.IsNullOrWhiteSpace(created.TransferCode));
            Assert.All(created.Splits ?? [], split =>
            {
                Assert.NotNull(split.JournalEntryLineId);
                Assert.NotEqual(Guid.Empty, split.JournalEntryLineId);
            });

            var dp156EscrowLineId = scenario.EscrowLines
                .First(context => ParseDepositInt(context.Deposit.DepositCode) == TransferCreateIntegrationTestSupport.SelectedDepositInts.First(depositInt => depositInt == 156))
                .JournalEntryLineId;

            var dp156Splits = (created.Splits ?? [])
                .Where(split => (split.Description ?? string.Empty).Contains("093", StringComparison.OrdinalIgnoreCase)
                    || (split.Description ?? string.Empty).Contains("953", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(dp156Splits);
            Assert.All(dp156Splits, split => Assert.Equal(dp156EscrowLineId, split.JournalEntryLineId));

            var py953Split = (created.Splits ?? [])
                .FirstOrDefault(split => (split.Description ?? string.Empty).Contains("PY-000000953", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(py953Split);
            Assert.Equal(4060m, py953Split.Amount);

            var health = await services.HealthRepository.RunTransferHealthCheckAsync(scenario.OrganizationId, scenario.OfficeId.ToString());
            var transferIssues = (health.Issues ?? [])
                .Where(issue => issue.DocumentId == created.TransferId)
                .ToList();
            Assert.Empty(transferIssues);

            var reloaded = await services.AccountingRepository.GetTransferByIdAsync(created.TransferId, scenario.OrganizationId);
            Assert.NotNull(reloaded);
            Assert.Equal(created.TransferCode, reloaded.TransferCode);
            Assert.True((reloaded.Splits ?? []).Count > 0);
        }
        finally
        {
            if (created != null)
                await services.AccountingManager.DeleteTransferAsync(created.TransferId, scenario.OrganizationId, currentUser);
        }
    }

    [Fact]
    public async Task SqlPreflight_SelectedDepositsAndPy953Path_IsReady()
    {
        TransferCreateIntegrationTestSupport.EnsureEnabled();

        var services = TransferCreateIntegrationTestSupport.CreateServices();
        var scenario = await TransferCreateIntegrationTestSupport.LoadScenarioAsync(
            services.AccountingRepository,
            services.OrganizationRepository,
            services.JournalEntryRepository);

        await TransferCreateIntegrationTestSupport.AssertPy953RematchPathReadyAsync(
            scenario.OrganizationId,
            scenario.AccountingOffice.DefaultEscrowDepositAccountId
                ?? throw new InvalidOperationException("Escrow deposit account is not configured."));

        Assert.Equal(4, scenario.Deposits.Count);
        Assert.Equal(TransferCreateIntegrationTestSupport.SelectedDepositInts.Length, scenario.EscrowLines.Count);
        Assert.All(scenario.Deposits, deposit =>
        {
            Assert.Null(deposit.TransferId);
            Assert.True(string.IsNullOrWhiteSpace(deposit.TransferCode));
        });
    }

    private static int? ParseDepositInt(string depositCode)
    {
        var digits = new string(depositCode.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : null;
    }
}
