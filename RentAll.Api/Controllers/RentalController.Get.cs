using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Rentals;

namespace RentAll.Api.Controllers
{
    public partial class RentalController
    {
        /// <summary>
        /// Get rental by ID
        /// </summary>
        /// <param name="id">Rental ID</param>
        /// <returns>Rental</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Rental ID is required" });

            try
            {
                var rental = await _rentalRepository.GetByIdAsync(id);
                if (rental == null)
                    return NotFound(new { message = "Rental not found" });

                return Ok(new RentalResponseDto(rental));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rental by ID: {RentalId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the rental" });
            }
        }

        /// <summary>
        /// Get active rentals
        /// </summary>
        /// <returns>List of active rentals</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveRentals()
        {
            try
            {
                var rentals = await _rentalRepository.GetActiveRentalsAsync();
                var response = rentals.Select(r => new RentalResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active rentals");
                return StatusCode(500, new { message = "An error occurred while retrieving active rentals" });
            }
        }

        /// <summary>
        /// Get rentals by Property ID
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>List of rentals</returns>
        [HttpGet("property/{propertyId}")]
        public async Task<IActionResult> GetByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest(new { message = "Property ID is required" });

            try
            {
                var rentals = await _rentalRepository.GetByPropertyIdAsync(propertyId);
                var response = rentals.Select(r => new RentalResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rentals by PropertyId: {PropertyId}", propertyId);
                return StatusCode(500, new { message = "An error occurred while retrieving rentals" });
            }
        }

        /// <summary>
        /// Get rentals by Contact ID
        /// </summary>
        /// <param name="contactId">Contact ID</param>
        /// <returns>List of rentals</returns>
        [HttpGet("contact/{contactId}")]
        public async Task<IActionResult> GetByContactId(Guid contactId)
        {
            if (contactId == Guid.Empty)
                return BadRequest(new { message = "Contact ID is required" });

            try
            {
                var rentals = await _rentalRepository.GetByContactIdAsync(contactId);
                var response = rentals.Select(r => new RentalResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rentals by ContactId: {ContactId}", contactId);
                return StatusCode(500, new { message = "An error occurred while retrieving rentals" });
            }
        }
    }
}