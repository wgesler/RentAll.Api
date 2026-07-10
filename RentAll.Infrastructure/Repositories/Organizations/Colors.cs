using Dapper;
using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<Colour>> GetColorsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ColorEntity>("Organization.Color_GetAll", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Colour>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Colour?> GetColorByIdAsync(int colorId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ColorEntity>("Organization.Color_GetById", new
        {
            ColorId = colorId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task UpdateColorByIdAsync(Colour color)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        const string sql = @"
IF EXISTS (SELECT 1 FROM Organization.Color WHERE ColorId = @ColorId AND OrganizationId = @OrganizationId)
BEGIN
    UPDATE Organization.Color
    SET
        ReservationStatusId = @ReservationStatusId,
        NoticeDays = @NoticeDays,
        Color = @Color
    WHERE
        ColorId = @ColorId
        AND OrganizationId = @OrganizationId;
END
ELSE IF EXISTS (
    SELECT 1
    FROM Organization.Color
    WHERE
        OrganizationId = @OrganizationId
        AND ReservationStatusId = @ReservationStatusId
        AND ISNULL(NoticeDays, -1) = ISNULL(@NoticeDays, -1)
)
BEGIN
    UPDATE Organization.Color
    SET Color = @Color
    WHERE
        OrganizationId = @OrganizationId
        AND ReservationStatusId = @ReservationStatusId
        AND ISNULL(NoticeDays, -1) = ISNULL(@NoticeDays, -1);
END
ELSE
BEGIN
    INSERT INTO Organization.Color (OrganizationId, ReservationStatusId, NoticeDays, Color)
    VALUES (@OrganizationId, @ReservationStatusId, @NoticeDays, @Color);
END";

        await db.ExecuteAsync(sql, new
        {
            ColorId = color.ColorId,
            OrganizationId = color.OrganizationId,
            ReservationStatusId = color.ReservationStatusId,
            NoticeDays = color.NoticeDays,
            Color = color.Color
        });
    }
    #endregion
}
