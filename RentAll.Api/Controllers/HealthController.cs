using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Api.Dtos.Health;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/health")]
[Authorize]
public class HealthController : BaseController
{
    private readonly IHealthRepository _healthRepository;
    private readonly IAccountingManager _accountingManager;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthRepository healthRepository, IAccountingManager accountingManager, ILogger<HealthController> logger)
    {
        _healthRepository = healthRepository;
        _accountingManager = accountingManager;
        _logger = logger;
    }

    [HttpPost("receipt/check")]
    public Task<IActionResult> CheckReceipts([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunReceiptHealthCheckAsync(orgId, officeIds));

    [HttpPost("bill/check")]
    public Task<IActionResult> CheckBills([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunBillHealthCheckAsync(orgId, officeIds));

    [HttpPost("work-order/check")]
    public Task<IActionResult> CheckWorkOrders([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunWorkOrderHealthCheckAsync(orgId, officeIds));

    [HttpPost("invoice/check")]
    public Task<IActionResult> CheckInvoices([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunInvoiceHealthCheckAsync(orgId, officeIds));

    [HttpPost("payment/check")]
    public Task<IActionResult> CheckPayments([FromBody] PaymentHealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunPaymentHealthCheckAsync(orgId, officeIds, dto.PaymentKindId));

    [HttpPost("payment-invoice/check")]
    public Task<IActionResult> CheckInvoicePayments([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunPaymentHealthCheckAsync(orgId, officeIds, (int)PaymentKind.Invoice));

    [HttpPost("payment-bill/check")]
    public Task<IActionResult> CheckBillPayments([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunPaymentHealthCheckAsync(orgId, officeIds, (int)PaymentKind.Bill));

    [HttpPost("payment-owner/check")]
    public Task<IActionResult> CheckOwnerPayments([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunPaymentHealthCheckAsync(orgId, officeIds, (int)PaymentKind.Owner));

    [HttpPost("deposit/check")]
    public Task<IActionResult> CheckDeposits([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunDepositHealthCheckAsync(orgId, officeIds));

    [HttpPost("transfer/check")]
    public Task<IActionResult> CheckTransfers([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunTransferHealthCheckAsync(orgId, officeIds));

    [HttpPost("manual-journal-entry/check")]
    public Task<IActionResult> CheckManualJournalEntries([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunManualJournalEntryHealthCheckAsync(orgId, officeIds));

    [HttpPost("document-links/check")]
    public Task<IActionResult> CheckDocumentLinks([FromBody] HealthCheckRequestDto dto)
        => RunHealthCheckAsync(dto, (orgId, officeIds) => _healthRepository.RunDocumentLinksHealthCheckAsync(orgId, officeIds));

    [HttpPost("transaction-chain/export")]
    public Task<IActionResult> ExportTransactionChain([FromBody] TransactionChainExportRequestDto dto)
        => RunTransactionChainExportAsync(dto);

    [HttpPost("receipt/fix")]
    public Task<IActionResult> FixReceipts([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "receipt");

    [HttpPost("bill/fix")]
    public Task<IActionResult> FixBills([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "bill");

    [HttpPost("work-order/fix")]
    public Task<IActionResult> FixWorkOrders([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "workOrder");

    [HttpPost("invoice/fix")]
    public Task<IActionResult> FixInvoices([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "invoice");

    [HttpPost("payment-invoice/fix")]
    public Task<IActionResult> FixInvoicePayments([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "payment", (int)PaymentKind.Invoice);

    [HttpPost("payment-bill/fix")]
    public Task<IActionResult> FixBillPayments([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "payment", (int)PaymentKind.Bill);

    [HttpPost("payment-owner/fix")]
    public Task<IActionResult> FixOwnerPayments([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "payment", (int)PaymentKind.Owner);

    [HttpPost("deposit/fix")]
    public Task<IActionResult> FixDeposits([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "deposit");

    [HttpPost("transfer/fix")]
    public Task<IActionResult> FixTransfers([FromBody] HealthCheckRequestDto dto)
        => RunHealthFixAsync(dto, "transfer");

    [HttpPost("document-links/fix")]
    public Task<IActionResult> FixDocumentLinks([FromBody] HealthCheckRequestDto dto)
        => RunDocumentLinksHealthFixAsync(dto);

    [HttpPost("rebuild")]
    public async Task<IActionResult> RebuildDocumentJournalEntries([FromBody] HealthRebuildRequestDto dto)
    {
        if (!HasAdminAccess())
            return Unauthorized("Only Admin or SuperAdmin can run health checks.");

        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var allowedOfficeIds = (CurrentOfficeAccess ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => int.TryParse(value, out var id) ? id : 0).Where(id => id > 0).ToHashSet();
        if (!allowedOfficeIds.Contains(dto.OfficeId))
            return Forbid();

        try
        {
            var result = await _accountingManager.RebuildHealthDocumentJournalEntriesAsync(CurrentOrganizationId, dto.OfficeId, dto.DocumentType, dto.DocumentId, dto.RelatedId, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding journal entries for health document {DocumentId}", dto.DocumentId);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return ServerError(string.IsNullOrWhiteSpace(detail) ? "An error occurred while rebuilding journal entries" : $"Journal entry rebuild failed: {detail}");
        }
    }

    private async Task<IActionResult> RunHealthFixAsync(
        HealthCheckRequestDto dto,
        string syncType,
        int? paymentKindId = null)
    {
        if (!HasAdminAccess())
            return Unauthorized("Only Admin or SuperAdmin can run health fixes.");

        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var officeIds = ResolveRequestedOfficeIds(dto);
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        try
        {
            _logger.LogError(
                "[HealthFixTrace] Fix SyncType={SyncType} PaymentKindId={PaymentKindId} OfficeIds={OfficeIds}",
                syncType,
                paymentKindId,
                officeIds);

            var result = await _accountingManager.SyncJournalEntriesForHealthFixAsync(
                CurrentOrganizationId,
                officeIds,
                syncType,
                Array.Empty<Guid>(),
                paymentKindId,
                CurrentUser);

            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running health fix for {SyncType}", syncType);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return ServerError(string.IsNullOrWhiteSpace(detail)
                ? "An error occurred while running the health fix"
                : $"Health fix failed: {detail}");
        }
    }

    private async Task<IActionResult> RunDocumentLinksHealthFixAsync(HealthCheckRequestDto dto)
    {
        if (!HasAdminAccess())
            return Unauthorized("Only Admin or SuperAdmin can run health fixes.");

        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var officeIds = ResolveRequestedOfficeIds(dto);
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        try
        {
            _logger.LogError("[HealthFixTrace] Fix SyncType=documentLinks OfficeIds={OfficeIds}", officeIds);
            var result = await _accountingManager.RepairDocumentLinksForHealthFixAsync(CurrentOrganizationId, officeIds, CurrentUser);
            return Ok(new JournalEntrySyncResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error repairing payment/deposit/transfer document links");
            var detail = ex.InnerException?.Message ?? ex.Message;
            return ServerError(string.IsNullOrWhiteSpace(detail)
                ? "An error occurred while repairing document links"
                : $"Document link repair failed: {detail}");
        }
    }

    private async Task<IActionResult> RunTransactionChainExportAsync(TransactionChainExportRequestDto dto)
    {
        if (!HasAdminAccess())
            return Unauthorized("Only Admin or SuperAdmin can export transaction reports.");

        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var officeIds = ResolveRequestedOfficeIds(dto);
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        try
        {
            var result = await _healthRepository.GetTransactionChainExportAsync(
                CurrentOrganizationId,
                officeIds,
                dto.StartDate,
                dto.EndDate);
            return Ok(new TransactionChainExportDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting transaction chain report");
            var detail = ex.InnerException?.Message ?? ex.Message;
            return ServerError(string.IsNullOrWhiteSpace(detail)
                ? "An error occurred while exporting the transaction report"
                : $"Transaction report export failed: {detail}");
        }
    }

    private async Task<IActionResult> RunHealthCheckAsync(
        HealthCheckRequestDto dto,
        Func<Guid, string, Task<Domain.Models.DocumentHealthResult>> runCheck)
    {
        if (!HasAdminAccess())
            return Unauthorized("Only Admin or SuperAdmin can run health checks.");

        if (dto == null)
            return BadRequest("Request data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var officeIds = ResolveRequestedOfficeIds(dto);
        if (string.IsNullOrWhiteSpace(officeIds))
            return Forbid();

        try
        {
            var result = await runCheck(CurrentOrganizationId, officeIds);
            await _accountingManager.StampHealthIssuePostedJournalEntriesAsync(CurrentOrganizationId, officeIds, result);
            return Ok(new DocumentHealthResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running document health check");
            var detail = ex.InnerException?.Message ?? ex.Message;
            return ServerError(string.IsNullOrWhiteSpace(detail)
                ? "An error occurred while running the health check"
                : $"Health check failed: {detail}");
        }
    }

    private bool HasAdminAccess()
        => IsAdmin() || IsSuperAdmin();

    private string ResolveRequestedOfficeIds(HealthCheckRequestDto dto)
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
}
