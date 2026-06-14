using RentAll.Api.Dtos.Accounting.JournalEntries;

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
            var officeIds = ResolveOfficeIdsForJournalEntrySync(dto.OfficeIds);
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
            var officeIds = ResolveOfficeIdsForJournalEntrySync(dto.OfficeIds);
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
            var officeIds = ResolveOfficeIdsForJournalEntrySync(dto.OfficeIds);
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
            var officeIds = ResolveOfficeIdsForJournalEntrySync(dto.OfficeIds);
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
            var officeIds = ResolveOfficeIdsForJournalEntrySync(dto.OfficeIds);
            var result = await _accountingManager.SyncReceiptJournalEntriesAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing receipt journal entries");
            return ServerError("An error occurred while syncing receipt journal entries");
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
            var officeIds = ResolveOfficeIdsForJournalEntrySync(dto.OfficeIds);
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
    public async Task<IActionResult> ClearAllJournalEntries()
    {
        try
        {
            var result = await _accountingManager.ClearAllJournalEntriesAsync(CurrentOrganizationId);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all journal entries");
            return ServerError("An error occurred while clearing journal entries");
        }
    }

    string ResolveOfficeIdsForJournalEntrySync(int[] requestedOfficeIds)
    {
        var allowedOfficeIds = CurrentOfficeAccess
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.Parse(id))
            .ToHashSet();

        if (requestedOfficeIds == null || requestedOfficeIds.Length == 0)
            return CurrentOfficeAccess;

        var filteredOfficeIds = requestedOfficeIds.Where(allowedOfficeIds.Contains).Distinct().ToArray();
        if (filteredOfficeIds.Length == 0)
            throw new Exception("No accessible offices were provided");

        return string.Join(",", filteredOfficeIds);
    }
}
