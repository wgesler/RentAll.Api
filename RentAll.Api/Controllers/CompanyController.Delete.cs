using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class CompanyController
    {
        /// <summary>
        /// Delete a company
        /// </summary>
        /// <param name="id">Company ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Company ID is required" });

            try
            {
                // Check if company exists
                var company = await _companyRepository.GetByIdAsync(id);
                if (company == null)
                    return NotFound(new { message = "Company not found" });

                await _companyRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company: {CompanyId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the company" });
            }
        }
    }
}

