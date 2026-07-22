using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
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

    #region BankCards
    private BankCard ConvertEntityToModel(BankCardEntity e)
    {
        return new BankCard
        {
            BankCardId = e.BankCardId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            CardTypeId = e.CardTypeId,
            CardName = e.CardName,
            DisplayName = e.DisplayName,
            LastFour = e.LastFour,
            ChartOfAccountId = e.ChartOfAccountId
        };
    }
    #endregion

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

    #region ClosedDate
    private ClosedDate ConvertClosedDateEntityToModel(ClosedDateEntity e)
    {
        return new ClosedDate
        {
            ClosedDateId = e.ClosedDateId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            StartDate = DateOnly.FromDateTime(e.StartDate),
            EndDate = DateOnly.FromDateTime(e.EndDate),
            PostingStatusId = (PostingStatus)e.PostingStatusId
        };
    }
    #endregion

    #region ChartOfAccounts
    private ChartOfAccount ConvertEntityToModel(ChartOfAccountEntity e)
    {
        return new ChartOfAccount
        {
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            AccountId = e.AccountId,
            AccountNo = e.AccountNo,
            AccountType = (AccountType)e.AccountTypeId,
            Name = e.Name,
            IsSubaccount = e.IsSubaccount,
            SubAccountId = e.SubAccountId,
            Description = e.Description,
            EndingBalance = e.EndingBalance,
            StatementDate = e.StatementDate,
            Note = e.Note
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
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ContactId = e.ContactId,
            ContactName = e.ContactName,
            CompanyId = e.CompanyId,
            CompanyName = e.CompanyName,
            ResponsibleParty = e.ResponsibleParty,
            InvoiceDate = e.InvoiceDate,
            DueDate = e.DueDate,
            AccountingPeriod = e.AccountingPeriod,
            InvoicePeriod = e.InvoicePeriod,
            PostingStatusId = e.PostingStatusId,
            TotalAmount = e.TotalAmount,
            PaidAmount = e.PaidAmount,
            Notes = e.Notes,
            IsActive = e.IsActive,
            LedgerLines = lines,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
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
            LedgerLineDate = e.LedgerLineDate,
            PaymentId = e.PaymentId == Guid.Empty ? null : e.PaymentId,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }
    #endregion

    #region Deposit Helpers
    private static Deposit ConvertDepositEntityToModel(DepositEntity e)
    {
        var splits = DeserializeDepositSplits(e.Splits);

        return new Deposit
        {
            DepositId = e.DepositId,
            DepositCode = e.DepositCode,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId == Guid.Empty ? null : e.PropertyId,
            PropertyIds = new List<Guid>(),
            DepositDate = e.DepositDate,
            AccountingPeriod = e.AccountingPeriod,
            Amount = e.Amount,
            Description = e.Description,
            BankAccountId = e.BankAccountId,
            BankAccountDisplayName = e.BankAccountDisplayName,
            Splits = splits,
            PostingStatusId = e.PostingStatusId,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedByName = e.CreatedByName,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static List<DepositSplit> DeserializeDepositSplits(string? splitsJson)
    {
        if (string.IsNullOrWhiteSpace(splitsJson))
            return new List<DepositSplit>();

        try
        {
            var splitRows = JsonSerializer.Deserialize<List<DepositSplitJson>>(splitsJson, JsonOptions) ?? new List<DepositSplitJson>();
            return splitRows.Select(split => new DepositSplit
            {
                DepositSplitId = split.DepositSplitId ?? 0,
                Amount = split.Amount,
                Description = split.Description,
                PropertyId = split.PropertyId == Guid.Empty ? null : split.PropertyId,
                ReservationId = split.ReservationId == Guid.Empty ? null : split.ReservationId,
                ContactId = split.ContactId == Guid.Empty ? null : split.ContactId,
                JournalEntryLineId = split.JournalEntryLineId == Guid.Empty ? null : split.JournalEntryLineId,
                ChartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null,
            }).ToList();
        }
        catch
        {
            return new List<DepositSplit>();
        }
    }

    private static DepositSplit ConvertDepositSplitEntityToModel(DepositSplitEntity e)
    {
        return new DepositSplit
        {
            DepositSplitId = e.DepositSplitId,
            Amount = e.Amount,
            Description = e.Description,
            PropertyId = e.PropertyId == Guid.Empty ? null : e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId == Guid.Empty ? null : e.ReservationId,
            ReservationCode = e.ReservationCode,
            ContactId = e.ContactId == Guid.Empty ? null : e.ContactId,
            ContactName = e.ContactName,
            JournalEntryLineId = e.JournalEntryLineId == Guid.Empty ? null : e.JournalEntryLineId,
            ChartOfAccountId = e.ChartOfAccountId,
            ChartOfAccountDisplayName = e.ChartOfAccountDisplayName
        };
    }

    private static string SerializeDepositSplits(List<DepositSplit>? splits)
    {
        var rows = (splits ?? new List<DepositSplit>()).Select(split => new DepositSplitJson
        {
            DepositSplitId = split.DepositSplitId > 0 ? split.DepositSplitId : null,
            Amount = split.Amount,
            Description = split.Description,
            PropertyId = split.PropertyId == Guid.Empty ? null : split.PropertyId,
            ReservationId = split.ReservationId == Guid.Empty ? null : split.ReservationId,
            ContactId = split.ContactId == Guid.Empty ? null : split.ContactId,
            JournalEntryLineId = split.JournalEntryLineId == Guid.Empty ? null : split.JournalEntryLineId,
            ChartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null
        }).ToList();

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private sealed class DepositSplitJson
    {
        public int? DepositSplitId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? ContactId { get; set; }
        public Guid? JournalEntryLineId { get; set; }
        public int? ChartOfAccountId { get; set; }
    }
    #endregion

    #region Transfer Helpers
    private static Transfer ConvertTransferEntityToModel(TransferEntity e)
    {
        var splits = DeserializeTransferSplits(e.Splits);

        return new Transfer
        {
            TransferId = e.TransferId,
            TransferCode = e.TransferCode,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId == Guid.Empty ? null : e.PropertyId,
            PropertyIds = new List<Guid>(),
            TransferDate = e.TransferDate,
            AccountingPeriod = e.AccountingPeriod,
            Amount = e.Amount,
            Description = e.Description,
            BankAccountId = e.BankAccountId,
            BankAccountDisplayName = e.BankAccountDisplayName,
            Splits = splits,
            PostingStatusId = e.PostingStatusId,
            HasBeenTransfered = e.HasBeenTransfered,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedByName = e.CreatedByName,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static List<TransferSplit> DeserializeTransferSplits(string? splitsJson)
    {
        if (string.IsNullOrWhiteSpace(splitsJson))
            return new List<TransferSplit>();

        try
        {
            var splitRows = JsonSerializer.Deserialize<List<TransferSplitJson>>(splitsJson, JsonOptions) ?? new List<TransferSplitJson>();
            return splitRows.Select(split => new TransferSplit
            {
                TransferSplitId = split.TransferSplitId ?? 0,
                Amount = split.Amount,
                Description = split.Description,
                PropertyId = split.PropertyId == Guid.Empty ? null : split.PropertyId,
                ReservationId = split.ReservationId == Guid.Empty ? null : split.ReservationId,
                ContactId = split.ContactId == Guid.Empty ? null : split.ContactId,
                JournalEntryLineId = split.JournalEntryLineId == Guid.Empty ? null : split.JournalEntryLineId,
                ChartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null,
            }).ToList();
        }
        catch
        {
            return new List<TransferSplit>();
        }
    }

    private static TransferSplit ConvertTransferSplitEntityToModel(TransferSplitEntity e)
    {
        return new TransferSplit
        {
            TransferSplitId = e.TransferSplitId,
            Amount = e.Amount,
            Description = e.Description,
            PropertyId = e.PropertyId == Guid.Empty ? null : e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId == Guid.Empty ? null : e.ReservationId,
            ReservationCode = e.ReservationCode,
            ContactId = e.ContactId == Guid.Empty ? null : e.ContactId,
            ContactName = e.ContactName,
            JournalEntryLineId = e.JournalEntryLineId == Guid.Empty ? null : e.JournalEntryLineId,
            ChartOfAccountId = e.ChartOfAccountId,
            ChartOfAccountDisplayName = e.ChartOfAccountDisplayName
        };
    }

    private static string SerializeTransferSplits(List<TransferSplit>? splits)
    {
        var rows = (splits ?? new List<TransferSplit>()).Select(split => new TransferSplitJson
        {
            TransferSplitId = split.TransferSplitId > 0 ? split.TransferSplitId : null,
            Amount = split.Amount,
            Description = split.Description,
            PropertyId = split.PropertyId == Guid.Empty ? null : split.PropertyId,
            ReservationId = split.ReservationId == Guid.Empty ? null : split.ReservationId,
            ContactId = split.ContactId == Guid.Empty ? null : split.ContactId,
            JournalEntryLineId = split.JournalEntryLineId == Guid.Empty ? null : split.JournalEntryLineId,
            ChartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null
        }).ToList();

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private sealed class TransferSplitJson
    {
        public int? TransferSplitId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? ContactId { get; set; }
        public Guid? JournalEntryLineId { get; set; }
        public int? ChartOfAccountId { get; set; }
    }
    #endregion

    #region Payment Helpers
    private static List<Payment> MapPaymentsWithLedgerLineEntities(
        IEnumerable<PaymentEntity>? paymentEntities,
        IEnumerable<PaymentLedgerLineEntity>? ledgerLineEntities)
    {
        if (paymentEntities == null || !paymentEntities.Any())
            return new List<Payment>();

        var linesByPaymentId = (ledgerLineEntities ?? Enumerable.Empty<PaymentLedgerLineEntity>())
            .GroupBy(line => line.PaymentId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertPaymentLedgerLineEntityToModel)
                    .OrderBy(line => line.LedgerLineDate)
                    .ThenBy(line => line.InvoiceCode)
                    .ThenBy(line => line.LineNumber)
                    .ToList());

        var payments = paymentEntities.Select(ConvertPaymentEntityToModel).ToList();
        foreach (var payment in payments)
        {
            if (linesByPaymentId.TryGetValue(payment.PaymentId, out var lines))
                payment.LedgerLines = lines;
        }

        return payments;
    }

    private static Payment ConvertPaymentEntityToModel(PaymentEntity e)
    {
        return new Payment
        {
            PaymentId = e.PaymentId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PaymentDate = e.PaymentDate,
            Amount = e.Amount,
            CostCodeId = e.CostCodeId,
            CostCodeDescription = e.CostCodeDescription,
            Description = e.Description,
            PaymentTypeId = e.PaymentTypeId,
            PaymentTypeDescription = e.PaymentTypeDescription,
            DepositId = e.DepositId == Guid.Empty ? null : e.DepositId,
            DepositCode = e.DepositCode,
            PostingStatusId = e.PostingStatusId,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedByName = e.CreatedByName,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static PaymentLedgerLine ConvertPaymentLedgerLineEntityToModel(PaymentLedgerLineEntity e)
    {
        return new PaymentLedgerLine
        {
            LedgerLineId = e.LedgerLineId,
            InvoiceId = e.InvoiceId,
            InvoiceCode = e.InvoiceCode,
            LineNumber = e.LineNumber,
            ReservationId = e.ReservationId == Guid.Empty ? null : e.ReservationId,
            CostCodeId = e.CostCodeId,
            Amount = e.Amount,
            Description = e.Description,
            LedgerLineDate = e.LedgerLineDate,
            PaymentId = e.PaymentId,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }
    #endregion
}
