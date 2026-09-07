using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Reservations;

public partial class ReservationRepository
{
    public async Task<IEnumerable<ReservationPayment>> GetReservationPaymentsByReservationIdAsync(Guid organizationId, Guid reservationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationPaymentEntity>("Property.ReservationPayment_GetByReservationId", new
        {
            OrganizationId = organizationId,
            ReservationId = reservationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<ReservationPayment>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<ReservationPayment?> GetReservationPaymentByIdAsync(int reservationPaymentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationPaymentEntity>("Property.ReservationPayment_GetById", new
        {
            ReservationPaymentId = reservationPaymentId,
            OrganizationId = organizationId
        });

        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertEntityToModel(entity);
    }

    public async Task<ReservationPayment> CreateReservationPaymentAsync(ReservationPayment payment)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationPaymentEntity>("Property.ReservationPayment_Add", new
        {
            OrganizationId = payment.OrganizationId,
            ReservationId = payment.ReservationId,
            Amount = payment.Amount,
            StartDate = payment.StartDate,
            EndDate = payment.EndDate,
            CreatedBy = payment.CreatedBy
        });

        var entity = res?.FirstOrDefault()
            ?? throw new InvalidOperationException("Reservation payment not created");

        return ConvertEntityToModel(entity);
    }

    public async Task<ReservationPayment> UpdateReservationPaymentByIdAsync(ReservationPayment payment)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationPaymentEntity>("Property.ReservationPayment_UpdateById", new
        {
            ReservationPaymentId = payment.ReservationPaymentId,
            OrganizationId = payment.OrganizationId,
            ReservationId = payment.ReservationId,
            Amount = payment.Amount,
            StartDate = payment.StartDate,
            EndDate = payment.EndDate,
            ModifiedBy = payment.ModifiedBy
        });

        var entity = res?.FirstOrDefault()
            ?? throw new InvalidOperationException("Reservation payment not found");

        return ConvertEntityToModel(entity);
    }

    public async Task DeleteReservationPaymentByIdAsync(int reservationPaymentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.ReservationPayment_DeleteById", new
        {
            ReservationPaymentId = reservationPaymentId,
            OrganizationId = organizationId
        });
    }

    public async Task<IEnumerable<ReservationPayment>> ApplyReservationRentChangeAsync(
        Reservation reservation,
        decimal newAmount,
        DateOnly effectiveDate,
        Guid modifiedBy)
    {
        var billingStart = ResolveBillingArrivalDate(reservation);
        var billingEnd = ResolveBillingDepartureDate(reservation);

        if (effectiveDate < billingStart || effectiveDate > billingEnd)
            throw new InvalidOperationException("Effective date must fall within the reservation billing period.");

        var existing = (await GetReservationPaymentsByReservationIdAsync(reservation.OrganizationId, reservation.ReservationId))
            .OrderBy(p => p.StartDate)
            .ThenBy(p => p.ReservationPaymentId)
            .ToList();

        if (existing.Count == 0)
        {
            await CreateReservationPaymentAsync(new ReservationPayment
            {
                OrganizationId = reservation.OrganizationId,
                ReservationId = reservation.ReservationId,
                Amount = newAmount,
                StartDate = effectiveDate,
                EndDate = billingEnd,
                CreatedBy = modifiedBy,
                ModifiedBy = modifiedBy
            });
        }
        else
        {
            var current = existing.FirstOrDefault(p => p.StartDate <= effectiveDate && p.EndDate >= effectiveDate);
            if (current == null)
                throw new InvalidOperationException("No payment record covers the effective date.");

            if (effectiveDate > current.StartDate)
            {
                current.EndDate = effectiveDate.AddDays(-1);
                current.ModifiedBy = modifiedBy;
                await UpdateReservationPaymentByIdAsync(current);
            }
            else
            {
                await DeleteReservationPaymentByIdAsync(current.ReservationPaymentId, reservation.OrganizationId);
            }

            foreach (var futurePayment in existing.Where(p => p.StartDate > effectiveDate).ToList())
            {
                await DeleteReservationPaymentByIdAsync(futurePayment.ReservationPaymentId, reservation.OrganizationId);
            }

            await CreateReservationPaymentAsync(new ReservationPayment
            {
                OrganizationId = reservation.OrganizationId,
                ReservationId = reservation.ReservationId,
                Amount = newAmount,
                StartDate = effectiveDate,
                EndDate = billingEnd,
                CreatedBy = modifiedBy,
                ModifiedBy = modifiedBy
            });
        }

        return await GetReservationPaymentsByReservationIdAsync(reservation.OrganizationId, reservation.ReservationId);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateReservationPaymentWindow(
        Reservation reservation,
        DateOnly startDate,
        DateOnly endDate)
    {
        var billingStart = ResolveBillingArrivalDate(reservation);
        var billingEnd = ResolveBillingDepartureDate(reservation);

        if (startDate > endDate)
            return (false, "StartDate must be on or before EndDate");

        if (startDate < billingStart || endDate > billingEnd)
            return (false, "Payment dates must fall within the reservation billing period.");

        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateReservationPaymentOverlap(
        IEnumerable<ReservationPayment> payments,
        DateOnly startDate,
        DateOnly endDate,
        int? excludeReservationPaymentId = null)
    {
        foreach (var payment in payments)
        {
            if (excludeReservationPaymentId.HasValue && payment.ReservationPaymentId == excludeReservationPaymentId.Value)
                continue;

            if (startDate <= payment.EndDate && payment.StartDate <= endDate)
                return (false, "Payment dates cannot overlap another payment record for this reservation.");
        }

        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateReservationPaymentAmount(decimal amount)
    {
        if (amount <= 0)
            return (false, "Payment amount must be greater than zero.");

        return (true, null);
    }

    private static ReservationPayment ConvertEntityToModel(ReservationPaymentEntity e)
    {
        return new ReservationPayment
        {
            ReservationPaymentId = e.ReservationPaymentId,
            OrganizationId = e.OrganizationId,
            ReservationId = e.ReservationId,
            Amount = e.Amount,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            CreatedByName = e.CreatedByName,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static DateOnly ResolveBillingArrivalDate(Reservation reservation)
        => reservation.BillingStartDate ?? reservation.ArrivalDate;

    private static DateOnly ResolveBillingDepartureDate(Reservation reservation)
        => reservation.BillingEndDate ?? reservation.DepartureDate;
}
