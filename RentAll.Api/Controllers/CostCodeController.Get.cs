using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.CostCodes;

namespace RentAll.Api.Controllers
{
	public partial class CostCodeController
	{
		/// <summary>
		/// Get all cost codes
		/// </summary>
		/// <returns>List of cost codes</returns>
		[HttpGet("office")]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var costCodes = await _costCodeRepository.GetAllAsync(CurrentOfficeAccess, CurrentOrganizationId);
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
		[HttpGet("office/{officeId:int}")]
		public async Task<IActionResult> GetByOfficeId(int officeId)
		{
			try
			{
				if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
					return Unauthorized("You do not have access to this office's cost codes");

				var costCodes = await _costCodeRepository.GetAllByOfficeIdAsync(officeId, CurrentOrganizationId);
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
		[HttpGet("office/{officeId:int}/costCodeId/{costCodeId:int}")]
		public async Task<IActionResult> GetByCostCodeId(int officeId, int costCodeId)
		{
			if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
				return Unauthorized("You do not have access to this office's cost codes");

			if (costCodeId <= 0)
				return BadRequest("Invalid cost code");

			try
			{
				var costCode = await _costCodeRepository.GetByIdAsync(costCodeId, officeId, CurrentOrganizationId);
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
		[HttpGet("office/{officeId:int}/code/{code}")]
		public async Task<IActionResult> GetByCode(int officeId, string code)
		{
			if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == officeId))
				return Unauthorized("You do not have access to this office's cost codes");

			if (string.IsNullOrWhiteSpace(code))
				return BadRequest("Invalid code");

			try
			{
				var costCode = await _costCodeRepository.GetByCostCodeAsync(code, officeId, CurrentOrganizationId);
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
	}
}
