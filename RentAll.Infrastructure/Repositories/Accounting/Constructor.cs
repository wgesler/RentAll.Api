using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Accounting;
using RentAll.Infrastructure.Serialization;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository : IAccountingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SqlColumnJsonSerializerOptions.CaseInsensitive;
    private readonly string _dbConnectionString;
    private readonly ILogger<AccountingRepository> _logger;

    public AccountingRepository(IOptions<AppSettings> appSettings, ILogger<AccountingRepository> logger)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        _logger = logger;
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
        var lines = ParseLedgerLinesJson(e.LedgerLines, e.InvoiceId);

        return new Invoice
        {
            InvoiceId = e.InvoiceId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            InvoiceCode = e.InvoiceCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            ContactId = e.ContactId,
            ResponsibleParty = e.ResponsibleParty,
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

    private List<LedgerLine> ParseLedgerLinesJson(string? json, Guid invoiceId)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<LedgerLine>();

        try
        {
            var entityLines = DeserializeLedgerLineEntities(json);
            return entityLines.Select(ConvertLedgerLineEntityToModel).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not deserialize LedgerLines JSON for invoice {InvoiceId}. First 240 chars: {Preview}",
                invoiceId,
                json.Length <= 240 ? json : json[..240]);
            return new List<LedgerLine>();
        }
    }

    private static List<LedgerLineEntity> DeserializeLedgerLineEntities(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<LedgerLineEntity>>(json, JsonOptions) ?? new List<LedgerLineEntity>();
        }
        catch (JsonException)
        {
            // e.g. { "LedgerLines": [ ... ] } or ROOT from SQL FOR JSON
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<LedgerLineEntity>>(root.GetRawText(), JsonOptions) ?? new List<LedgerLineEntity>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            ReadOnlySpan<string> knownArrayProps = ["LedgerLines", "ledgerLines", "lines", "value"];
            foreach (var name in knownArrayProps)
            {
                if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<LedgerLineEntity>>(el.GetRawText(), JsonOptions) ?? new List<LedgerLineEntity>();
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<LedgerLineEntity>>(prop.Value.GetRawText(), JsonOptions) ?? new List<LedgerLineEntity>();
            }
        }

        throw new JsonException("LedgerLines JSON is not an array and no array property was found.");
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
