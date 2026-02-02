using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Buildings;

namespace RentAll.Api.Controllers
{
	public partial class BuildingController
	{
		/// <summary>
		/// Get all buildings
		/// </summary>
		/// <returns>List of buildings</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var buildings = await _buildingRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
				var response = buildings.Select(b => new BuildingResponseDto(b));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all buildings");
				return ServerError("An error occurred while retrieving buildings");
			}
		}

		/// <summary>
		/// Get building by ID
		/// </summary>
		/// <param name="id">Building ID</param>
		/// <returns>Building</returns>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			if (id <= 0)
				return BadRequest("Building ID is required");

			try
			{
				var building = await _buildingRepository.GetByIdAsync(id, CurrentOrganizationId);
				if (building == null)
					return NotFound("Building not found");

				return Ok(new BuildingResponseDto(building));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting building by ID: {BuildingId}", id);
				return ServerError("An error occurred while retrieving the building");
			}
		}
	}
}





