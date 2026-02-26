using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Select
    public async Task<IEnumerable<Colour>> GetAllColorsAsync(Guid organizationId)
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

    #region Update
    public async Task UpdateColorByIdAsync(Colour color)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcQueryAsync<ColorEntity>("Organization.Color_UpdateById", new
        {
            ColorId = color.ColorId,
            OrganizationId = color.OrganizationId,
            ReservationStatusId = color.ReservationStatusId,
            Color = color.Color
        });
    }
    #endregion
}
