using RentAll.Domain.Managers;

namespace RentAll.Test;

public class AccountingManagerTransferSplitReconciliationTests
{
    [Fact]
    public async Task CreateTransferAsync_Dp156Py953AndR093SharedEscrowLine_PassesValidationBeforeInsert()
    {
        var manager = TransferSplitReconciliationTestSupport.CreateManager(out _);
        var transfer = TransferSplitReconciliationTestSupport.BuildDp156TransferWithPy953AndR093Splits();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.CreateTransferAsync(transfer, TransferSplitReconciliationTestSupport.CurrentUser));

        Assert.Equal("VALIDATION_PASSED", ex.Message);
        Assert.All(transfer.Splits ?? [], split =>
        {
            Assert.NotNull(split.JournalEntryLineId);
            Assert.NotEqual(Guid.Empty, split.JournalEntryLineId);
        });
        Assert.Equal(
            TransferSplitReconciliationTestSupport.R093ArLineId,
            transfer.Splits!.First(split =>
                (split.Description ?? string.Empty).Contains("R-000000093", StringComparison.OrdinalIgnoreCase)).JournalEntryLineId);
        Assert.Equal(
            TransferSplitReconciliationTestSupport.Payment953UfLineId,
            transfer.Splits!.First(split =>
                (split.Description ?? string.Empty).Contains("PY-000000953", StringComparison.OrdinalIgnoreCase)).JournalEntryLineId);

        var r093Split = transfer.Splits!.First(split =>
            (split.Description ?? string.Empty).Contains("R-000000093", StringComparison.OrdinalIgnoreCase));
        var py953Split = transfer.Splits!.First(split =>
            (split.Description ?? string.Empty).Contains("PY-000000953", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(TransferSplitReconciliationTestSupport.R093PropertyId, r093Split.PropertyId);
        Assert.Null(py953Split.PropertyId);
        Assert.Equal(7750m, Math.Abs(r093Split.SourceJournalEntryLineAmount ?? 0m));
        Assert.Equal(4060m, Math.Abs(py953Split.SourceJournalEntryLineAmount ?? 0m));
    }

    /// <summary>
    /// When payment is not stamped to the deposit and payment JEs carry no deposit link, rematch/validation
    /// blocks create before SyncDepositTransferIds (post-insert "deposit link" failure is no longer reached).
    /// </summary>
    [Fact]
    public async Task CreateTransferAsync_Dp156PersistedUfLines_FailsBeforeJournalEntryLikeProduction()
    {
        var (manager, _, probe) = TransferSplitReconciliationTestSupport.CreateManagerForPostInsertFailureProbe(
            paymentHasDepositStamp: false);
        var transfer = TransferSplitReconciliationTestSupport.BuildDp156TransferWithDistinctUfLineIds();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            manager.CreateTransferAsync(transfer, TransferSplitReconciliationTestSupport.CurrentUser));

        Assert.Contains("escrow deposit journal entry line", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(probe.CreateTransferInvoked, "Create should not run when pre-insert split validation fails.");
        Assert.False(probe.SetDepositTransferIdInvoked);
        Assert.False(probe.CreateJournalEntryInvoked);
    }
}
