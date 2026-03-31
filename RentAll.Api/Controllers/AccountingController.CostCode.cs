using RentAll.Api.Dtos.Accounting.CostCodes;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get
        [HttpGet("cost-code/office")]
        public async Task<IActionResult> GetCostCodesByOfficeIdsAsync()
        {
            try
            {
                var costCodes = await _accountingRepository.GetCostCodesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = costCodes.Select(c => new CostCodeResponseDto(c)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all cost codes");
                return ServerError("An error occurred while retrieving cost codes");
            }
        }

        [HttpGet("cost-code/office/{officeId:int}")]
        public async Task<IActionResult> GetCostCodesByOfficeIdAsync(int officeId)
        {
            try
            {
                if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                    return Unauthorized("You do not have access to this office's cost codes");

                var costCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(CurrentOrganizationId, officeId);
                var response = costCodes.Select(c => new CostCodeResponseDto(c)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all cost codes");
                return ServerError("An error occurred while retrieving cost codes");
            }
        }

        [HttpGet("cost-code/office/{officeId:int}/costCodeId/{costCodeId:int}")]
        public async Task<IActionResult> GetCostCodeByIdAsync(int officeId, int costCodeId)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's cost codes");

            if (costCodeId <= 0)
                return BadRequest("Invalid cost code");

            try
            {
                var costCode = await _accountingRepository.GetCostCodeByIdAsync(costCodeId, CurrentOrganizationId, officeId);
                if (costCode == null)
                    return NotFound("Cost Code not found");

                var response = new CostCodeResponseDto(costCode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost code by ID: {costCodeId}", costCodeId);
                return ServerError("An error occurred while retrieving the cost code");
            }
        }

        [HttpGet("cost-code/office/{officeId:int}/code/{code}")]
        public async Task<IActionResult> GetByCostCodeAsync(int officeId, string code)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's cost codes");

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Invalid code");

            try
            {
                var costCode = await _accountingRepository.GetByCostCodeAsync(code, CurrentOrganizationId, officeId);
                if (costCode == null)
                    return NotFound("Cost Code not found");

                var response = new CostCodeResponseDto(costCode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost code by code: {code}", code);
                return ServerError("An error occurred while retrieving the cost code");
            }
        }

        #endregion

        #region Post

        [HttpPost("cost-code")]
        public async Task<IActionResult> Create([FromBody] CreateCostCodeDto dto)
        {
            if (dto == null)
                return BadRequest("Cost Code data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid cost code request");

            try
            {
                var costCode = dto.ToModel();
                costCode.OrganizationId = CurrentOrganizationId;
                var createdCostCode = await _accountingRepository.CreateAsync(costCode);

                var response = new CostCodeResponseDto(createdCostCode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cost code");
                return ServerError("An error occurred while creating the cost code");
            }
        }
        #endregion

        #region Put
        [HttpPut("cost-code")]
        public async Task<IActionResult> Update([FromBody] UpdateCostCodeDto dto)
        {
            if (dto == null)
                return BadRequest("Cost Code data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid cost code request");

            try
            {
                var existingCostCode = await _accountingRepository.GetCostCodeByIdAsync(dto.CostCodeId, CurrentOrganizationId, dto.OfficeId);
                if (existingCostCode == null)
                    return NotFound("Cost Code not found");

                var costCode = dto.ToModel();
                costCode.OrganizationId = CurrentOrganizationId;
                var updatedCostCode = await _accountingRepository.UpdateByIdAsync(costCode);

                var response = new CostCodeResponseDto(updatedCostCode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cost code: {CostCodeId}", dto.CostCodeId);
                return ServerError("An error occurred while updating the cost code");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("cost-code/office/{officeId:int}/costcodeid/{costCodeId:int}")]
        public async Task<IActionResult> DeleteCostCodeByIdAsync(int officeId, int costCodeId)
        {
            if (costCodeId <= 0)
                return BadRequest("Invalid Cost Code ID");

            try
            {
                await _accountingRepository.DeleteCostCodeByIdAsync(costCodeId, CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cost code: {costCodeId}", costCodeId);
                return ServerError("An error occurred while deleting the cost code");
            }
        }

        #endregion
    }
}
