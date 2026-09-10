using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<CloseOwnerStatementMonthResult> CloseOwnerStatementMonthAsync(
        Guid organizationId,
        DateOnly endDate,
        IReadOnlyList<OwnerStatementMonthCloseLine> lines,
        Guid currentUser)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            throw new Exception("Accounting is not enabled for this organization.");

        if (endDate == default)
            throw new Exception("End date is required to close an owner statement month.");

        if (lines == null || lines.Count == 0)
            throw new Exception("At least one owner statement line is required to close the month.");

        return new CloseOwnerStatementMonthResult
        {
            PropertiesProcessed = lines.Count
        };
    }

    public async Task<CloseAccountingPeriodResult> SoftCloseOwnerApJournalEntriesForOwnerStatementMonthAsync(Guid organizationId, DateOnly endDate, IReadOnlyList<OwnerStatementMonthCloseLine> lines, Guid currentUser)
    {
        var result = new CloseAccountingPeriodResult();
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return result;

        if (endDate == default)
            throw new Exception("End date is required to soft close owner AP journal entries.");

        if (lines == null || lines.Count == 0)
            return result;

        var closedPropertyKeys = lines
            .Where(line => line.PropertyId != Guid.Empty && line.OfficeId > 0)
            .Select(line => GetOwnerStatementPropertyKey(line.OfficeId, line.PropertyId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (closedPropertyKeys.Count == 0)
            return result;

        var officeIds = lines.Select(line => line.OfficeId).Where(officeId => officeId > 0).Distinct().ToList();
        var ownerApLines = await SearchOwnerApAgingJournalEntryLinesAsync(organizationId, officeIds, endDate, includeUnposted: true);
        var journalEntryIds = ownerApLines
            .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
            .Where(line => closedPropertyKeys.Contains(GetOwnerStatementPropertyKey(line.OfficeId, line.PropertyId!.Value)))
            .Where(line => line.TransactionDate <= endDate)
            .Where(line => line.PostingStatusId is (int)PostingStatus.Open or (int)PostingStatus.Posted)
            .Select(line => line.JournalEntryId)
            .Distinct()
            .ToList();

        foreach (var journalEntryId in journalEntryIds)
        {
            try
            {
                await SoftCloseJournalEntryAsync(journalEntryId, organizationId, currentUser);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(ex.Message);
            }
        }

        return result;
    }

    private static string GetOwnerStatementPropertyKey(int officeId, Guid propertyId)
        => $"{officeId:D}|{propertyId:D}";
}
