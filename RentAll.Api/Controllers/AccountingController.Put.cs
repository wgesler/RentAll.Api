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
		/// <param name="invoiceId">Invoice ID</param>
		/// <param name="dto">Invoice data</param>
		/// <returns>Updated invoice</returns>
		[HttpPut("invoice/{invoiceId}")]
		public async Task<IActionResult> UpdateInvoice(Guid invoiceId, [FromBody] UpdateInvoiceDto dto)
		{
			if (dto == null)
				return BadRequest("Invoice data is required");

			var (isValid, errorMessage) = dto.IsValid(invoiceId);
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid invoice data");

			try
			{
				var existingInvoice = await _invoiceRepository.GetByIdAsync(invoiceId, CurrentOrganizationId);
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
				_logger.LogError(ex, "Error updating invoice: {InvoiceId}", invoiceId);
				return ServerError("An error occurred while updating the invoice");
			}
		}

		#endregion

		#region LedgerLine PUT Endpoints

		/// <summary>
		/// Update an existing ledger line
		/// </summary>
		/// <param name="ledgerLineId">Ledger Line ID</param>
		/// <param name="dto">Ledger Line data</param>
		/// <returns>Updated ledger line</returns>
		[HttpPut("ledgerline/{ledgerLineId}")]
		public async Task<IActionResult> UpdateLedgerLine(int ledgerLineId, [FromBody] UpdateLedgerLineDto dto)
		{
			if (dto == null)
				return BadRequest("Ledger Line data is required");

			var (isValid, errorMessage) = dto.IsValid(ledgerLineId);
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid ledger line data");

			try
			{
				var existingLedgerLine = await _ledgerLineRepository.GetByIdAsync(ledgerLineId);
				if (existingLedgerLine == null)
					return NotFound("Ledger Line not found");

				var ledgerLine = dto.ToModel();

				var updatedLedgerLine = await _ledgerLineRepository.UpdateByIdAsync(ledgerLine);

				var response = new LedgerLineResponseDto(updatedLedgerLine);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating ledger line: {LedgerLineId}", ledgerLineId);
				return ServerError("An error occurred while updating the ledger line");
			}
		}

		#endregion
	}
}
