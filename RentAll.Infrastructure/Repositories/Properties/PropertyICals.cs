using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Properties;

public partial class PropertyRepository
{
    #region Property ICals
    public async Task<IEnumerable<string>> GetPropertyICalsByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyICalEntity>("Property.PropertyICal_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        return NormalizeICalUrls((res ?? []).Select(item => item.ICalUrl));
    }

    public async Task ReplacePropertyICalsAsync(Guid propertyId, IEnumerable<string>? calendars)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.PropertyICal_DeleteByPropertyId", new
        {
            PropertyId = propertyId
        });

        foreach (var url in NormalizeICalUrls(calendars))
        {
            await db.DapperProcQueryAsync<PropertyICalEntity>("Property.PropertyICal_Add", new
            {
                PropertyId = propertyId,
                ICalUrl = url
            });
        }
    }

    private Property? MapPropertyWithICalUrls(IEnumerable<PropertyEntity>? headers, IEnumerable<PropertyICalEntity>? rows)
    {
        var header = headers?.FirstOrDefault();
        if (header == null)
            return null;

        var property = ConvertEntityToModel(header);
        property.ExternalCalendars = MapICalUrls(header.PropertyId, rows);
        return property;
    }

    private List<PropertyList> MapPropertyListWithICalUrls(IEnumerable<PropertyListEntity>? headers, IEnumerable<PropertyICalEntity>? rows)
    {
        return (headers ?? []).Select(header =>
        {
            var property = ConvertEntityToModel(header);
            property.ExternalCalendars = MapICalUrls(header.PropertyId, rows);
            return property;
        }).ToList();
    }

    private List<ExternalExportPropertyList> MapExportListWithICalUrls(IEnumerable<ExternalExportPropertyListEntity>? headers, IEnumerable<PropertyICalEntity>? rows)
    {
        return (headers ?? []).Select(header =>
        {
            var property = ConvertExternalExportEntityToModel(header);
            property.ExternalCalendars = MapICalUrls(header.PropertyId, rows);
            return property;
        }).ToList();
    }

    private async Task<Property?> AttachPropertyICalsIfMissingAsync(Property? property)
    {
        if (property == null)
            return property;

        property.ExternalCalendars = (await GetPropertyICalsByPropertyIdAsync(property.PropertyId)).ToList();
        return property;
    }

    internal static List<string> MapICalUrls(Guid propertyId, IEnumerable<PropertyICalEntity>? rows)
    {
        return NormalizeICalUrls((rows ?? []).Where(row => row.PropertyId == propertyId).Select(row => row.ICalUrl));
    }

    internal static List<string> NormalizeICalUrls(IEnumerable<string?>? urls)
    {
        return (urls ?? [])
            .Select(url => (url ?? string.Empty).Trim())
            .Where(url => url.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    #endregion
}
