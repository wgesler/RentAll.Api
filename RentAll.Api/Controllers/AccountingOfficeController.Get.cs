using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.AccountingOffices;

namespace RentAll.Api.Controllers
{
	public partial class AccountingOfficeController
	{
		/// <summary>
		/// Get all accounting offices
		/// </summary>
		/// <returns>List of accounting offices</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var accountingOffices = await _accountingOfficeRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
				var response = new List<AccountingOfficeResponseDto>();
				foreach (var accountingOffice in accountingOffices)
				{
					var dto = new AccountingOfficeResponseDto(accountingOffice);
					if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
						dto.FileDetails = await _fileService.GetFileDetailsAsync(accountingOffice.LogoPath);

					response.Add(dto);
				}
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all accounting offices");
				return ServerError("An error occurred while retrieving accounting offices");
			}
		}

		/// <summary>
		/// Get accounting office by ID
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <returns>Accounting office</returns>
		[HttpGet("{officeId}")]
		public async Task<IActionResult> GetById(int officeId)
		{
			if (officeId <= 0)
				return BadRequest("Office ID is required");

			try
			{
				var accountingOffice = await _accountingOfficeRepository.GetByIdAsync(CurrentOrganizationId, officeId);
				if (accountingOffice == null)
					return NotFound("Accounting office not found");

				var response = new AccountingOfficeResponseDto(accountingOffice);
				if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
					response.FileDetails = await _fileService.GetFileDetailsAsync(accountingOffice.LogoPath);

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting accounting office by ID: {OfficeId}", officeId);
				return ServerError("An error occurred while retrieving the accounting office");
			}
		}
	}
}
