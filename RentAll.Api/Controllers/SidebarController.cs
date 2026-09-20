using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/sidebar")]
[Authorize]
public sealed class SidebarController : BaseController
{
    private readonly ICommonRepository _commonRepository;
    private readonly IAccountingManager _accountingManager;
    private readonly ILogger<SidebarController> _logger;

    public SidebarController(
        ICommonRepository commonRepository,
        IAccountingManager accountingManager,
        ILogger<SidebarController> logger)
    {
        _commonRepository = commonRepository;
        _accountingManager = accountingManager;
        _logger = logger;
    }

    [HttpGet("attention-summary")]
    public async Task<IActionResult> GetAttentionSummaryAsync()
    {
        try
        {
            return Ok(await LoadSummaryAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sidebar attention summary");
            return ServerError("An error occurred while loading sidebar attention indicators");
        }
    }

    private async Task<SidebarAttentionSummaryDto> LoadSummaryAsync()
    {
        var organizationId = CurrentOrganizationId;
        var officeAccess = CurrentOfficeAccess;
        var currentUserId = CurrentUser;
        var countsTask = _commonRepository.GetSidebarAttentionCountsAsync(organizationId, officeAccess, currentUserId);
        var depositsTask = _accountingManager.GetUnreturnedSecurityDepositsAsync(organizationId, officeAccess);
        await Task.WhenAll(countsTask, depositsTask);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var depositCount = depositsTask.Result.Rows.Count(row => row.DepartureDate <= today
            && !row.DepositReturned
            && (Math.Max(0m, row.BalanceAmount - row.ReturnedAmount) > 0.005m
                || Math.Max(0m, row.OwedAmount - row.TransferredAmount) > 0.005m));

        return new SidebarAttentionSummaryDto(countsTask.Result.AssignedTicketCount, countsTask.Result.NewLeadCount,
            depositCount, countsTask.Result.PendingReceiptDraftCount);
    }
}

public sealed record SidebarAttentionSummaryDto(
    long AssignedTicketCount,
    long NewLeadCount,
    int SecurityDepositCount,
    long PendingReceiptDraftCount);
