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
        if (!TransferCreateIntegrationTestSupport.EnsureEnabled())
            return;

        var services = TransferCreateIntegrationTestSupport.CreateServices();
        var scenario = await TransferCreateIntegrationTestSupport.TryLoadScenarioAsync(services);

        var currentUser = TransferCreateIntegrationTestSupport.IntegrationTestUserId;
        var transfer = await TransferCreateIntegrationTestSupport.BuildTransferModelAsync(
            services.AccountingManager,
            services.OrganizationManager,
            scenario,
            currentUser);

        Assert.NotEmpty(transfer.Splits);
        Assert.NotEqual(0m, transfer.Amount);

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

            foreach (var escrowContext in scenario.EscrowLines)
            {
                var depositCode = escrowContext.Deposit.DepositCode ?? string.Empty;
                var relatedSplits = (created.Splits ?? [])
                    .Where(split => (split.Description ?? string.Empty).Contains(depositCode, StringComparison.OrdinalIgnoreCase)
                        || split.JournalEntryLineId == escrowContext.JournalEntryLineId)
                    .ToList();
                if (relatedSplits.Count == 0)
                    continue;

                Assert.All(relatedSplits, split => Assert.Equal(escrowContext.JournalEntryLineId, split.JournalEntryLineId));
            }

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
            {
                await services.AccountingManager.DeleteTransferAsync(created.TransferId, scenario.OrganizationId, currentUser);
                await TransferCreateIntegrationTestSupport.AssertScenarioDepositsUnlinkedAfterTransferDeleteAsync(
                    services.AccountingRepository,
                    scenario);
            }
        }
    }

    [Fact]
    public async Task SqlPreflight_SelectedDepositsAndPy953Path_IsReady()
    {
        if (!TransferCreateIntegrationTestSupport.EnsureEnabled())
            return;

        var services = TransferCreateIntegrationTestSupport.CreateServices();
        var scenario = await TransferCreateIntegrationTestSupport.TryLoadScenarioAsync(services);

        await TransferCreateIntegrationTestSupport.AssertScenarioPaymentRematchPathsReadyAsync(
            scenario.OrganizationId,
            scenario.OfficeId,
            scenario.AccountingOffice.DefaultEscrowDepositAccountId
                ?? throw new InvalidOperationException("Escrow deposit account is not configured."),
            scenario.Deposits);

        Assert.InRange(scenario.Deposits.Count, TransferCreateIntegrationTestSupport.MinDepositsPerIntegrationRun, TransferCreateIntegrationTestSupport.TargetDepositsPerIntegrationRun);
        Assert.Equal(scenario.Deposits.Count, scenario.EscrowLines.Count);
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
