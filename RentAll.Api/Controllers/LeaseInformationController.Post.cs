using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.LeaseInformations;

namespace RentAll.Api.Controllers
{
	public partial class LeaseInformationController
	{
		/// <summary>
		/// Create a new lease information
		/// </summary>
		/// <param name="dto">Lease information data</param>
		/// <returns>Created lease information</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateLeaseInformationDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Lease information data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				// Verify property belongs to organization
				var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
				if (property == null)
					return NotFound(new { message = "Property not found" });

				// Verify contact belongs to organization if ContactId is provided
				if (dto.ContactId.HasValue)
				{
					var contact = await _contactRepository.GetByIdAsync(dto.ContactId.Value, CurrentOrganizationId);
					if (contact == null)
						return NotFound(new { message = "Contact not found" });
				}

				var leaseInformation = dto.ToModel(CurrentUser);
				var createdLeaseInformation = await _leaseInformationRepository.CreateAsync(leaseInformation);
				return CreatedAtAction(nameof(GetById), new { id = createdLeaseInformation.LeaseInformationId }, new LeaseInformationResponseDto(createdLeaseInformation));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating lease information");
				return StatusCode(500, new { message = "An error occurred while creating the lease information" });
			}
		}
	}
}

