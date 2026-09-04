using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Domain.Interfaces.Managers;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    [HttpPost("journal-entry/sync/invoices")]
    public async Task<IActionResult> SyncInvoiceJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncInvoiceJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing invoice journal entries");
            return ServerError("An error occurred while syncing invoice journal entries");
        }
    }

    [HttpPost("journal-entry/sync/payments")]
    public async Task<IActionResult> SyncPaymentJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncPaymentJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing payment journal entries");
            return ServerError("An error occurred while syncing payment journal entries");
        }
    }

    [HttpPost("journal-entry/clear/invoices")]
    public async Task<IActionResult> ClearInvoiceJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.ClearInvoiceJournalEntriesAsync(CurrentOrganizationId, officeIds);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing invoice journal entries");
            return ServerError("An error occurred while clearing invoice journal entries");
        }
    }

    [HttpPost("journal-entry/sync/bills")]
    public async Task<IActionResult> SyncBillJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncBillJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing bill journal entries");
            return ServerError("An error occurred while syncing bill journal entries");
        }
    }

    [HttpPost("journal-entry/clear/bills")]
    public async Task<IActionResult> ClearBillJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.ClearBillJournalEntriesAsync(CurrentOrganizationId, officeIds);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing bill journal entries");
            return ServerError("An error occurred while clearing bill journal entries");
        }
    }

    [HttpPost("journal-entry/sync/receipts")]
    public async Task<IActionResult> SyncReceiptJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncReceiptJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing receipt journal entries");
            return ServerError("An error occurred while syncing receipt journal entries");
        }
    }

    [HttpPost("journal-entry/sync/work-orders")]
    public async Task<IActionResult> SyncWorkOrderJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncWorkOrderJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing work order journal entries");
            return ServerError("An error occurred while syncing work order journal entries");
        }
    }

    [HttpPost("journal-entry/sync/deposits")]
    public async Task<IActionResult> SyncDepositJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncDepositJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing deposit journal entries");
            return ServerError("An error occurred while syncing deposit journal entries");
        }
    }

    [HttpPost("journal-entry/sync/transfers")]
    public async Task<IActionResult> SyncTransferJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.SyncTransferJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing transfer journal entries");
            return ServerError("An error occurred while syncing transfer journal entries");
        }
    }

    [HttpPost("journal-entry/sync/type/start")]
    public IActionResult StartDocumentTypeJournalEntriesSync([FromBody] StartDocumentTypeJournalEntrySyncRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var syncType = NormalizeSyncType(dto.SyncType);
        if (string.IsNullOrWhiteSpace(syncType) || !DocumentTypeSyncTypes.Contains(syncType))
            return BadRequest("Sync type is not supported");

        var officeIds = ResolveRequestedOfficeIds(new SyncJournalEntriesRequestDto { OfficeIds = dto.OfficeIds });
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        var documentIds = dto.ResolveDocumentIds();

        if (dto.HealthFix)
        {
            _logger.LogError(
                "[HealthFixTrace] Start SyncType={SyncType} PaymentKindId={PaymentKindId} DocumentCount={DocumentCount}",
                syncType,
                dto.PaymentKindId,
                documentIds.Length);
        }

        var jobId = Guid.NewGuid().ToString("N");
        var job = CreateDocumentTypeSyncJob(jobId, syncType);
        SyncJobs[jobId] = job;

        _ = Task.Run(() => RunDocumentTypeJournalEntriesSyncJobAsync(
            job,
            syncType,
            CurrentOrganizationId,
            officeIds,
            CurrentUser,
            documentIds,
            dto.PaymentKindId,
            dto.HealthFix));

        return Ok(new StartJournalEntrySyncJobResponseDto { JobId = jobId });
    }

    [HttpPost("journal-entry/sync/transfers/start")]
    public IActionResult StartTransferJournalEntriesSync([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var officeIds = ResolveRequestedOfficeIds(dto);
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        var jobId = Guid.NewGuid().ToString("N");
        var job = CreateTransferSyncJob(jobId);
        SyncJobs[jobId] = job;

        _ = Task.Run(() => RunTransferJournalEntriesSyncJobAsync(job, CurrentOrganizationId, officeIds, CurrentUser));

        return Ok(new StartJournalEntrySyncJobResponseDto { JobId = jobId });
    }

    [HttpPost("journal-entry/sync/document-links")]
    public async Task<IActionResult> SyncDocumentLinks([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            await _accountingManager.SyncDocumentLinksAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new { message = "Document links synced." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing journal entry document links");
            return ServerError("An error occurred while syncing journal entry document links");
        }
    }

    [HttpPost("journal-entry/sync/split-links")]
    public async Task<IActionResult> RepairDepositAndTransferSplitLinks([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.RepairDepositAndTransferSplitLinksAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error repairing deposit and transfer split links");
            return ServerError("An error occurred while repairing deposit and transfer split links");
        }
    }

    [HttpPost("journal-entry/sync/all/start")]
    public IActionResult StartAllJournalEntriesSync([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var officeIds = ResolveRequestedOfficeIds(dto);
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        var jobId = Guid.NewGuid().ToString("N");
        var job = CreateSyncJob(jobId);
        SyncJobs[jobId] = job;

        _ = Task.Run(() => RunAllJournalEntriesSyncJobAsync(job, CurrentOrganizationId, officeIds, dto.StartDate, dto.EndDate, CurrentUser));

        return Ok(new StartJournalEntrySyncJobResponseDto { JobId = jobId });
    }

    [HttpGet("journal-entry/sync/all/status/{jobId}")]
    public IActionResult GetAllJournalEntriesSyncStatus(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return BadRequest("Job ID is required");

        if (!SyncJobs.TryGetValue(jobId.Trim(), out var job))
            return NotFound("Sync job not found");

        lock (job.SyncRoot)
        {
            return Ok(new JournalEntrySyncJobStatusDto
            {
                JobId = job.JobId,
                IsRunning = job.IsRunning,
                IsCompleted = job.IsCompleted,
                Message = job.Message,
                Types = job.Types.Values
                    .OrderBy(t => GetSyncTypeSortOrder(t.Type))
                    .Select(CloneSyncTypeStatus)
                    .ToList()
            });
        }
    }

    [HttpPost("journal-entry/clear/receipts")]
    public async Task<IActionResult> ClearReceiptJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.ClearReceiptJournalEntriesAsync(CurrentOrganizationId, officeIds);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing receipt journal entries");
            return ServerError("An error occurred while clearing receipt journal entries");
        }
    }

    [HttpPost("journal-entry/clear/all")]
    public async Task<IActionResult> ClearAllJournalEntries([FromBody] SyncJournalEntriesRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var officeIds = (dto.OfficeIds == null || dto.OfficeIds.Length == 0)
                ? string.Empty
                : ResolveRequestedOfficeIds(dto);
            if (dto.OfficeIds != null && dto.OfficeIds.Length > 0 && string.IsNullOrWhiteSpace(officeIds))
                return Forbid();

            var result = await _accountingManager.ClearAllJournalEntriesAsync(CurrentOrganizationId, officeIds);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all journal entries");
            return ServerError("An error occurred while clearing journal entries");
        }
    }

    private string ResolveRequestedOfficeIds(SyncJournalEntriesRequestDto dto)
    {
        var allowedOfficeIds = (CurrentOfficeAccess ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        if (allowedOfficeIds.Count == 0)
            return string.Empty;

        var requestedOfficeIds = (dto.OfficeIds ?? Array.Empty<int>())
            .Where(id => id > 0)
            .ToHashSet();

        if (requestedOfficeIds.Count == 0)
            return string.Join(',', allowedOfficeIds.OrderBy(id => id));

        var scopedOfficeIds = requestedOfficeIds
            .Where(allowedOfficeIds.Contains)
            .OrderBy(id => id)
            .ToList();

        return string.Join(',', scopedOfficeIds);
    }

    private JournalEntrySyncJobState CreateSyncJob(string jobId)
    {
        var job = new JournalEntrySyncJobState
        {
            JobId = jobId,
            IsRunning = true,
            IsCompleted = false,
            Message = "Sync started."
        };

        foreach (var (type, label) in GetSyncTypeMap())
        {
            job.Types[type] = new JournalEntrySyncJobTypeStatusDto
            {
                Type = type,
                Label = label,
                Status = "Pending"
            };
        }

        return job;
    }

    private static readonly HashSet<string> DocumentTypeSyncTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "invoice",
        "payment",
        "bill",
        "receipt",
        "workOrder",
        "deposit",
        "transfer"
    };

    private JournalEntrySyncJobState CreateDocumentTypeSyncJob(string jobId, string syncType)
    {
        var label = GetSyncTypeMap()
            .FirstOrDefault(map => string.Equals(map.Type, syncType, StringComparison.OrdinalIgnoreCase))
            .Label;

        var job = new JournalEntrySyncJobState
        {
            JobId = jobId,
            IsRunning = true,
            IsCompleted = false,
            Message = "Sync started."
        };

        job.Types[syncType] = new JournalEntrySyncJobTypeStatusDto
        {
            Type = syncType,
            Label = string.IsNullOrWhiteSpace(label) ? syncType : label,
            Status = "Pending"
        };

        return job;
    }

    private async Task RunDocumentTypeJournalEntriesSyncJobAsync(
        JournalEntrySyncJobState job,
        string syncType,
        Guid organizationId,
        string officeIds,
        Guid currentUser,
        Guid[]? documentIds = null,
        int? paymentKindId = null,
        bool healthFix = false)
    {
        var progress = new Progress<JournalEntrySyncProgress>(update => ApplySyncProgress(job, update));
        var targetedDocumentIds = documentIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray() ?? [];

        try
        {
            if (healthFix && targetedDocumentIds.Length == 0)
                throw new InvalidOperationException("Health fix requires at least one document ID.");

            SetSyncJobMessage(job, targetedDocumentIds.Length > 0
                ? $"Fixing {targetedDocumentIds.Length} {syncType} document(s)..."
                : $"Syncing {syncType}...");
            await RunScopedJournalEntrySyncAsync(async manager =>
            {
                if (targetedDocumentIds.Length > 0)
                {
                    await manager.SyncJournalEntriesForHealthFixAsync(
                        organizationId,
                        officeIds,
                        syncType,
                        targetedDocumentIds,
                        paymentKindId,
                        currentUser,
                        progress);
                    return;
                }

                if (healthFix)
                    throw new InvalidOperationException("Health fix cannot run a full sync.");

                switch (syncType)
                {
                    case "invoice":
                        await manager.SyncInvoiceJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
                        break;
                    case "payment":
                        await manager.SyncPaymentJournalEntriesAsync(organizationId, officeIds, currentUser, progress, syncDocumentLinksAtEnd: true);
                        break;
                    case "bill":
                        await manager.SyncBillJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
                        break;
                    case "receipt":
                        await manager.SyncReceiptJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
                        break;
                    case "workOrder":
                        await manager.SyncWorkOrderJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
                        break;
                    case "deposit":
                        await manager.SyncDepositJournalEntriesAsync(organizationId, officeIds, currentUser, progress, syncDocumentLinksAtEnd: true);
                        break;
                    case "transfer":
                        await manager.SyncTransferJournalEntriesAsync(organizationId, officeIds, currentUser, progress, syncDocumentLinksAtEnd: false);
                        break;
                    default:
                        throw new Exception($"Sync type '{syncType}' is not supported.");
                }
            });
            SetSyncJobMessage(job, "Sync complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running document-type journal-entry sync job {JobId} ({SyncType})", job.JobId, syncType);
            SetSyncJobMessage(job, $"Sync failed: {ex.Message}");
        }
        finally
        {
            lock (job.SyncRoot)
            {
                job.IsRunning = false;
                job.IsCompleted = true;
            }
        }
    }

    private JournalEntrySyncJobState CreateTransferSyncJob(string jobId)
    {
        var job = new JournalEntrySyncJobState
        {
            JobId = jobId,
            IsRunning = true,
            IsCompleted = false,
            Message = "Transfer rebuild started."
        };
        job.Types["transfer"] = new JournalEntrySyncJobTypeStatusDto
        {
            Type = "transfer",
            Label = "Transfers",
            Status = "Pending"
        };
        return job;
    }

    private async Task RunTransferJournalEntriesSyncJobAsync(
        JournalEntrySyncJobState job,
        Guid organizationId,
        string officeIds,
        Guid currentUser)
    {
        var progress = new Progress<JournalEntrySyncProgress>(update => ApplySyncProgress(job, update));

        try
        {
            SetSyncJobMessage(job, "Rebuilding transfer journal entries...");
            await RunScopedJournalEntrySyncAsync(manager =>
                manager.SyncTransferJournalEntriesAsync(
                    organizationId,
                    officeIds,
                    currentUser,
                    progress,
                    syncDocumentLinksAtEnd: false));
            SetSyncJobMessage(job, "Transfer rebuild complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running transfer journal-entry sync job {JobId}", job.JobId);
            SetSyncJobMessage(job, $"Transfer rebuild failed: {ex.Message}");
        }
        finally
        {
            lock (job.SyncRoot)
            {
                job.IsRunning = false;
                job.IsCompleted = true;
            }
        }
    }

    private async Task RunAllJournalEntriesSyncJobAsync(
        JournalEntrySyncJobState job,
        Guid organizationId,
        string officeIds,
        DateOnly? startDate,
        DateOnly? endDate,
        Guid currentUser)
    {
        var progress = new Progress<JournalEntrySyncProgress>(update => ApplySyncProgress(job, update));

        try
        {
            SetSyncJobMessage(job, "Syncing invoices...");
            await RunScopedJournalEntrySyncAsync(manager => manager.SyncInvoiceJournalEntriesAsync(organizationId, officeIds, currentUser, progress));

            SetSyncJobMessage(job, "Syncing payments, bills, receipts, and work orders...");
            await Task.WhenAll(
                RunScopedJournalEntrySyncAsync(manager => manager.SyncPaymentJournalEntriesAsync(organizationId, officeIds, currentUser, progress, syncDocumentLinksAtEnd: false)),
                RunScopedJournalEntrySyncAsync(manager => manager.SyncBillJournalEntriesAsync(organizationId, officeIds, currentUser, progress)),
                RunScopedJournalEntrySyncAsync(manager => manager.SyncReceiptJournalEntriesAsync(organizationId, officeIds, currentUser, progress)),
                RunScopedJournalEntrySyncAsync(manager => manager.SyncWorkOrderJournalEntriesAsync(organizationId, officeIds, currentUser, progress)));

            SetSyncJobMessage(job, "Syncing deposits...");
            await RunScopedJournalEntrySyncAsync(manager => manager.SyncDepositJournalEntriesAsync(organizationId, officeIds, currentUser, progress, syncDocumentLinksAtEnd: false));

            SetSyncJobMessage(job, "Syncing transfers...");
            await RunScopedJournalEntrySyncAsync(manager => manager.SyncTransferJournalEntriesAsync(organizationId, officeIds, currentUser, progress, syncDocumentLinksAtEnd: false));

            SetSyncJobMessage(job, "Syncing periodic fees...");
            await RunScopedJournalEntrySyncAsync(manager => manager.SyncPeriodicFeeJournalEntriesAsync(organizationId, officeIds, startDate, endDate, progress));

            SetSyncJobMessage(job, "Syncing document links and repairing deposit/transfer split links...");
            await RunScopedJournalEntrySyncAsync(manager => manager.SyncDocumentLinksAsync(organizationId, officeIds, currentUser, progress));

            SetSyncJobMessage(job, "Sync complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running journal-entry sync job {JobId}", job.JobId);
            SetSyncJobMessage(job, $"Sync failed: {ex.Message}");
        }
        finally
        {
            lock (job.SyncRoot)
            {
                job.IsRunning = false;
                job.IsCompleted = true;
            }
        }
    }

    private async Task RunScopedJournalEntrySyncAsync(Func<IAccountingManager, Task> syncAction)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var scopedAccountingManager = scope.ServiceProvider.GetRequiredService<IAccountingManager>();
        await syncAction(scopedAccountingManager);
    }

    private static void SetSyncJobMessage(JournalEntrySyncJobState job, string message)
    {
        lock (job.SyncRoot)
        {
            job.Message = message;
        }
    }

    private void ApplySyncProgress(JournalEntrySyncJobState job, JournalEntrySyncProgress update)
    {
        var type = NormalizeSyncType(update.SyncType);
        if (string.IsNullOrWhiteSpace(type))
            return;

        lock (job.SyncRoot)
        {
            if (!job.Types.TryGetValue(type, out var status))
            {
                status = new JournalEntrySyncJobTypeStatusDto { Type = type };
                job.Types[type] = status;
            }

            status.Total = update.Total;
            status.Processed = update.Processed;
            status.Skipped = update.Skipped;
            status.Errors = update.Errors;
            status.Status = string.IsNullOrWhiteSpace(update.Status) ? status.Status : update.Status;
        }
    }

    private static IEnumerable<(string Type, string Label)> GetSyncTypeMap()
    {
        return
        [
            ("invoice", "Invoices"),
            ("payment", "Payments"),
            ("bill", "Bills"),
            ("receipt", "Receipts"),
            ("workOrder", "Work Orders"),
            ("deposit", "Deposits"),
            ("transfer", "Transfers"),
            ("departureFee", "Departure Fees"),
            ("linenAndTowelFee", "Linen & Towel Fees"),
            ("retainedEarnings", "Retained Earnings"),
            ("documentLinkPayment", "Document Links (Payments)"),
            ("documentLinkDeposit", "Document Links (Deposits)"),
            ("documentLinkTransfer", "Document Links (Transfers)"),
            ("splitLinkRepair", "Deposit/Transfer Split Link Repair")
        ];
    }

    private static string NormalizeSyncType(string? syncType)
    {
        return GetSyncTypeMap()
            .FirstOrDefault(map => string.Equals(map.Type, syncType, StringComparison.OrdinalIgnoreCase))
            .Type ?? string.Empty;
    }

    private static int GetSyncTypeSortOrder(string syncType)
    {
        return syncType switch
        {
            "invoice" => 1,
            "payment" => 2,
            "bill" => 3,
            "receipt" => 4,
            "workOrder" => 5,
            "deposit" => 6,
            "transfer" => 7,
            "departureFee" => 8,
            "linenAndTowelFee" => 9,
            "retainedEarnings" => 10,
            "documentLinkPayment" => 11,
            "documentLinkDeposit" => 12,
            "documentLinkTransfer" => 13,
            "splitLinkRepair" => 14,
            _ => int.MaxValue
        };
    }

    private static JournalEntrySyncJobTypeStatusDto CloneSyncTypeStatus(JournalEntrySyncJobTypeStatusDto status)
    {
        var label = GetSyncTypeMap()
            .FirstOrDefault(map => string.Equals(map.Type, status.Type, StringComparison.OrdinalIgnoreCase))
            .Label;

        return new JournalEntrySyncJobTypeStatusDto
        {
            Type = status.Type,
            Label = string.IsNullOrWhiteSpace(label) ? status.Label : label,
            Total = status.Total,
            Processed = status.Processed,
            Skipped = status.Skipped,
            Errors = status.Errors,
            Status = status.Status
        };
    }
}
