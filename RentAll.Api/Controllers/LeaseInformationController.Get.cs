using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.LeaseInformations;

namespace RentAll.Api.Controllers
{
	public partial class LeaseInformationController
	{
		/// <summary>
		/// Get lease information by ID
		/// </summary>
		/// <param name="id">Lease Information ID</param>
		/// <returns>Lease information</returns>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(Guid id)
		{
			if (id == Guid.Empty)
				return BadRequest(new { message = "Lease Information ID is required" });

			try
			{
				var leaseInformation = await _leaseInformationRepository.GetByIdAsync(id, CurrentOrganizationId);
				if (leaseInformation == null)
					return NotFound(new { message = "Lease information not found" });

				return Ok(new LeaseInformationResponseDto(leaseInformation));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting lease information by ID: {LeaseInformationId}", id);
				return StatusCode(500, new { message = "An error occurred while retrieving the lease information" });
			}
		}

		/// <summary>
		/// Get lease information by Property ID
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>Lease information</returns>
		[HttpGet("property/{propertyId}")]
		public async Task<IActionResult> GetByPropertyId(Guid propertyId)
		{
			if (propertyId == Guid.Empty)
				return BadRequest(new { message = "Property ID is required" });

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				var leaseInformation = await _leaseInformationRepository.GetByPropertyIdAsync(propertyId, CurrentOrganizationId);
				if (leaseInformation == null)
					return NotFound(new { message = "Lease information not found" });

				return Ok(new LeaseInformationResponseDto(leaseInformation));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting lease information by Property ID: {PropertyId}", propertyId);
				return StatusCode(500, new { message = "An error occurred while retrieving the lease information" });
			}
		}
	}
}

