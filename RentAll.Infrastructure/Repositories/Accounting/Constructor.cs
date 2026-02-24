using System.Text.Json;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository : IAccountingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dbConnectionString;

    public AccountingRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    #region CostCodes
    private CostCode ConvertEntityToModel(CostCodeEntity e)
    {
        return new CostCode
        {
            CostCodeId = e.CostCodeId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            Code = e.CostCode,
            TransactionType = (TransactionType)e.TransactionTypeId,
            Description = e.Description,
            IsActive = e.IsActive
        };
    }
    #endregion

    #region Invoices
    private Invoice ConvertEntityToModel(InvoiceEntity e)
    {
        List<LedgerLine> lines = new List<LedgerLine>();
        if (!string.IsNullOrWhiteSpace(e.LedgerLines))
        {
            try
            {
                var entityLines = JsonSerializer.Deserialize<List<LedgerLineEntity>>(e.LedgerLines, JsonOptions) ?? new List<LedgerLineEntity>();
                lines = entityLines.Select(ConvertLedgerLineEntityToModel).ToList();
            }
            catch
            {
                lines = new List<LedgerLine>();
            }
        }

        return new Invoice
        {
            InvoiceId = e.InvoiceId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            InvoiceCode = e.InvoiceCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            InvoiceDate = e.InvoiceDate,
            DueDate = e.DueDate,
            InvoicePeriod = e.InvoicePeriod,
            TotalAmount = e.TotalAmount,
            PaidAmount = e.PaidAmount,
            Notes = e.Notes,
            IsActive = e.IsActive,
            LedgerLines = lines
        };
    }

    private LedgerLine ConvertLedgerLineEntityToModel(LedgerLineEntity e)
    {
        return new LedgerLine
        {
            LedgerLineId = e.LedgerLineId,
            InvoiceId = e.InvoiceId,
            LineNumber = e.LineNumber,
            ReservationId = e.ReservationId,
            CostCodeId = e.CostCodeId,
            Amount = e.Amount,
            Description = e.Description,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }
    #endregion
}
