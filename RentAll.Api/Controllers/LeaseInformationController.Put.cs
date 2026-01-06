using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.LeaseInformations;

namespace RentAll.Api.Controllers
{
	public partial class LeaseInformationController
	{
		/// <summary>
		/// Update an existing lease information
		/// </summary>
		/// <param name="dto">Lease information data</param>
		/// <returns>Updated lease information</returns>
		[HttpPut("")]
		public async Task<IActionResult> Update([FromBody] UpdateLeaseInformationDto dto)
		{
			if (dto == null)
				return BadRequest("Lease information data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				// Check if lease information exists
				var existing = await _leaseInformationRepository.GetByIdAsync(dto.LeaseInformationId, CurrentOrganizationId);
				if (existing == null)
					return NotFound("Lease information not found");

				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound("Property not found");

				// Verify contact belongs to organization if ContactId is provided
				if (dto.ContactId.HasValue)
				{
					var contact = await _contactRepository.GetByIdAsync(dto.ContactId.Value, CurrentOrganizationId);
					if (contact == null)
						return NotFound("Contact not found");
				}

				var leaseInformation = dto.ToModel(CurrentUser);
				var updatedLeaseInformation = await _leaseInformationRepository.UpdateByIdAsync(leaseInformation);
				return Ok(new LeaseInformationResponseDto(updatedLeaseInformation));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating lease information: {LeaseInformationId}", dto.LeaseInformationId);
				return ServerError("An error occurred while updating the lease information");
			}
		}
	}
}

