using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Accounting.CostCodes;

namespace RentAll.Api.Controllers
{
    public partial class CostCodeController
    {
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
    }
}
