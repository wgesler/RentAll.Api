using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Accounting.CostCodes;

namespace RentAll.Api.Controllers
{
    public partial class CostCodeController
    {
        /// <summary>
        /// Create a new cost code
        /// </summary>
        /// <param name="dto">Cost Code data</param>
        /// <returns>Created cost code</returns>
        [HttpPost]
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
    }
}
