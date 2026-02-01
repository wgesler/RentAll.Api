using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Invoices;
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
				var invoice = dto.ToModel(CurrentUser);
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

		#region LedgerLine GET Endpoints

		/// <summary>
		/// Create initial leger lines
		/// </summary>
		/// <returns>Ledger Line</returns>
		[HttpPost("ledger-line/reservation")]
		public async Task<IActionResult> CreateLedgerLinesByReservationId([FromBody] CreateInvoiceMonthlyDataDto dto)
		{
			if (dto == null)
				return BadRequest("Invoice data is required");

			try
			{
				var reservation = await _reservationRepository.GetByIdAsync(dto.ReservationId, CurrentOrganizationId);
				if (reservation == null)
					return NotFound("Reservation not found");

				var ledgerLines = _accountingManager.GetLedgerLinesByReservationIdAsync(reservation, dto.StartDate, dto.EndDate);
				var data = new InvoiceMonthlyData { Invoice = dto.Invoice, ReservationId = dto.ReservationId, LedgerLines = ledgerLines };
				var response = new InvoiceMonthlyDataResponseDto(data);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting ledger lines: {ReservationId}", dto.ReservationId);
				return ServerError("An error occurred while retrieving the ledger line");
			}
		}
		#endregion

	}
}
