using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        /// <summary>
        /// Delete a contact
        /// </summary>
        /// <param name="id">Contact ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Contact ID is required");

            try
            {
                // Check if contact exists
                var contact = await _contactRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (contact == null)
                    return NotFound("Contact not found");

                await _contactRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact: {ContactId}", id);
                return ServerError("An error occurred while deleting the contact");
            }
        }
    }
}








