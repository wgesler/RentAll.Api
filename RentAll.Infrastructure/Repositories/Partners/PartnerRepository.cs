using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Partners;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Partners;
using RentAll.Infrastructure.Entities.Properties;

namespace RentAll.Infrastructure.Repositories.Partners;

public class PartnerRepository : IPartnerRepository
{
    private readonly string _dbConnectionString;

    public PartnerRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    public async Task<IEnumerable<PropertyList>> GetAllPropertiesAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyListEntity>("Partner.Partner_GetAllProperties");

        if (res == null || !res.Any())
            return Enumerable.Empty<PropertyList>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<PropertyList>> GetActivePropertyListBySelectionCriteriaAsync(Guid userId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyListEntity>("Partner.Partner_GetActiveListBySelection", new
        {
            UserId = userId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<PropertyList>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<PartnerCityState>> GetListOfCitiesAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PartnerCityStateEntity>("Partner.Partner_GetListOfCities");

        if (res == null || !res.Any())
            return Enumerable.Empty<PartnerCityState>();

        return res.Select(e => new PartnerCityState
        {
            City = e.City,
            State = e.State
        });
    }

    public async Task<PartnerContact?> GetPartnerContactAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PartnerContactEntity>("Partner.Partner_GetPartnerContact", new
        {
            PropertyId = propertyId
        });

        var contact = res?.FirstOrDefault();
        if (contact == null)
            return null;

        return new PartnerContact
        {
            CompanyName = contact.CompanyName,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email
        };
    }

    private static PropertyList ConvertEntityToModel(PropertyListEntity e) =>
        new()
        {
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            PropertyLeaseType = (PropertyLeaseType)e.PropertyLeaseTypeId,
            ShortAddress = e.ShortAddress,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            Owner1Id = e.Owner1Id,
            VendorId = e.VendorId,
            ContactName = e.ContactName,
            AvailableFrom = e.AvailableFrom,
            AvailableUntil = e.AvailableUntil,
            UnitLevel = e.UnitLevel,
            Bedrooms = e.Bedrooms,
            Bathrooms = e.Bathrooms,
            Accomodates = e.Accomodates,
            SquareFeet = e.SquareFeet,
            PropertyType = (PropertyType)e.PropertyTypeId,
            Unfurnished = e.Unfurnished,
            MonthlyRate = e.MonthlyRate,
            DailyRate = e.DailyRate,
            DepartureFee = e.DepartureFee,
            PetFee = e.PetFee,
            MaidServiceFee = e.MaidServiceFee,
            PropertyStatus = (PropertyStatus)e.PropertyStatusId,
            NoticeStatus = (NoticeStatusType)e.NoticeStatusId,
            BedroomId1 = e.BedroomId1,
            BedroomId2 = e.BedroomId2,
            BedroomId3 = e.BedroomId3,
            BedroomId4 = e.BedroomId4,
            onCleanerUserId = e.onCleanerUserId,
            onCleaningDate = e.onCleaningDate,
            onCarpetUserId = e.onCarpetUserId,
            onCarpetDate = e.onCarpetDate,
            onInspectorUserId = e.onInspectorUserId,
            onInspectingDate = e.onInspectingDate,
            offCleanerUserId = e.offCleanerUserId,
            offCleaningDate = e.offCleaningDate,
            offCarpetUserId = e.offCarpetUserId,
            offCarpetDate = e.offCarpetDate,
            offInspectorUserId = e.offInspectorUserId,
            offInspectingDate = e.offInspectingDate,
            OnlineChecked = e.OnlineChecked,
            OfflineChecked = e.OfflineChecked,
            ExternalCalendar = e.ExternalCalendar,
            IsActive = e.IsActive
        };
}
