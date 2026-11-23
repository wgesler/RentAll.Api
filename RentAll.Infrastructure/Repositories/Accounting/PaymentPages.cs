using Microsoft.Data.SqlClient;
using RentAll.Domain.Models.Accounting;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region PaymentPages
    /// <summary>The office's row, otherwise the organization's row, otherwise the system default.</summary>
    public async Task<PaymentPage?> GetPaymentPageByOfficeIdAsync(Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentPageEntity>("Accounting.PaymentPage_GetByOfficeId", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId
        });
        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertEntityToModel(entity);
    }

    public async Task<PaymentPage?> GetPaymentPageByIdAsync(int paymentPageId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentPageEntity>("Accounting.PaymentPage_GetById", new
        {
            PaymentPageId = paymentPageId,
            OrganizationId = organizationId
        });
        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertEntityToModel(entity);
    }

    public async Task<IEnumerable<PaymentPage>> GetPaymentPagesByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentPageEntity>("Accounting.PaymentPage_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
        return res?.Select(ConvertEntityToModel).ToList() ?? new List<PaymentPage>();
    }

    public async Task<PaymentPage> AddPaymentPageAsync(PaymentPage page)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentPageEntity>("Accounting.PaymentPage_Add", new
        {
            OrganizationId = page.OrganizationId,
            OfficeId = page.OfficeId,
            RequestPageUrl = page.RequestPageUrl,
            ResponsePageUrl = page.ResponsePageUrl
        });
        var entity = res?.FirstOrDefault() ?? throw new InvalidOperationException("Payment page was not created.");
        return ConvertEntityToModel(entity);
    }

    public async Task<PaymentPage> UpdatePaymentPageByIdAsync(PaymentPage page)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentPageEntity>("Accounting.PaymentPage_UpdateById", new
        {
            PaymentPageId = page.PaymentPageId,
            OrganizationId = page.OrganizationId,
            RequestPageUrl = page.RequestPageUrl,
            ResponsePageUrl = page.ResponsePageUrl
        });
        var entity = res?.FirstOrDefault() ?? throw new InvalidOperationException("Payment page was not found.");
        return ConvertEntityToModel(entity);
    }

    public async Task DeletePaymentPageByIdAsync(int paymentPageId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcQueryAsync<PaymentPageEntity>("Accounting.PaymentPage_DeleteById", new
        {
            PaymentPageId = paymentPageId,
            OrganizationId = organizationId
        });
    }

    private static PaymentPage ConvertEntityToModel(PaymentPageEntity e) => new()
    {
        PaymentPageId = e.PaymentPageId,
        OrganizationId = e.OrganizationId,
        OfficeId = e.OfficeId,
        RequestPageUrl = e.RequestPageUrl,
        ResponsePageUrl = e.ResponsePageUrl,
        CreatedOn = e.CreatedOn,
        ModifiedOn = e.ModifiedOn
    };
    #endregion

    #region PaymentLinks
    public async Task<PaymentLink?> GetPaymentLinkByTokenAsync(string token)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentLinkEntity>("Accounting.PaymentLink_GetByToken", new { Token = token });
        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertEntityToModel(entity);
    }

    public async Task<PaymentLink> AddPaymentLinkAsync(PaymentLink link)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentLinkEntity>("Accounting.PaymentLink_Add", new
        {
            Token = link.Token,
            OrganizationId = link.OrganizationId,
            OfficeId = link.OfficeId,
            ReservationId = link.ReservationId,
            ExpiresOn = link.ExpiresOn,
            CreatedBy = link.CreatedBy
        });
        var entity = res?.FirstOrDefault() ?? throw new InvalidOperationException("Payment link was not created.");
        return ConvertEntityToModel(entity);
    }

    /// <summary>Marks the link used once the payer has saved a card or bank account through it.</summary>
    public async Task<PaymentLink?> CompletePaymentLinkByTokenAsync(string token)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PaymentLinkEntity>("Accounting.PaymentLink_UpdateCompletedByToken", new { Token = token });
        var entity = res?.FirstOrDefault();
        return entity == null ? null : ConvertEntityToModel(entity);
    }

    private static PaymentLink ConvertEntityToModel(PaymentLinkEntity e) => new()
    {
        PaymentLinkId = e.PaymentLinkId,
        Token = e.Token,
        OrganizationId = e.OrganizationId,
        OfficeId = e.OfficeId,
        ReservationId = e.ReservationId,
        ExpiresOn = e.ExpiresOn,
        CompletedOn = e.CompletedOn,
        CreatedOn = e.CreatedOn,
        CreatedBy = e.CreatedBy
    };
    #endregion
}
