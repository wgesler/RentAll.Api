
namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {

        #region Get

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

        [HttpGet("lease-information/property/{propertyId}")]
        public async Task<IActionResult> GetLeaseInformationByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var leaseInformation = await _reservationRepository.GetLeaseInformationByPropertyIdAsync(propertyId, CurrentOrganizationId, property.OfficeId);
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

        [HttpGet("lease-information/scope")]
        public async Task<IActionResult> GetLeaseInformationByScope([FromQuery] int? officeId = null, [FromQuery] Guid? propertyId = null)
        {
            try
            {
                if (propertyId.HasValue)
                {
                    var property = await _propertyRepository.GetPropertyByIdAsync(propertyId.Value, CurrentOrganizationId);
                    if (property == null)
                        return NotFound("Property not found");

                    officeId = property.OfficeId;
                }

                var leaseInformation = await _reservationRepository.GetLeaseInformationByScopeAsync(CurrentOrganizationId, officeId, propertyId);
                if (leaseInformation == null)
                    return Ok();

                return Ok(new LeaseInformationResponseDto(leaseInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lease information by scope: {OrganizationId}-{OfficeId}-{PropertyId}", CurrentOrganizationId, officeId, propertyId);
                return ServerError("An error occurred while retrieving the lease information");
            }
        }

        #endregion

        #region Post

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
                if (dto.PropertyId.HasValue)
                {
                    var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId.Value, CurrentOrganizationId);
                    if (property == null)
                        return NotFound("Property not found");

                    dto.OfficeId = property.OfficeId;
                }

                var leaseInformation = dto.ToModel(CurrentUser);
                var createdLeaseInformation = await _reservationRepository.CreateLeaseInformationAsync(leaseInformation);

                var response = new LeaseInformationResponseDto(createdLeaseInformation);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lease information");
                return ServerError("An error occurred while creating the lease information");
            }
        }

        #endregion

        #region Put

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
                if (dto.PropertyId.HasValue)
                {
                    var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId.Value, CurrentOrganizationId);
                    if (property == null)
                        return NotFound("Property not found");

                    dto.OfficeId = property.OfficeId;
                }

                var leaseInformation = dto.ToModel(CurrentUser);

                // Check if lease information exists
                var existing = await _reservationRepository.GetLeaseInformationByScopeAsync(CurrentOrganizationId, dto.OfficeId, dto.PropertyId);
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
                _logger.LogError(ex, "Error updating lease information: {OrganizationId}-{OfficeId}-{PropertyId}", dto.OrganizationId, dto.OfficeId, dto.PropertyId);
                return ServerError("An error occurred while updating the lease information");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("lease-information/{reservationId}")]
        public async Task<IActionResult> DeleteLeaseInformationByIdAsync(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Lease Information ID is required");

            try
            {
                // Check if lease information exists
                var leaseInformation = await _reservationRepository.GetLeaseInformationByIdAsync(reservationId, CurrentOrganizationId);
                if (leaseInformation == null)
                    return NotFound("Lease information not found");

                await _reservationRepository.DeleteLeaseInformationByIdAsync(reservationId, CurrentOrganizationId, CurrentUser);
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
