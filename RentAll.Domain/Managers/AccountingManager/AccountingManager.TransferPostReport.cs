using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<Transfer> PostTransferReportAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        if (transferId == Guid.Empty)
            throw new ArgumentException("TransferId is required");

        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found");

        if (!transfer.IsActive)
            throw new Exception("Transfer is inactive");

        if (transfer.PostingStatusId == (int)PostingStatus.HardClosed)
            throw new Exception("Transfer is hard closed and cannot be posted");

        if (transfer.PostingStatusId == (int)PostingStatus.SoftClosed)
            throw new Exception("Transfer is soft closed and cannot be posted");

        if (transfer.HasBeenTransfered)
            throw new Exception("Transfer has already been transfered");

        ValidateTransferForJournalEntry(transfer);

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
            throw new Exception("Default escrow deposit account is not configured for this office");

        transfer.BankAccountId = escrowDepositAccountId;
        transfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(transfer);

        var refreshedTransfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found after update");

        await TryReplaceJournalEntriesFromTransferAsync(refreshedTransfer, currentUser);

        refreshedTransfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found after journal entry refresh");

        var transferJournalEntries = await GetJournalEntriesForSourceAsync(
            refreshedTransfer.OrganizationId,
            refreshedTransfer.OfficeId,
            SourceType.Transfer,
            transferId);
        if (!transferJournalEntries.Any(entry => entry.JournalEntryId != Guid.Empty))
            throw new Exception("Unable to create transfer journal entry");

        refreshedTransfer.HasBeenTransfered = true;
        refreshedTransfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(refreshedTransfer);

        return await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? refreshedTransfer;
    }
}
