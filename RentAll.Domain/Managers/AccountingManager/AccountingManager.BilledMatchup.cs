using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task RebuildReservationBilledMatchupAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return;

        var activeReservations = (await _reservationRepository.GetActiveReservationsByOfficeIdsAsync(organizationId, officeIds))
            .ToList();

        var accountingOfficesByOfficeId = (await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(organizationId, officeIds))
            .ToDictionary(office => office.OfficeId);

        var existingInvoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludePaid = true
        }))
            .Where(invoice => invoice.ReservationId.HasValue && invoice.ReservationId.Value != Guid.Empty)
            .Where(invoice => accountingOfficesByOfficeId.TryGetValue(invoice.OfficeId, out var invoiceOffice)
                && RentalFeeLineParser.IsAccountingPeriodOnOrAfterInvoiceStart(
                    invoice,
                    AccountingOfficePeriodBoundary.GetInvoiceStart(invoiceOffice)))
            .ToList();

        var invoicesByReservationId = existingInvoices
            .GroupBy(invoice => invoice.ReservationId!.Value)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Invoice>)group.ToList());

        var asOfDate = DateOnly.FromDateTime(DateTime.Today);

        foreach (var reservation in activeReservations)
        {
            invoicesByReservationId.TryGetValue(reservation.ReservationId, out var reservationInvoices);
            accountingOfficesByOfficeId.TryGetValue(reservation.OfficeId, out var accountingOffice);
            var existingBilledRows = await _accountingRepository.GetBilledByReservationIdAsync(organizationId, reservation.ReservationId);
            var ignoreByMonth = existingBilledRows.ToDictionary(row => row.MonthStart, row => row.Ignore);

            var billedRows = BuildBilledMatchupMonthlyRows(
                reservation,
                organizationId,
                currentUser,
                reservationInvoices,
                asOfDate,
                accountingOffice);
            foreach (var billedRow in billedRows)
            {
                if (ignoreByMonth.TryGetValue(billedRow.MonthStart, out var ignore))
                    billedRow.Ignore = ignore;
            }

            await _accountingRepository.ReplaceBilledMonthlyRowsForReservationAsync(
                organizationId,
                reservation.ReservationId,
                billedRows);
        }

        var activeReservationIds = activeReservations
            .Select(reservation => reservation.ReservationId)
            .ToList();
        await _accountingRepository.DeleteBilledByOrganizationAndOfficeIdsExceptReservationsAsync(
            organizationId,
            officeIds,
            activeReservationIds);
    }

    public Task<IReadOnlyList<Billed>> GetBilledMatchupAsync(Guid organizationId, string officeIds)
        => GetBilledMatchupInternalAsync(organizationId, officeIds);

    public async Task<IReadOnlyList<Billed>> GetBilledByReservationIdAsync(Guid organizationId, Guid reservationId)
        => await _accountingRepository.GetBilledByReservationIdAsync(organizationId, reservationId);

    public async Task<Billed> CreateBilledAsync(Billed billed, Guid currentUser)
    {
        billed.CreatedBy = currentUser;
        billed.ModifiedBy = currentUser;
        return await _accountingRepository.CreateBilledAsync(billed);
    }

    public async Task<Billed?> UpdateBilledByReservationIdAsync(Billed billed, Guid currentUser)
    {
        billed.ModifiedBy = currentUser;
        return await _accountingRepository.UpdateBilledByReservationIdAsync(billed);
    }

    public Task DeleteBilledByReservationIdAsync(Guid organizationId, Guid reservationId)
        => _accountingRepository.DeleteBilledByReservationIdAsync(organizationId, reservationId);

    public Task<Billed?> SetBilledIgnoreByIdAsync(Guid organizationId, int billedId, bool ignore, Guid currentUser)
        => _accountingRepository.SetBilledIgnoreByIdAsync(organizationId, billedId, ignore, currentUser);

    private async Task<IReadOnlyList<Billed>> GetBilledMatchupInternalAsync(Guid organizationId, string officeIds)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return Array.Empty<Billed>();

        var rows = await _accountingRepository.GetBilledByOrganizationAndOfficeIdsAsync(organizationId, officeIds);
        return rows;
    }
}
