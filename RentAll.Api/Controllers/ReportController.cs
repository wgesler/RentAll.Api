using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RentAll.Api.Dtos.Reports;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/report")]
[Authorize]
public class ReportController : BaseController
{
    private readonly IReportManager _reportManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportManager reportManager, IServiceScopeFactory serviceScopeFactory, ILogger<ReportController> logger)
    {
        _reportManager = reportManager;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    [HttpPost("journal-entry-recap/search")]
    public async Task<IActionResult> SearchJournalEntryRecapReport([FromBody] GetRecapReportDto dto)
    {
        if (dto == null)
            return BadRequest("Journal entry recap search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var report = await _reportManager.GetJournalEntryRecapReportAsync(criteria);
            return Ok(new RecapReportResponseDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching journal entry recap report");
            return ServerError("An error occurred while retrieving the journal entry recap report");
        }
    }

    [HttpPost("transfer/search")]
    public async Task<IActionResult> SearchTransferReport([FromBody] GetTransferReportDto dto)
    {
        if (dto == null)
            return BadRequest("Transfer report search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var report = await _reportManager.GetTransferReportAsync(criteria);
            return Ok(new TransferReportResponseDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transfer report");
            return ServerError("An error occurred while retrieving the transfer report");
        }
    }

    [HttpPost("owner-cash/search")]
    public async Task<IActionResult> SearchOwnerCashReport([FromBody] GetOwnerCashReportDto dto)
    {
        if (dto == null)
            return BadRequest("Owner cash report search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var report = await _reportManager.GetOwnerCashReportAsync(criteria);
            return Ok(new OwnerCashReportResponseDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching owner cash report");
            return ServerError("An error occurred while retrieving the owner cash report");
        }
    }

    [HttpPost("owner-accrual/search")]
    public async Task<IActionResult> SearchOwnerAccrualReport([FromBody] GetOwnerAccrualReportDto dto)
    {
        if (dto == null)
            return BadRequest("Owner accrual report search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var report = await _reportManager.GetOwnerAccrualReportAsync(criteria);
            return Ok(new OwnerAccrualReportResponseDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching owner accrual report");
            return ServerError("An error occurred while retrieving the owner accrual report");
        }
    }

    [HttpPost("owner-reports/search")]
    public async Task<IActionResult> SearchOwnerReports([FromBody] GetOwnerCashReportDto dto)
    {
        if (dto == null)
            return BadRequest("Owner report search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var bundle = await _reportManager.GetOwnerReportsBundleAsync(criteria);
            return Ok(new OwnerReportsResponseDto(bundle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching owner reports");
            return ServerError("An error occurred while retrieving owner reports");
        }
    }

    [HttpPost("owner-statement-list/close-month")]
    public async Task<IActionResult> CloseOwnerStatementMonth([FromBody] CloseOwnerStatementMonthDto dto)
    {
        if (dto == null)
            return BadRequest("Owner statement month close request is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var organizationId = CurrentOrganizationId;
            var endDate = dto.EndDate!.Value;
            var closeLines = dto.ToLines();
            var currentUser = CurrentUser;
            var result = await _reportManager.CloseOwnerStatementMonthAsync(
                organizationId,
                endDate,
                closeLines,
                currentUser);
            result.OwnerApSoftCloseQueued = true;
            QueueOwnerApSoftCloseAfterOwnerStatementMonthClose(organizationId, endDate, closeLines, currentUser);
            return Ok(new CloseOwnerStatementMonthResultDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing owner statement month");
            return ServerError(ex.Message);
        }
    }

    [HttpPost("owner-invoice-outstanding/search")]
    public async Task<IActionResult> SearchOwnerInvoiceOutstanding([FromBody] GetOwnerCashReportDto dto)
    {
        if (dto == null)
            return BadRequest("Owner invoice outstanding search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var rows = await _reportManager.GetOwnerInvoiceOutstandingAsync(criteria);
            return Ok(rows.Select(row => new OwnerInvoiceOutstandingResponseDto(row)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching owner invoice outstanding");
            return ServerError("An error occurred while retrieving owner invoice outstanding rows");
        }
    }

    [HttpPost("escrow/search")]
    public async Task<IActionResult> SearchEscrowReport([FromBody] GetEscrowReportDto dto)
    {
        if (dto == null)
            return BadRequest("Escrow report search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var report = await _reportManager.GetEscrowReportAsync(criteria, dto.Cushion);
            return Ok(new EscrowReportResponseDto(report));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching escrow report");
            return ServerError("An error occurred while retrieving the escrow report");
        }
    }

    [HttpPost("owner-report/journal-entry-line/search")]
    public async Task<IActionResult> SearchOwnerReportJournalEntryLines([FromBody] GetOwnerReportJournalEntryLineDto dto)
    {
        if (dto == null)
            return BadRequest("Owner report journal entry line search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var lines = await _reportManager.GetOwnerReportJournalEntryLinesAsync(criteria);
            var response = lines.Select(line => new OwnerReportJournalEntryLineResponseDto(line)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching owner report journal entry lines");
            return ServerError("An error occurred while retrieving owner report journal entry lines");
        }
    }

    [HttpPost("escrow/journal-entry-line/search")]
    public async Task<IActionResult> SearchEscrowReportJournalEntryLines([FromBody] GetEscrowReportJournalEntryLineDto dto)
    {
        if (dto == null)
            return BadRequest("Escrow report journal entry line search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var lines = await _reportManager.GetEscrowReportJournalEntryLinesAsync(criteria);
            var response = lines.Select(line => new OwnerReportJournalEntryLineResponseDto(line)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching escrow report journal entry lines");
            return ServerError("An error occurred while retrieving escrow report journal entry lines");
        }
    }

    private void QueueOwnerApSoftCloseAfterOwnerStatementMonthClose(Guid organizationId, DateOnly endDate, IReadOnlyList<OwnerStatementMonthCloseLine> lines, Guid currentUser)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var accountingManager = scope.ServiceProvider.GetRequiredService<IAccountingManager>();
                var softCloseResult = await accountingManager.SoftCloseOwnerApJournalEntriesForOwnerStatementMonthAsync(organizationId, endDate, lines, currentUser);
                if (softCloseResult.FailedCount > 0)
                {
                    _logger.LogError(
                        "[OwnerStatementMonthClose] Background owner AP soft close completed with {SuccessCount} success and {FailedCount} failures for organization {OrganizationId}. Errors: {Errors}",
                        softCloseResult.SuccessCount,
                        softCloseResult.FailedCount,
                        organizationId,
                        string.Join("; ", softCloseResult.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OwnerStatementMonthClose] Background owner AP soft close failed for organization {OrganizationId}", organizationId);
            }
        });
    }
}
