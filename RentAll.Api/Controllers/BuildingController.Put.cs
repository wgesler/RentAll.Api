using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Buildings;

namespace RentAll.Api.Controllers
{
	public partial class BuildingController
	{
		/// <summary>
		/// Update an existing building
		/// </summary>
		/// <param name="dto">Building data</param>
		/// <returns>Updated building</returns>
		[HttpPut]
		public async Task<IActionResult> Update([FromBody] BuildingUpdateDto dto)
		{
			if (dto == null)
				return BadRequest("Building data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var existingBuilding = await _buildingRepository.GetByIdAsync(dto.BuildingId, CurrentOrganizationId);
				if (existingBuilding == null)
					return NotFound("Building not found");

				// Check if BuildingCode is being changed and if the new code already exists
				if (existingBuilding.BuildingCode != dto.BuildingCode)
				{
					if (await _buildingRepository.ExistsByBuildingCodeAsync(dto.BuildingCode, CurrentOrganizationId, dto.OfficeId))
						return Conflict("Building Code already exists");
				}

				var building = dto.ToModel();
				var updatedBuilding = await _buildingRepository.UpdateByIdAsync(building);
				return Ok(new BuildingResponseDto(updatedBuilding));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating building: {BuildingId}", dto.BuildingId);
				return ServerError("An error occurred while updating the building");
			}
		}
	}
}





