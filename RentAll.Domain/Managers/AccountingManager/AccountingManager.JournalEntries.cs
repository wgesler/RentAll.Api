using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry)
    {
        journalEntry.IsPosted = false;
        journalEntry.IsVoided = false;

        if (string.IsNullOrWhiteSpace(journalEntry.JournalEntryCode))
        {
            journalEntry.JournalEntryCode = await _organizationManager.GenerateEntityCodeAsync(
                journalEntry.OrganizationId,
                EntityType.JournalEntry);
        }

        ValidateJournalEntryForSave(journalEntry, requireActiveLines: true);
        return await _journalEntryRepository.CreateJournalEntryAsync(journalEntry);
    }

    public async Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry journalEntry)
    {
        var existing = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntry.JournalEntryId, journalEntry.OrganizationId);
        if (existing == null)
            throw new Exception("Journal entry not found");

        EnsureJournalEntryIsEditable(existing);

        journalEntry.JournalEntryCode = existing.JournalEntryCode;
        journalEntry.SourceTypeId = existing.SourceTypeId;
        journalEntry.SourceId = existing.SourceId;
        journalEntry.IsPosted = existing.IsPosted;
        journalEntry.IsVoided = existing.IsVoided;
        journalEntry.CreatedBy = existing.CreatedBy;

        ValidateJournalEntryForSave(journalEntry, requireActiveLines: true);
        return await _journalEntryRepository.UpdateJournalEntryByIdAsync(journalEntry);
    }

    public async Task<JournalEntry> PostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry == null)
            throw new Exception("Journal entry not found");

        if (journalEntry.IsVoided)
            throw new Exception("A voided journal entry cannot be posted");

        if (journalEntry.IsPosted)
            throw new Exception("Journal entry is already posted");

        ValidateJournalEntryForSave(journalEntry, requireActiveLines: true);

        journalEntry.IsPosted = true;
        if (journalEntry.PostingDate == default)
            journalEntry.PostingDate = DateOnly.FromDateTime(DateTime.UtcNow);

        journalEntry.ModifiedBy = currentUser;
        return await _journalEntryRepository.UpdateJournalEntryByIdAsync(journalEntry);
    }

    public async Task<JournalEntry> VoidJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry == null)
            throw new Exception("Journal entry not found");

        if (journalEntry.IsVoided)
            throw new Exception("Journal entry is already voided");

        if (!journalEntry.IsPosted)
            throw new Exception("Only posted journal entries can be voided");

        journalEntry.IsVoided = true;
        journalEntry.ModifiedBy = currentUser;
        return await _journalEntryRepository.UpdateJournalEntryByIdAsync(journalEntry);
    }

    public async Task DeleteJournalEntryAsync(Guid journalEntryId, Guid organizationId)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry == null)
            throw new Exception("Journal entry not found");

        if (journalEntry.IsPosted)
            throw new Exception("Posted journal entries cannot be deleted");

        EnsureJournalEntryIsEditable(journalEntry);
        await _journalEntryRepository.DeleteJournalEntryByIdAsync(journalEntryId, organizationId);
    }

    public async Task<JournalEntry> UnpostJournalEntryAsync(Guid journalEntryId, Guid organizationId, Guid currentUser)
    {
        var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
        if (journalEntry == null)
            throw new Exception("Journal entry not found");

        if (journalEntry.IsVoided)
            throw new Exception("A voided journal entry cannot be unposted");

        if (!journalEntry.IsPosted)
            throw new Exception("Journal entry is not posted");

        journalEntry.IsPosted = false;
        journalEntry.ModifiedBy = currentUser;
        return await _journalEntryRepository.UpdateJournalEntryByIdAsync(journalEntry);
    }

    static void EnsureJournalEntryIsEditable(JournalEntry journalEntry)
    {
        if (journalEntry.IsVoided)
            throw new Exception("Voided journal entries cannot be changed");
    }

    static void ValidateJournalEntryForSave(JournalEntry journalEntry, bool requireActiveLines)
    {
        if (journalEntry.OfficeId <= 0)
            throw new Exception("OfficeId is required");

        if (journalEntry.TransactionDate == default)
            throw new Exception("TransactionDate is required");

        if (journalEntry.PostingDate == default)
            throw new Exception("PostingDate is required");

        var activeLines = journalEntry.JournalEntryLines
            .Where(l => l.Debit != 0 || l.Credit != 0)
            .ToList();

        if (requireActiveLines && activeLines.Count == 0)
            throw new Exception("At least one journal entry line is required");

        foreach (var line in activeLines)
        {
            if (line.ChartOfAccountId <= 0)
                throw new Exception("ChartOfAccountId is required on each journal entry line");

            if (line.Debit < 0 || line.Credit < 0)
                throw new Exception("Debit and Credit must be zero or greater");

            if (line.Debit != 0 && line.Credit != 0)
                throw new Exception("A journal entry line cannot have both debit and credit amounts");
        }

        var totalDebit = activeLines.Sum(l => l.Debit);
        var totalCredit = activeLines.Sum(l => l.Credit);

        if (Math.Abs(totalDebit - totalCredit) > 0.005m)
            throw new Exception("Journal entry debits and credits must balance");
    }
}
