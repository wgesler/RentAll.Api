using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Domain.Models;

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

        _ = Task.Run(() => RunAllJournalEntriesSyncJobAsync(job, CurrentOrganizationId, officeIds, CurrentUser));

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
            var officeIds = ResolveRequestedOfficeIds(dto);
            if (string.IsNullOrWhiteSpace(officeIds))
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

    private async Task RunAllJournalEntriesSyncJobAsync(
        JournalEntrySyncJobState job,
        Guid organizationId,
        string officeIds,
        Guid currentUser)
    {
        var progress = new Progress<JournalEntrySyncProgress>(update => ApplySyncProgress(job, update));

        try
        {
            await _accountingManager.SyncInvoiceJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
            await _accountingManager.SyncBillJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
            await _accountingManager.SyncReceiptJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
            await _accountingManager.SyncWorkOrderJournalEntriesAsync(organizationId, officeIds, currentUser, progress);
            await _accountingManager.SyncPeriodicFeeJournalEntriesAsync(organizationId, officeIds, progress);

            lock (job.SyncRoot)
            {
                job.Message = "Sync complete.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running journal-entry sync job {JobId}", job.JobId);
            lock (job.SyncRoot)
            {
                job.Message = $"Sync failed: {ex.Message}";
            }
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
            ("bill", "Bills"),
            ("receipt", "Receipts"),
            ("workOrder", "Work Orders"),
            ("departureFee", "Departure Fees"),
            ("linenAndTowelFee", "Linen & Towel Fees")
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
            "bill" => 2,
            "receipt" => 3,
            "workOrder" => 4,
            "departureFee" => 5,
            "linenAndTowelFee" => 6,
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
