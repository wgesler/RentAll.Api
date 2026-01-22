using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Invoices;
using RentAll.Api.Dtos.LedgerLines;

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
				var invoice = dto.ToModel(CurrentUser);
				invoice.OrganizationId = CurrentOrganizationId;

				var createdInvoice = await _invoiceRepository.CreateAsync(invoice);

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

		#region LedgerLine POST Endpoints

		/// <summary>
		/// Create a new ledger line
		/// </summary>
		/// <param name="dto">Ledger Line data</param>
		/// <returns>Created ledger line</returns>
		[HttpPost("ledgerline")]
		public async Task<IActionResult> CreateLedgerLine([FromBody] CreateLedgerLineDto dto)
		{
			if (dto == null)
				return BadRequest("Ledger Line data is required");

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid ledger line data");

			try
			{
				var ledgerLine = dto.ToModel();

				var createdLedgerLine = await _ledgerLineRepository.CreateAsync(ledgerLine);

				var response = new LedgerLineResponseDto(createdLedgerLine);
				return CreatedAtAction(nameof(GetLedgerLineById), new { ledgerLineId = createdLedgerLine.LedgerLineId }, response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating ledger line");
				return ServerError("An error occurred while creating the ledger line");
			}
		}

		#endregion
	}
}
