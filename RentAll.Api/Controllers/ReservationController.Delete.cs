using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class ReservationController
    {
        /// <summary>
        /// Delete a reservation
        /// </summary>
        /// <param name="id">Reservation ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                // Check if reservation exists
                var reservation = await _reservationRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                await _reservationRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reservation: {ReservationId}", id);
                return ServerError("An error occurred while deleting the reservation");
            }
        }
    }
}


