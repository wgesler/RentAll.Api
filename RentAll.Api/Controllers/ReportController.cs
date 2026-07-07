using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Reports;
using RentAll.Domain.Interfaces.Managers;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/report")]
[Authorize]
public class ReportController : BaseController
{
    private readonly IReportManager _reportManager;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportManager reportManager, ILogger<ReportController> logger)
    {
        _reportManager = reportManager;
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
}
