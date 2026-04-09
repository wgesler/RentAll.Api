namespace RentAll.Api.Controllers
{
    public partial class ContactController
    {
        [HttpDelete("{contactId}")]
        public async Task<IActionResult> DeleteContactByIdAsync(Guid contactId)
        {
            if (contactId == Guid.Empty)
                return BadRequest("Contact ID is required");

            try
            {
                // Check if contact exists then check/delete w9 and insurance files
                var existing = await _contactRepository.GetContactByIdAsync(contactId, CurrentOrganizationId);
                if (existing != null)
                {
                    // Get the office name for file storage path
                    var office = await _organizationRepository.GetOfficeByIdAsync(existing.OfficeId, existing.OrganizationId);
                    var officeName = office != null ? office.Name : null;

                    if (!string.IsNullOrWhiteSpace(existing.W9Path))
                        await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.W9Path, ImageType.W9Forms);
                    if (!string.IsNullOrEmpty(existing.InsurancePath))
                        await _fileService.DeleteImageAsync(existing.OrganizationId, officeName, existing.InsurancePath, ImageType.Insurances);
                }

                await _contactRepository.DeleteContactByIdAsync(contactId, CurrentUser);
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








