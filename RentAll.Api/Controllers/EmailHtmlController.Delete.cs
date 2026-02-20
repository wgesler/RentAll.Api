using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class EmailHtmlController
    {
        /// <summary>
        /// Delete email html for organization.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            try
            {
                var existing = await _emailRepository.GetEmailHtmlByOrganizationIdAsync(CurrentOrganizationId);
                if (existing == null)
                    return NotFound("EmailHtml not found");

                await _emailRepository.DeleteEmailHtmlByOrganizationIdAsync(CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EmailHtml for organization: {OrganizationId}", CurrentOrganizationId);
                return ServerError("An error occurred while deleting EmailHtml");
            }
        }
    }
}
