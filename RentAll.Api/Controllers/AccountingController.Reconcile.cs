using RentAll.Api.Dtos.Accounting.Reconcile;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpGet("reconcile/office/{officeId:int}/account/{accountId:int}")]
    public async Task<IActionResult> GetReconcilesByAccountId(int officeId, int accountId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        if (accountId <= 0)
            return BadRequest("AccountId is required");

        if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
            return BadRequest("Unauthorized");

        try
        {
            var reconciles = await _accountingRepository.GetReconcilesByAccountIdAsync(CurrentOrganizationId, officeId, accountId);
            return Ok(reconciles.Select(r => new ReconcileResponseDto(r)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reconciles for account {AccountId}", accountId);
            return ServerError("An error occurred while retrieving reconciles");
        }
    }

    [HttpGet("reconcile/office/{officeId:int}/reconcileId/{reconcileId:int}")]
    public async Task<IActionResult> GetReconcileById(int officeId, int reconcileId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        if (reconcileId <= 0)
            return BadRequest("ReconcileId is required");

        if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
            return BadRequest("Unauthorized");

        try
        {
            var reconcile = await _accountingRepository.GetReconcileByIdAsync(reconcileId, CurrentOrganizationId, officeId);
            if (reconcile == null)
                return NotFound("Reconcile not found");

            return Ok(new ReconcileResponseDto(reconcile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reconcile {ReconcileId}", reconcileId);
            return ServerError("An error occurred while retrieving the reconcile");
        }
    }

    #endregion

    #region Post

    [HttpPost("reconcile")]
    public async Task<IActionResult> CreateReconcile([FromBody] CreateReconcileDto dto)
    {
        if (dto == null)
            return BadRequest("Reconcile data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid reconcile data");

        try
        {
            var chartOfAccount = await _accountingRepository.GetChartOfAccountByIdAsync(CurrentOrganizationId, dto.OfficeId, dto.AccountId);
            if (chartOfAccount == null)
                return NotFound("Chart of account not found");

            var reconcile = await _accountingRepository.CreateReconcileAsync(dto.ToModel(CurrentOrganizationId));
            return Ok(new ReconcileResponseDto(reconcile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reconcile for account {AccountId}", dto.AccountId);
            return ServerError("An error occurred while creating the reconcile");
        }
    }

    #endregion

    #region Put

    [HttpPut("reconcile")]
    public async Task<IActionResult> UpdateReconcile([FromBody] UpdateReconcileDto dto)
    {
        if (dto == null)
            return BadRequest("Reconcile data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid reconcile data");

        try
        {
            var existing = await _accountingRepository.GetReconcileByIdAsync(dto.ReconcileId, CurrentOrganizationId, dto.OfficeId);
            if (existing == null)
                return NotFound("Reconcile not found");

            var chartOfAccount = await _accountingRepository.GetChartOfAccountByIdAsync(CurrentOrganizationId, dto.OfficeId, dto.AccountId);
            if (chartOfAccount == null)
                return NotFound("Chart of account not found");

            var reconcile = await _accountingRepository.UpdateReconcileByIdAsync(dto.ToModel(CurrentOrganizationId));
            return Ok(new ReconcileResponseDto(reconcile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reconcile {ReconcileId}", dto.ReconcileId);
            return ServerError("An error occurred while updating the reconcile");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("reconcile/office/{officeId:int}/reconcileId/{reconcileId:int}")]
    public async Task<IActionResult> DeleteReconcileById(int officeId, int reconcileId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        if (reconcileId <= 0)
            return BadRequest("ReconcileId is required");

        if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
            return BadRequest("Unauthorized");

        try
        {
            var existing = await _accountingRepository.GetReconcileByIdAsync(reconcileId, CurrentOrganizationId, officeId);
            if (existing == null)
                return NotFound("Reconcile not found");

            await _accountingRepository.DeleteReconcileByIdAsync(reconcileId, CurrentOrganizationId, officeId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reconcile {ReconcileId}", reconcileId);
            return ServerError("An error occurred while deleting the reconcile");
        }
    }

    #endregion
}
