using System.Text.Json;
using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    private static readonly JsonSerializerOptions BilledJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    #region Billed Selects
    public async Task<Billed?> GetBilledByReservationIdAsync(Guid organizationId, Guid reservationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<BilledEntity>("Accounting.Billed_GetByReservationId", new
        {
            OrganizationId = organizationId,
            ReservationId = reservationId
        });

        return rows?.FirstOrDefault() is { } entity ? ConvertBilledEntityToModel(entity) : null;
    }

    public async Task<List<Billed>> GetBilledByOrganizationAndOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<BilledEntity>("Accounting.Billed_GetByOrganizationAndOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (rows == null || !rows.Any())
            return [];

        return rows.Select(ConvertBilledEntityToModel).ToList();
    }
    #endregion

    #region Billed Creates
    public async Task<Billed> UpsertBilledByReservationIdAsync(Billed billed)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<BilledEntity>("Accounting.Billed_UpsertByReservationId", new
        {
            OrganizationId = billed.OrganizationId,
            OfficeId = billed.OfficeId,
            ReservationId = billed.ReservationId,
            ReservationCode = billed.ReservationCode,
            StartDate = billed.StartDate,
            EndDate = billed.EndDate,
            InvoiceStart = billed.InvoiceStart,
            BillingTypeId = (int)billed.BillingType,
            TotalNumberOfDays = billed.TotalNumberOfDays,
            DaysSinceStart = billed.DaysSinceStart,
            DaysStayed = billed.DaysStayed,
            DaysBilled = billed.DaysBilled,
            RentalFeeLines = SerializeBilledRentalFeeLines(billed.RentalFeeLines),
            ModifiedBy = billed.ModifiedBy
        });

        if (rows == null || !rows.Any())
            throw new Exception("Billed row not upserted");

        return ConvertBilledEntityToModel(rows.First());
    }

    public async Task<Billed> CreateBilledAsync(Billed billed)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<BilledEntity>("Accounting.Billed_Add", new
        {
            OrganizationId = billed.OrganizationId,
            OfficeId = billed.OfficeId,
            ReservationId = billed.ReservationId,
            ReservationCode = billed.ReservationCode,
            StartDate = billed.StartDate,
            EndDate = billed.EndDate,
            InvoiceStart = billed.InvoiceStart,
            BillingTypeId = (int)billed.BillingType,
            TotalNumberOfDays = billed.TotalNumberOfDays,
            DaysSinceStart = billed.DaysSinceStart,
            DaysStayed = billed.DaysStayed,
            DaysBilled = billed.DaysBilled,
            RentalFeeLines = SerializeBilledRentalFeeLines(billed.RentalFeeLines),
            CreatedBy = billed.CreatedBy
        });

        if (rows == null || !rows.Any())
            throw new Exception("Billed row not created");

        return ConvertBilledEntityToModel(rows.First());
    }
    #endregion

    #region Billed Updates
    public async Task<Billed?> UpdateBilledByReservationIdAsync(Billed billed)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<BilledEntity>("Accounting.Billed_UpdateByReservationId", new
        {
            OrganizationId = billed.OrganizationId,
            OfficeId = billed.OfficeId,
            ReservationId = billed.ReservationId,
            ReservationCode = billed.ReservationCode,
            StartDate = billed.StartDate,
            EndDate = billed.EndDate,
            InvoiceStart = billed.InvoiceStart,
            BillingTypeId = (int)billed.BillingType,
            TotalNumberOfDays = billed.TotalNumberOfDays,
            DaysSinceStart = billed.DaysSinceStart,
            DaysStayed = billed.DaysStayed,
            DaysBilled = billed.DaysBilled,
            RentalFeeLines = SerializeBilledRentalFeeLines(billed.RentalFeeLines),
            ModifiedBy = billed.ModifiedBy
        });

        return rows?.FirstOrDefault() is { } entity ? ConvertBilledEntityToModel(entity) : null;
    }
    #endregion

    #region Billed Deletes
    public async Task DeleteBilledByReservationIdAsync(Guid organizationId, Guid reservationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Billed_DeleteByReservationId", new
        {
            OrganizationId = organizationId,
            ReservationId = reservationId
        });
    }

    public async Task DeleteBilledByOrganizationAndOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Billed_DeleteByOrganizationAndOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });
    }

    public async Task DeleteBilledByOrganizationAndOfficeIdsExceptReservationsAsync(
        Guid organizationId,
        string officeIds,
        IReadOnlyCollection<Guid> reservationIds)
    {
        var reservationIdList = (reservationIds ?? [])
            .Where(reservationId => reservationId != Guid.Empty)
            .Distinct()
            .ToList();

        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Billed_DeleteByOrganizationAndOfficeIdsExceptReservations", new
        {
            OrganizationId = organizationId,
            Offices = officeIds,
            ReservationIds = reservationIdList.Count == 0 ? null : string.Join(',', reservationIdList)
        });
    }
    #endregion

    private static Billed ConvertBilledEntityToModel(BilledEntity entity)
        => new()
        {
            BilledId = entity.BilledId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            ReservationId = entity.ReservationId,
            ReservationCode = entity.ReservationCode,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            InvoiceStart = entity.InvoiceStart,
            BillingType = Enum.IsDefined(typeof(BillingType), entity.BillingTypeId)
                ? (BillingType)entity.BillingTypeId
                : BillingType.Monthly,
            TotalNumberOfDays = entity.TotalNumberOfDays,
            DaysSinceStart = entity.DaysSinceStart,
            DaysStayed = entity.DaysStayed,
            DaysBilled = entity.DaysBilled,
            RentalFeeLines = DeserializeBilledRentalFeeLines(entity.RentalFeeLines),
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            ModifiedOn = entity.ModifiedOn,
            ModifiedBy = entity.ModifiedBy
        };

    private static string? SerializeBilledRentalFeeLines(IReadOnlyList<string>? rentalFeeLines)
    {
        var lines = (rentalFeeLines ?? [])
            .Select(line => (line ?? string.Empty).Trim())
            .Where(line => line.Length > 0)
            .ToList();

        return lines.Count == 0 ? null : JsonSerializer.Serialize(lines, BilledJsonOptions);
    }

    private static List<string> DeserializeBilledRentalFeeLines(string? rentalFeeLinesJson)
    {
        if (string.IsNullOrWhiteSpace(rentalFeeLinesJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(rentalFeeLinesJson, BilledJsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
