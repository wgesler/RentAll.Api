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
            Assert.Equal(TransferSplitReconciliationTestSupport.EscrowLineId, split.JournalEntryLineId);
        });

        var r093Split = transfer.Splits!.First(split =>
            (split.Description ?? string.Empty).Contains("R-000000093", StringComparison.OrdinalIgnoreCase));
        var py953Split = transfer.Splits!.First(split =>
            (split.Description ?? string.Empty).Contains("PY-000000953", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(TransferSplitReconciliationTestSupport.R093PropertyId, r093Split.PropertyId);
        Assert.Equal(TransferSplitReconciliationTestSupport.Py953PropertyId, py953Split.PropertyId);
        Assert.Null(r093Split.SourceJournalEntryLineAmount);
        Assert.Null(py953Split.SourceJournalEntryLineAmount);
    }

    /// <summary>
    /// Production fingerprint: header+splits saved, SyncDepositTransferIds cannot resolve deposits
    /// from persisted payment/reservation UF lines, no deposit stamp, no transfer JE.
    /// Matches Fix_FailedTransfer_NoJe.sql and prod exception at CollectDepositIdsFromTransferSplitsAsync.
    /// </summary>
    [Fact]
    public async Task CreateTransferAsync_Dp156PersistedUfLines_FailsBeforeJournalEntryLikeProduction()
    {
        var (manager, _, probe) = TransferSplitReconciliationTestSupport.CreateManagerForPostInsertFailureProbe(
            paymentHasDepositStamp: false);
        var transfer = TransferSplitReconciliationTestSupport.BuildDp156TransferWithDistinctUfLineIds();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            manager.CreateTransferAsync(transfer, TransferSplitReconciliationTestSupport.CurrentUser));

        Assert.Contains("deposit link", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(probe.CreateTransferInvoked, "Transfer header+splits should be persisted before the failure.");
        Assert.False(probe.SetDepositTransferIdInvoked, "No Deposit.TransferId stamp should occur (Fix_FailedTransfer_NoJe fingerprint).");
        Assert.False(probe.CreateJournalEntryInvoked, "No transfer JE should be created.");
        Assert.Equal(
            TransferSplitReconciliationTestSupport.EscrowLineId,
            transfer.Splits!.First().JournalEntryLineId);
        Assert.All(probe.PersistedSplitLineIds, lineId =>
        {
            Assert.NotEqual(TransferSplitReconciliationTestSupport.EscrowLineId, lineId);
        });
    }
}
