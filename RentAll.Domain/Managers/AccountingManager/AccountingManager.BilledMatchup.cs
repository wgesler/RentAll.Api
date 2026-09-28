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

        var existingInvoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludePaid = true
        }))
            .Where(invoice => invoice.ReservationId.HasValue && invoice.ReservationId.Value != Guid.Empty)
            .ToList();

        var invoicesByReservationId = existingInvoices
            .GroupBy(invoice => invoice.ReservationId!.Value)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Invoice>)group.ToList());

        var asOfDate = DateOnly.FromDateTime(DateTime.Today);
        var accountingOfficesByOfficeId = (await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(organizationId, officeIds))
            .ToDictionary(office => office.OfficeId);

        foreach (var reservation in activeReservations)
        {
            invoicesByReservationId.TryGetValue(reservation.ReservationId, out var reservationInvoices);
            accountingOfficesByOfficeId.TryGetValue(reservation.OfficeId, out var accountingOffice);
            var billed = BuildBilledMatchupRow(
                reservation,
                organizationId,
                currentUser,
                reservationInvoices,
                asOfDate,
                accountingOffice);
            await _accountingRepository.UpsertBilledByReservationIdAsync(billed);
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

    public Task<Billed?> GetBilledByReservationIdAsync(Guid organizationId, Guid reservationId)
        => _accountingRepository.GetBilledByReservationIdAsync(organizationId, reservationId);

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

    private async Task<IReadOnlyList<Billed>> GetBilledMatchupInternalAsync(Guid organizationId, string officeIds)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return Array.Empty<Billed>();

        var rows = await _accountingRepository.GetBilledByOrganizationAndOfficeIdsAsync(organizationId, officeIds);
        return rows;
    }
}
