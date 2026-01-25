using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Invoices;
using RentAll.Api.Dtos.LedgerLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers
{
	public partial class AccountingController
	{
		#region Invoice POST Endpoints

		/// <summary>
		/// Create a new invoice
		/// </summary>
		/// <param name="dto">Invoice data</param>
		/// <returns>Created invoice</returns>
		[HttpPost("invoice")]
		public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
		{
			if (dto == null)
				return BadRequest("Invoice data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid invoice data");

			try
			{
				var invoice = dto.ToModel();
				invoice.OrganizationId = CurrentOrganizationId;
				var createdInvoice = await _invoiceRepository.CreateAsync(invoice);

				await _accountingManager.ApplyInvoiceToReservationAsync(createdInvoice);
						
				var response = new InvoiceResponseDto(createdInvoice);
				return CreatedAtAction(nameof(GetInvoiceById), new { invoiceId = createdInvoice.InvoiceId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating invoice");
				return ServerError("An error occurred while creating the invoice");
			}
		}

		#endregion
	}
}
