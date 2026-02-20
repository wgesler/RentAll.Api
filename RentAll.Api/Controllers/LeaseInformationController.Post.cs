using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations.LeaseInformations;

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
                return BadRequest("Lease information data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
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
                var createdLeaseInformation = await _reservationRepository.CreateLeaseInformationAsync(leaseInformation);
                return CreatedAtAction(nameof(GetById), new { id = createdLeaseInformation.PropertyId }, new LeaseInformationResponseDto(createdLeaseInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lease information");
                return ServerError("An error occurred while creating the lease information");
            }
        }
    }
}

