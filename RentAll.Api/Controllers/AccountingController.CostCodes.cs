using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Accounting.CostCodes;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get

        /// <summary>
        /// Get all cost codes
        /// </summary>
        /// <returns>List of cost codes</returns>
        [HttpGet("cost-codes/office")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var costCodes = await _accountingRepository.GetAllAsync(CurrentOfficeAccess, CurrentOrganizationId);
                var response = costCodes.Select(c => new CostCodeResponseDto(c)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all cost codes");
                return ServerError("An error occurred while retrieving cost codes");
            }
        }

        /// <summary>
        /// Get all cost codes
        /// </summary>
        /// <returns>List of cost codes</returns>
        [HttpGet("cost-codes/office/{officeId:int}")]
        public async Task<IActionResult> GetByOfficeId(int officeId)
        {
            try
            {
                if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                    return Unauthorized("You do not have access to this office's cost codes");

                var costCodes = await _accountingRepository.GetAllByOfficeIdAsync(officeId, CurrentOrganizationId);
                var response = costCodes.Select(c => new CostCodeResponseDto(c)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all cost codes");
                return ServerError("An error occurred while retrieving cost codes");
            }
        }

        /// <summary>
        /// Get cost code by ID
        /// </summary>
        /// <param name="officeId">Office ID</param>
        /// <param name="costCodeId">Cost Code ID</param>
        /// <returns>Cost Code</returns>
        [HttpGet("cost-codes/office/{officeId:int}/costCodeId/{costCodeId:int}")]
        public async Task<IActionResult> GetByCostCodeId(int officeId, int costCodeId)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's cost codes");

            if (costCodeId <= 0)
                return BadRequest("Invalid cost code");

            try
            {
                var costCode = await _accountingRepository.GetByIdAsync(costCodeId, officeId, CurrentOrganizationId);
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

        /// <summary>
        /// Get cost code by code
        /// </summary>
        /// <param name="officeId">Office ID</param>
        /// <param name="code">Cost Code</param>
        /// <returns>Cost Code</returns>
        [HttpGet("cost-codes/office/{officeId:int}/code/{code}")]
        public async Task<IActionResult> GetByCode(int officeId, string code)
        {
            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
                return Unauthorized("You do not have access to this office's cost codes");

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Invalid code");

            try
            {
                var costCode = await _accountingRepository.GetByCostCodeAsync(code, officeId, CurrentOrganizationId);
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

        /// <summary>
        /// Create a new cost code
        /// </summary>
        /// <param name="dto">Cost Code data</param>
        /// <returns>Created cost code</returns>
        [HttpPost("cost-codes")]
        public async Task<IActionResult> Create([FromBody] CreateCostCodeDto dto)
        {
            if (dto == null)
                return BadRequest("Cost Code data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid cost code request");

            try
            {
                if (await _accountingRepository.ExistsByCostCodeAsync(dto.CostCode, dto.OfficeId, CurrentOrganizationId))
                    return Conflict("Cost Code already exists");

                var costCode = dto.ToModel();
                costCode.OrganizationId = CurrentOrganizationId;

                var createdCostCode = await _accountingRepository.CreateAsync(costCode);

                var response = new CostCodeResponseDto(createdCostCode);
                return CreatedAtAction(nameof(GetByCostCodeId), new { officeId = createdCostCode.OfficeId, costCodeId = createdCostCode.CostCodeId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cost code");
                return ServerError("An error occurred while creating the cost code");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing cost code
        /// </summary>
        /// <param name="dto">Cost Code data</param>
        /// <returns>Updated cost code</returns>
        [HttpPut()]
        public async Task<IActionResult> Update([FromBody] UpdateCostCodeDto dto)
        {
            if (dto == null)
                return BadRequest("Cost Code data is required");

            var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid cost code request");

            try
            {
                var existingCostCode = await _accountingRepository.GetByIdAsync(dto.CostCodeId, dto.OfficeId, CurrentOrganizationId);
                if (existingCostCode == null)
                    return NotFound("Cost Code not found");

                if (existingCostCode.Code != dto.CostCode)
                {
                    if (await _accountingRepository.ExistsByCostCodeAsync(dto.CostCode, dto.OfficeId, CurrentOrganizationId))
                        return Conflict("Cost Code already exists");
                }

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

        /// <summary>
        /// Delete a cost code
        /// </summary>
        /// <param name="officeId">Office ID</param>
        /// <param name="costCodeId">Cost Code ID</param>
        /// <returns>No content</returns>
        [HttpDelete("cost-codes/office/{officeId:int}/costcodeid/{costCodeId:int}")]
        public async Task<IActionResult> Delete(int officeId, int costCodeId)
        {
            if (costCodeId <= 0)
                return BadRequest("Invalid Cost Code ID");

            try
            {
                await _accountingRepository.DeleteByIdAsync(costCodeId, officeId, CurrentOrganizationId);
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
