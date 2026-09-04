using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Health;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/health")]
[Authorize]
public class HealthController : BaseController
{
    private readonly IHealthRepository _healthRepository;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthRepository healthRepository, ILogger<HealthController> logger)
    {
        _healthRepository = healthRepository;
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
            return Ok(new DocumentHealthResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running document health check");
            return ServerError("An error occurred while running the health check");
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
