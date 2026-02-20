using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Reservations.LeaseInformations;

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
    }
}

