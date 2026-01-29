using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Invoices;
using RentAll.Api.Dtos.LedgerLines;

namespace RentAll.Api.Controllers
{
	public partial class AccountingController
	{
		#region Invoice PUT Endpoints

		/// <summary>
		/// Update an existing invoice
		/// </summary>
		/// <param name="dto">Invoice data</param>
		/// <returns>Updated invoice</returns>
		[HttpPut("invoice")]
		public async Task<IActionResult> UpdateInvoice([FromBody] UpdateInvoiceDto dto)
		{
			if (dto == null)
				return BadRequest("Invoice data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid invoice data");

			try
			{
				var existingInvoice = await _invoiceRepository.GetByIdAsync(dto.InvoiceId, CurrentOrganizationId);
				if (existingInvoice == null)
					return NotFound("Invoice not found");

				var invoice = dto.ToModel(CurrentUser);
				invoice.OrganizationId = CurrentOrganizationId;

				var updatedInvoice = await _invoiceRepository.UpdateByIdAsync(invoice);

				var response = new InvoiceResponseDto(updatedInvoice);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating invoice: {InvoiceId}", dto.InvoiceId);
				return ServerError("An error occurred while updating the invoice");
			}
		}

		#endregion
	}
}
