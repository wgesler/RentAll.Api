using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Serialization;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository : IJournalEntryRepository
{
    private const int DefaultJournalEntryTransactionTypeId = 0;
    private static readonly JsonSerializerOptions JsonOptions = SqlColumnJsonSerializerOptions.CaseInsensitive;
    private readonly string _dbConnectionString;
    private readonly ILogger<JournalEntryRepository> _logger;

    public JournalEntryRepository(IOptions<AppSettings> appSettings, ILogger<JournalEntryRepository> logger)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        _logger = logger;
    }

    private JournalEntry ConvertEntityToModel(JournalEntryEntity e)
    {
        var lines = ParseJournalEntryLinesJson(e.JournalEntryLines, e.JournalEntryId);

        return new JournalEntry
        {
            JournalEntryId = e.JournalEntryId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            JournalEntryCode = e.JournalEntryCode,
            TransactionDate = e.TransactionDate,
            AccountingPeriod = e.AccountingPeriod,
            PostingStatusId = (PostingStatus)e.PostingStatusId,
            SourceTypeId = e.SourceTypeId,
            JournalEntryKindId = (JournalEntryKind)e.JournalEntryKindId,
            SourceId = e.SourceId,
            SourceCode = e.SourceCode,
            CheckNumber = e.CheckNumber,
            Memo = e.Memo,
            IsCashOnly = e.IsCashOnly,
            JournalEntryLines = lines,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    private List<JournalEntryLine> ParseJournalEntryLinesJson(string? json, Guid journalEntryId)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<JournalEntryLine>();

        try
        {
            var entityLines = DeserializeJournalEntryLineEntities(json);
            return entityLines.Select(ConvertJournalEntryLineEntityToModel).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not deserialize JournalEntryLines JSON for journal entry {JournalEntryId}. First 240 chars: {Preview}",
                journalEntryId,
                json.Length <= 240 ? json : json[..240]);
            return new List<JournalEntryLine>();
        }
    }

    private static List<JournalEntryLineEntity> DeserializeJournalEntryLineEntities(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<JournalEntryLineEntity>>(json, JsonOptions) ?? new List<JournalEntryLineEntity>();
        }
        catch (JsonException)
        {
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<JournalEntryLineEntity>>(root.GetRawText(), JsonOptions) ?? new List<JournalEntryLineEntity>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            ReadOnlySpan<string> knownArrayProps = ["JournalEntryLines", "journalEntryLines", "lines", "value"];
            foreach (var name in knownArrayProps)
            {
                if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<JournalEntryLineEntity>>(el.GetRawText(), JsonOptions) ?? new List<JournalEntryLineEntity>();
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<JournalEntryLineEntity>>(prop.Value.GetRawText(), JsonOptions) ?? new List<JournalEntryLineEntity>();
            }
        }

        throw new JsonException("JournalEntryLines JSON is not an array and no array property was found.");
    }

    private JournalEntryLine ConvertJournalEntryLineEntityToModel(JournalEntryLineEntity e)
    {
        return new JournalEntryLine
        {
            JournalEntryLineId = e.JournalEntryLineId,
            JournalEntryId = e.JournalEntryId,
            ChartOfAccountId = e.ChartOfAccountId,
            CostCodeId = e.CostCodeId,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            ContactId = e.ContactId,
            ContactName = e.ContactName,
            Debit = e.Debit,
            Credit = e.Credit,
            Memo = e.Memo,
            PerspectiveId = (Perspective)e.PerspectiveId,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    private JournalEntryLineSearchResult ConvertLineSearchEntityToModel(JournalEntryLineSearchEntity e)
    {
        return new JournalEntryLineSearchResult
        {
            JournalEntryLineId = e.JournalEntryLineId,
            JournalEntryId = e.JournalEntryId,
            ChartOfAccountId = e.ChartOfAccountId,
            CostCodeId = e.CostCodeId,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            ContactId = e.ContactId,
            ContactName = e.ContactName,
            Debit = e.Debit,
            Credit = e.Credit,
            Memo = e.Memo,
            IsCleared = e.IsCleared,
            ClearedOn = e.ClearedOn,
            PerspectiveId = e.PerspectiveId,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            OfficeId = e.OfficeId,
            JournalEntryCode = e.JournalEntryCode,
            TransactionDate = e.TransactionDate,
            AccountingPeriod = e.AccountingPeriod,
            PostingStatusId = e.PostingStatusId,
            JournalEntryKindId = e.JournalEntryKindId,
            SourceTypeId = e.SourceTypeId,
            SourceId = e.SourceId,
            SourceCode = e.SourceCode,
            CheckNumber = e.CheckNumber,
            JournalEntryMemo = e.JournalEntryMemo,
            JournalEntryCreatedOn = e.JournalEntryCreatedOn,
        };
    }

    private List<JournalEntry> MapJournalEntriesWithLineEntities(
        IEnumerable<JournalEntryEntity>? journalEntryEntities,
        IEnumerable<JournalEntryLineEntity>? lineEntities)
    {
        if (journalEntryEntities == null || !journalEntryEntities.Any())
            return new List<JournalEntry>();

        var linesByJournalEntryId = (lineEntities ?? Enumerable.Empty<JournalEntryLineEntity>())
            .GroupBy(line => line.JournalEntryId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertJournalEntryLineEntityToModel)
                    .GroupBy(line => line.JournalEntryLineId)
                    .Select(lineGroup => lineGroup.First())
                    .OrderBy(line => line.CreatedOn)
                    .ThenBy(line => line.JournalEntryLineId)
                    .ToList());

        var journalEntries = journalEntryEntities.Select(ConvertEntityToModel).ToList();
        foreach (var journalEntry in journalEntries)
        {
            if (linesByJournalEntryId.TryGetValue(journalEntry.JournalEntryId, out var lines) && lines.Count > 0)
                journalEntry.JournalEntryLines = lines;
        }

        return journalEntries;
    }
}
