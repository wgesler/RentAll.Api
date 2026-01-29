using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class AccountingOfficeController
	{
		/// <summary>
		/// Delete an accounting office
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <returns>No content</returns>
		[HttpDelete("{officeId}")]
		public async Task<IActionResult> Delete(int officeId)
		{
			if (officeId <= 0)
				return BadRequest("Office ID is required");

			try
			{
				// Check if accounting office exists
				var existingAccountingOffice = await _accountingOfficeRepository.GetByIdAsync(CurrentOrganizationId, officeId);
				if (existingAccountingOffice == null)
					return NotFound("Accounting office not found");

				// Delete logo if it exists
				if (!string.IsNullOrWhiteSpace(existingAccountingOffice.LogoPath))
				{
					await _fileService.DeleteLogoAsync(existingAccountingOffice.LogoPath);
				}

				await _accountingOfficeRepository.DeleteAsync(CurrentOrganizationId, officeId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting accounting office: {OfficeId}", officeId);
				return ServerError("An error occurred while deleting the accounting office");
			}
		}
	}
}
