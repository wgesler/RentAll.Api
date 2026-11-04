using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<ReservationStatusSetting>> GetReservationStatusSettingsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationStatusSettingEntity>("Organization.ReservationStatusSetting_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<ReservationStatusSetting>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<ReservationStatusSetting?> GetReservationStatusSettingAsync(Guid organizationId, int reservationStatusId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationStatusSettingEntity>("Organization.ReservationStatusSetting_GetByOrganizationAndStatus", new
        {
            OrganizationId = organizationId,
            ReservationStatusId = reservationStatusId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<ReservationStatusSetting?> UpdateReservationStatusSettingAsync(Guid organizationId, int reservationStatusId, bool isEnabled)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReservationStatusSettingEntity>("Organization.ReservationStatusSetting_UpdateByOrganizationAndStatus", new
        {
            OrganizationId = organizationId,
            ReservationStatusId = reservationStatusId,
            IsEnabled = isEnabled
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion
}
