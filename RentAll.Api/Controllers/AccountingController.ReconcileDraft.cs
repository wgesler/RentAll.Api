using RentAll.Api.Dtos.Accounting.ReconcileDraft;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpGet("reconcile-draft/office/{officeId:int}/account/{accountId:int}")]
    public async Task<IActionResult> GetReconcileDraftByAccountId(int officeId, int accountId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        if (accountId <= 0)
            return BadRequest("AccountId is required");

        if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
            return BadRequest("Unauthorized");

        try
        {
            var reconcileDraft = await _accountingRepository.GetReconcileDraftByAccountIdAsync(CurrentOrganizationId, officeId, accountId);
            if (reconcileDraft == null)
                return Ok(null);

            return Ok(new ReconcileDraftResponseDto(reconcileDraft));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reconcile draft for account {AccountId}", accountId);
            return ServerError("An error occurred while retrieving the reconcile draft");
        }
    }

    #endregion

    #region Put

    [HttpPut("reconcile-draft")]
    public async Task<IActionResult> SaveReconcileDraft([FromBody] SaveReconcileDraftDto dto)
    {
        if (dto == null)
            return BadRequest("Reconcile draft data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid reconcile draft data");

        try
        {
            var chartOfAccount = await _accountingRepository.GetChartOfAccountByIdAsync(CurrentOrganizationId, dto.OfficeId, dto.AccountId);
            if (chartOfAccount == null)
                return NotFound("Chart of account not found");

            var reconcileDraft = await _accountingRepository.UpsertReconcileDraftAsync(dto.ToModel(CurrentOrganizationId));
            return Ok(new ReconcileDraftResponseDto(reconcileDraft));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reconcile draft for account {AccountId}", dto.AccountId);
            return ServerError("An error occurred while saving the reconcile draft");
        }
    }

    #endregion
}
