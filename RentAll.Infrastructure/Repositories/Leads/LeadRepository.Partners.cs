using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<IEnumerable<LeadPartner>> GetPartnersByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PartnerEntity>("Lead.Partner_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<LeadPartner>();

        return res.Select(ConvertPartnerEntityToModel);
    }

    public async Task<LeadPartner?> GetPartnerByIdAsync(int partnerId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PartnerEntity>("Lead.Partner_GetById", new { PartnerId = partnerId });

        if (res == null || !res.Any())
            return null;

        return ConvertPartnerEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<LeadPartner> CreatePartnerAsync(LeadPartner partner)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PartnerEntity>("Lead.Partner_Add", new
        {
            OrganizationId = partner.OrganizationId,
            OfficeId = partner.OfficeId,
            LeadStateId = (int)partner.LeadState,
            Name = partner.Name,
            CompanyName = partner.CompanyName,
            Title = partner.Title,
            Email = partner.Email,
            Phone = partner.Phone,
            MarketsCitiesServed = partner.MarketsCitiesServed,
            FurnishedPropertiesInPortfolio = partner.FurnishedPropertiesInPortfolio,
            AboutYourBusiness = partner.AboutYourBusiness,
            Notes = partner.Notes,
            CreatedBy = partner.CreatedBy,
            ModifiedBy = partner.ModifiedBy,
            EmailPhoneConsent = partner.EmailPhoneConsent,
            SmsConsent = partner.SmsConsent,
            IsActive = partner.IsActive
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Partner lead was not created.");

        return ConvertPartnerEntityToModel(res.First());
    }

    #endregion

    #region Updates

    public async Task<LeadPartner> UpdatePartnerByIdAsync(LeadPartner partner)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PartnerEntity>("Lead.Partner_UpdateById", new
        {
            PartnerId = partner.PartnerId,
            OrganizationId = partner.OrganizationId,
            OfficeId = partner.OfficeId,
            LeadStateId = (int)partner.LeadState,
            Name = partner.Name,
            CompanyName = partner.CompanyName,
            Title = partner.Title,
            Email = partner.Email,
            Phone = partner.Phone,
            MarketsCitiesServed = partner.MarketsCitiesServed,
            FurnishedPropertiesInPortfolio = partner.FurnishedPropertiesInPortfolio,
            AboutYourBusiness = partner.AboutYourBusiness,
            Notes = partner.Notes,
            ModifiedBy = partner.ModifiedBy,
            EmailPhoneConsent = partner.EmailPhoneConsent,
            SmsConsent = partner.SmsConsent,
            IsActive = partner.IsActive
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Partner lead was not found or not updated.");

        return ConvertPartnerEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeletePartnerByIdAsync(int partnerId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Lead.Partner_DeleteById", new { PartnerId = partnerId });
    }

    #endregion
}
