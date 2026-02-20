
namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {

        #region Get

        /// <summary>
        /// Get lease information by ID
        /// </summary>
        /// <param name="reservationId">Lease Information ID</param>
        /// <returns>Lease information</returns>
        [HttpGet("lease-information/{reservationId}")]
        public async Task<IActionResult> GetLeaseInformationById(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Lease Information ID is required");

            try
            {
                var leaseInformation = await _reservationRepository.GetLeaseInformationByIdAsync(reservationId, CurrentOrganizationId);
                if (leaseInformation == null)
                    return NotFound("Lease information not found");

                return Ok(new LeaseInformationResponseDto(leaseInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lease information by ID: {LeaseInformationId}", reservationId);
                return ServerError("An error occurred while retrieving the lease information");
            }
        }

        /// <summary>
        /// Get lease information by Property ID
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Lease information</returns>
        [HttpGet("lease-information/property/{propertyId}")]
        public async Task<IActionResult> GetLeaseInformationByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var leaseInformation = await _reservationRepository.GetLeaseInformationByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (leaseInformation == null)
                    return Ok(); // Not required

                return Ok(new LeaseInformationResponseDto(leaseInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lease information by Property ID: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving the lease information");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new lease information
        /// </summary>
        /// <param name="dto">Lease information data</param>
        /// <returns>Created lease information</returns>
        [HttpPost("lease-information")]
        public async Task<IActionResult> CreateLeaseInformation([FromBody] CreateLeaseInformationDto dto)
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
                return CreatedAtAction(nameof(GetLeaseInformationById), new { id = createdLeaseInformation.PropertyId }, new LeaseInformationResponseDto(createdLeaseInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lease information");
                return ServerError("An error occurred while creating the lease information");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing lease information
        /// </summary>
        /// <param name="dto">Lease information data</param>
        /// <returns>Updated lease information</returns>
        [HttpPut("lease-information")]
        public async Task<IActionResult> UpdateLeaseInformation([FromBody] UpdateLeaseInformationDto dto)
        {
            if (dto == null)
                return BadRequest("Lease information data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var leaseInformation = dto.ToModel(CurrentUser);

                // Check if lease information exists
                var existing = await _reservationRepository.GetLeaseInformationByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (existing == null)
                {
                    var addLeaseInformation = await _reservationRepository.CreateLeaseInformationAsync(leaseInformation);
                    return Ok(new LeaseInformationResponseDto(addLeaseInformation));
                }
                else
                {
                    var updatedLeaseInformation = await _reservationRepository.UpdateLeaseInformationByIdAsync(leaseInformation);
                    return Ok(new LeaseInformationResponseDto(updatedLeaseInformation));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lease information: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while updating the lease information");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a lease information
        /// </summary>
        /// <param name="reservationId">Lease Information ID</param>
        /// <returns>No content</returns>
        [HttpDelete("lease-information/{reservationId}")]
        public async Task<IActionResult> DeleteLeaseInformation(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Lease Information ID is required");

            try
            {
                // Check if lease information exists
                var leaseInformation = await _reservationRepository.GetLeaseInformationByIdAsync(reservationId, CurrentOrganizationId);
                if (leaseInformation == null)
                    return NotFound("Lease information not found");

                await _reservationRepository.DeleteLeaseInformationByIdAsync(reservationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lease information: {LeaseInformationId}", reservationId);
                return ServerError("An error occurred while deleting the lease information");
            }
        }

        #endregion

    }
}
