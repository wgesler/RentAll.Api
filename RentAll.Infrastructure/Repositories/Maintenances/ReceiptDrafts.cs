using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Maintenances;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region ReceiptDraft Selects
    public async Task<IEnumerable<ReceiptDraft>> GetReceiptDraftsByCriteriaAsync(ReceiptDraftGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<ReceiptDraftEntity, ReceiptSplitEntity>("Maintenance.ReceiptDraft_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = criteria.IsActive,
            IncludeInactive = criteria.IncludeInactive,
            IncludePromoted = criteria.IncludePromoted,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate,
            ReceiptKind = criteria.ReceiptKind.HasValue ? (byte?)criteria.ReceiptKind.Value : null,
            VendorId = criteria.VendorId,
            DraftSourceFlags = criteria.DraftSourceFlags.HasValue ? (int?)criteria.DraftSourceFlags.Value : null
        });

        return MapReceiptDraftsWithSplitEntities(headers, splits);
    }

    public async Task<ReceiptDraft?> GetReceiptDraftByIdAsync(Guid receiptDraftId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<ReceiptDraftEntity, ReceiptSplitEntity>("Maintenance.ReceiptDraft_GetById", new
        {
            ReceiptDraftId = receiptDraftId,
            OrganizationId = organizationId
        });

        return MapReceiptDraftsWithSplitEntities(headers, splits).FirstOrDefault();
    }
    #endregion

    #region ReceiptDraft Creates
    public async Task<ReceiptDraft> CreateReceiptDraftAsync(ReceiptDraft draft)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var res = await db.DapperProcQueryAsync<ReceiptDraftEntity>("Maintenance.ReceiptDraft_Add", new
            {
                OrganizationId = draft.OrganizationId,
                OfficeId = draft.OfficeId,
                DraftCode = draft.DraftCode.Trim(),
                Properties = SerializeReceiptPropertyIds(draft.PropertyIds),
                ReceiptDate = draft.ReceiptDate,
                DueDate = draft.DueDate,
                AccountingPeriod = draft.AccountingPeriod,
                BillNumber = draft.BillNumber,
                Amount = draft.Amount,
                Description = draft.Description,
                BankCardId = draft.BankCardId,
                VendorId = draft.VendorId,
                VendorName = draft.VendorName,
                PaidAmount = draft.PaidAmount,
                PaidDate = draft.PaidDate,
                PaymentDescription = draft.PaymentDescription,
                Splits = SerializeReceiptSplits(draft.Splits),
                AgreementLineId = draft.AgreementLineId,
                ReceiptPath = draft.ReceiptPath,
                PaymentTypeId = draft.PaymentTypeId,
                CheckPrinted = draft.CheckPrinted,
                IsUtility = draft.IsUtility,
                BusinessPrivate = draft.BusinessPrivate,
                DraftSourceFlags = (int)draft.DraftSourceFlags,
                ExtractionJson = draft.ExtractionJson,
                IsActive = draft.IsActive,
                CreatedBy = draft.CreatedBy
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new Exception("Receipt draft record not created");

            var created = ConvertDraftEntityToModel(res.First());
            await InsertReceiptDraftSplitRowsAsync(db, transaction, created, draft.Splits, draft.CreatedBy);
            created.Splits = await GetReceiptDraftSplitsByReceiptDraftIdAsync(db, transaction, created.ReceiptDraftId);
            await transaction.CommitAsync();
            return created;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    #endregion

    #region ReceiptDraft Updates
    public async Task<ReceiptDraft> UpdateReceiptDraftAsync(ReceiptDraft draft)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var updated = await UpdateReceiptDraftCoreAsync(db, transaction, draft);
            await transaction.CommitAsync();
            return updated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ReceiptDraft> MarkReceiptDraftPromotedAsync(Guid receiptDraftId, Guid organizationId, Guid promotedReceiptId, Guid promotedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptDraftEntity>("Maintenance.ReceiptDraft_MarkPromoted", new
        {
            ReceiptDraftId = receiptDraftId,
            OrganizationId = organizationId,
            PromotedReceiptId = promotedReceiptId,
            PromotedBy = promotedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Receipt draft not found or already promoted");

        var draft = ConvertDraftEntityToModel(res.First());
        draft.Splits = await GetReceiptDraftSplitsByReceiptDraftIdAsync(db, null, draft.ReceiptDraftId);
        return draft;
    }

    private static async Task<ReceiptDraft> UpdateReceiptDraftCoreAsync(SqlConnection db, IDbTransaction transaction, ReceiptDraft draft)
    {
        var currentSplits = await GetReceiptDraftSplitsByReceiptDraftIdAsync(db, transaction, draft.ReceiptDraftId);

        var res = await db.DapperProcQueryAsync<ReceiptDraftEntity>("Maintenance.ReceiptDraft_UpdateById", new
        {
            ReceiptDraftId = draft.ReceiptDraftId,
            OrganizationId = draft.OrganizationId,
            OfficeId = draft.OfficeId,
            Properties = SerializeReceiptPropertyIds(draft.PropertyIds),
            ReceiptDate = draft.ReceiptDate,
            DueDate = draft.DueDate,
            AccountingPeriod = draft.AccountingPeriod,
            BillNumber = draft.BillNumber,
            Amount = draft.Amount,
            Description = draft.Description,
            BankCardId = draft.BankCardId,
            VendorId = draft.VendorId,
            VendorName = draft.VendorName,
            PaidAmount = draft.PaidAmount,
            PaidDate = draft.PaidDate,
            PaymentDescription = draft.PaymentDescription,
            Splits = SerializeReceiptSplits(draft.Splits),
            AgreementLineId = draft.AgreementLineId,
            ReceiptPath = draft.ReceiptPath,
            PaymentTypeId = draft.PaymentTypeId,
            CheckPrinted = draft.CheckPrinted,
            IsUtility = draft.IsUtility,
            BusinessPrivate = draft.BusinessPrivate,
            DraftSourceFlags = (int)draft.DraftSourceFlags,
            ExtractionJson = draft.ExtractionJson,
            IsActive = draft.IsActive,
            ModifiedBy = draft.ModifiedBy
        }, transaction: transaction);

        if (res == null || !res.Any())
            throw new Exception("Receipt draft record not found or already promoted");

        var updated = ConvertDraftEntityToModel(res.First());
        await SyncReceiptDraftSplitRowsForUpdateAsync(
            db,
            transaction,
            updated,
            draft.Splits ?? new List<ReceiptSplit>(),
            currentSplits,
            draft.ModifiedBy);
        updated.Splits = await GetReceiptDraftSplitsByReceiptDraftIdAsync(db, transaction, updated.ReceiptDraftId);
        return updated;
    }
    #endregion

    #region ReceiptDraft Deletes
    public async Task DeleteReceiptDraftByIdAsync(Guid receiptDraftId, Guid organizationId, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.ReceiptDraft_DeleteById", new
        {
            ReceiptDraftId = receiptDraftId,
            OrganizationId = organizationId,
            ModifiedBy = currentUser
        });
    }
    #endregion

    #region ReceiptDraft Private Methods
    private static List<ReceiptDraft> MapReceiptDraftsWithSplitEntities(
        IEnumerable<ReceiptDraftEntity>? draftEntities,
        IEnumerable<ReceiptSplitEntity>? splitEntities)
    {
        if (draftEntities == null || !draftEntities.Any())
            return new List<ReceiptDraft>();

        var splitsByDraftId = (splitEntities ?? Enumerable.Empty<ReceiptSplitEntity>())
            .GroupBy(split => split.ReceiptId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertEntityToModel)
                    .GroupBy(split => split.ReceiptSplitId)
                    .Select(splitGroup => splitGroup.First())
                    .OrderBy(split => split.ReceiptSplitId)
                    .ToList());

        var drafts = draftEntities.Select(ConvertDraftEntityToModel).ToList();
        foreach (var draft in drafts)
        {
            if (splitsByDraftId.TryGetValue(draft.ReceiptDraftId, out var splits) && splits.Count > 0)
                draft.Splits = splits;
        }

        return drafts;
    }

    private static ReceiptDraft ConvertDraftEntityToModel(ReceiptDraftEntity e)
    {
        var propertyIds = DeserializeReceiptPropertyIds(e.Properties);
        var splits = DeserializeReceiptSplits(e.Splits);

        return new ReceiptDraft
        {
            ReceiptDraftId = e.ReceiptDraftId,
            DraftCode = e.DraftCode,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyIds = propertyIds,
            ReceiptDate = e.ReceiptDate,
            DueDate = e.DueDate,
            AccountingPeriod = e.AccountingPeriod,
            BillNumber = e.BillNumber,
            Amount = e.Amount,
            Description = e.Description,
            BankCardId = e.BankCardId,
            BankCardDisplayName = e.BankCardDisplayName,
            VendorId = e.VendorId,
            VendorName = e.VendorName,
            PaidAmount = e.PaidAmount,
            PaidDate = e.PaidDate,
            PaymentDescription = e.PaymentDescription,
            Splits = splits,
            AgreementLineId = e.AgreementLineId,
            AgreementLineNotes = e.AgreementLineNotes,
            ReceiptPath = e.ReceiptPath,
            PaymentTypeId = e.PaymentTypeId,
            CheckPrinted = e.CheckPrinted,
            IsUtility = e.IsUtility,
            BusinessPrivate = e.BusinessPrivate,
            DraftSourceFlags = (ReceiptDraftSourceFlags)e.DraftSourceFlags,
            PromotedReceiptId = e.PromotedReceiptId,
            PromotedReceiptCode = e.PromotedReceiptCode,
            PromotedOn = e.PromotedOn,
            PromotedBy = e.PromotedBy,
            PromotedByName = e.PromotedByName,
            ExtractionJson = e.ExtractionJson,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedByName = e.CreatedByName,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static async Task<List<ReceiptSplit>> GetReceiptDraftSplitsByReceiptDraftIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid receiptDraftId)
    {
        var splitRows = await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptDraftSplit_GetByReceiptDraftId", new
        {
            ReceiptDraftId = receiptDraftId
        }, transaction: transaction);
        if (splitRows == null || !splitRows.Any())
            return new List<ReceiptSplit>();

        return splitRows
            .Select(ConvertEntityToModel)
            .GroupBy(split => split.ReceiptSplitId)
            .Select(group => group.First())
            .OrderBy(split => split.ReceiptSplitId)
            .ToList();
    }

    private static async Task InsertReceiptDraftSplitRowsAsync(
        SqlConnection db,
        IDbTransaction transaction,
        ReceiptDraft draft,
        List<ReceiptSplit>? splitsToInsert,
        Guid auditUser)
    {
        var splits = splitsToInsert ?? new List<ReceiptSplit>();
        if (splits.Count == 0)
            return;

        var workOrderCodeLookup = await BuildWorkOrderCodeLookupForDraftAsync(db, transaction, draft, splits);
        foreach (var split in splits)
        {
            var workOrderId = ResolveSplitWorkOrderId(split, existing: null, workOrderCodeLookup);
            var chartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null;
            var propertyId = NormalizeSplitPropertyId(split.PropertyId);
            await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptDraftSplit_Add", new
            {
                ReceiptDraftId = draft.ReceiptDraftId,
                Amount = split.Amount,
                Description = split.Description,
                ReceiptTypeId = split.ReceiptTypeId,
                PropertyId = propertyId,
                WorkOrderId = workOrderId,
                ChartOfAccountId = chartOfAccountId,
                CreatedBy = auditUser
            }, transaction: transaction);
        }
    }

    private static async Task SyncReceiptDraftSplitRowsForUpdateAsync(
        SqlConnection db,
        IDbTransaction transaction,
        ReceiptDraft draft,
        List<ReceiptSplit> splitsToSync,
        List<ReceiptSplit> currentSplits,
        Guid auditUser)
    {
        var currentSplitIds = currentSplits.Select(split => split.ReceiptSplitId).ToHashSet();
        var incomingSplitIds = splitsToSync
            .Where(split => split.ReceiptSplitId > 0)
            .Select(split => split.ReceiptSplitId)
            .ToHashSet();

        var splitsToDelete = currentSplitIds.Except(incomingSplitIds).ToList();
        foreach (var receiptSplitId in splitsToDelete)
        {
            await db.DapperProcExecuteAsync("Maintenance.ReceiptDraftSplit_DeleteById", new
            {
                ReceiptDraftSplitId = receiptSplitId
            }, transaction: transaction);
        }

        if (splitsToSync.Count == 0)
            return;

        var workOrderCodeLookup = await BuildWorkOrderCodeLookupForDraftAsync(db, transaction, draft, splitsToSync);
        var currentById = currentSplits.ToDictionary(split => split.ReceiptSplitId);

        foreach (var split in splitsToSync)
        {
            var existing = split.ReceiptSplitId > 0 && currentById.TryGetValue(split.ReceiptSplitId, out var match)
                ? match
                : null;
            var workOrderId = ResolveSplitWorkOrderId(split, existing, workOrderCodeLookup);
            var chartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null;
            var propertyId = NormalizeSplitPropertyId(split.PropertyId);

            if (split.ReceiptSplitId > 0 && currentSplitIds.Contains(split.ReceiptSplitId))
            {
                await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptDraftSplit_UpdateById", new
                {
                    ReceiptDraftSplitId = split.ReceiptSplitId,
                    ReceiptDraftId = draft.ReceiptDraftId,
                    Amount = split.Amount,
                    Description = split.Description,
                    ReceiptTypeId = split.ReceiptTypeId,
                    PropertyId = propertyId,
                    WorkOrderId = workOrderId,
                    ChartOfAccountId = chartOfAccountId,
                    ModifiedBy = auditUser
                }, transaction: transaction);
            }
            else
            {
                await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptDraftSplit_Add", new
                {
                    ReceiptDraftId = draft.ReceiptDraftId,
                    Amount = split.Amount,
                    Description = split.Description,
                    ReceiptTypeId = split.ReceiptTypeId,
                    PropertyId = propertyId,
                    WorkOrderId = workOrderId,
                    ChartOfAccountId = chartOfAccountId,
                    CreatedBy = auditUser
                }, transaction: transaction);
            }
        }
    }

    private static async Task<Dictionary<string, Guid>> BuildWorkOrderCodeLookupForDraftAsync(
        SqlConnection db,
        IDbTransaction transaction,
        ReceiptDraft draft,
        IEnumerable<ReceiptSplit> splits)
    {
        if (!draft.OfficeId.HasValue || draft.OfficeId.Value <= 0)
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        if (!splits.Any(NeedsWorkOrderCodeLookup))
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        var workOrders = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetListByOfficeIds", new
        {
            OrganizationId = draft.OrganizationId,
            Offices = draft.OfficeId.Value.ToString()
        }, transaction: transaction);

        return (workOrders ?? Enumerable.Empty<WorkOrderEntity>())
            .Where(workOrder => workOrder.WorkOrderId != Guid.Empty && !string.IsNullOrWhiteSpace(workOrder.WorkOrderCode))
            .GroupBy(workOrder => workOrder.WorkOrderCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().WorkOrderId, StringComparer.OrdinalIgnoreCase);
    }
    #endregion
}
