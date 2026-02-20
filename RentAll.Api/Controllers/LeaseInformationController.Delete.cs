using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class LeaseInformationController
    {
        /// <summary>
        /// Delete a lease information
        /// </summary>
        /// <param name="id">Lease Information ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Lease Information ID is required");

            try
            {
                // Check if lease information exists
                var leaseInformation = await _reservationRepository.GetLeaseInformationByIdAsync(id, CurrentOrganizationId);
                if (leaseInformation == null)
                    return NotFound("Lease information not found");

                await _reservationRepository.DeleteLeaseInformationByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lease information: {LeaseInformationId}", id);
                return ServerError("An error occurred while deleting the lease information");
            }
        }
    }
}

