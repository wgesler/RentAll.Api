namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        /// <summary>
        /// Delete a contact
        /// </summary>
        /// <param name="contactId">Contact ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{contactId}")]
        public async Task<IActionResult> Delete(Guid contactId)
        {
            if (contactId == Guid.Empty)
                return BadRequest("Contact ID is required");

            try
            {
                // Check if contact exists
                var contact = await _contactRepository.GetByIdAsync(contactId, CurrentOrganizationId);
                if (contact == null)
                    return NotFound("Contact not found");

                await _contactRepository.DeleteByIdAsync(contactId, CurrentUser);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact: {ContactId}", contactId);
                return ServerError("An error occurred while deleting the contact");
            }
        }
    }
}








