using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Invoices;
using RentAll.Api.Dtos.LedgerLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers
{
	public partial class AccountingController
	{
		#region Invoice GET Endpoints

		/// <summary>
		/// Get all invoices by offices
		/// </summary>
		/// <returns>List of invoices</returns>
		[HttpGet("invoice/office")]
		public async Task<IActionResult> GetAllInvoicesByOffice()
		{
			try
			{
				var invoices = await _invoiceRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
				var response = invoices.Select(i => new InvoiceResponseDto(i)).ToList();
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting invoices by office");
				return ServerError("An error occurred while retrieving invoices");
			}
		}

		/// <summary>
		/// Get all invoices by reservation ID
		/// </summary>
		/// <param name="reservationId">Reservation ID</param>
		/// <returns>List of invoices</returns>
		[HttpGet("invoice/reservation/{reservationId}")]
		public async Task<IActionResult> GetAllInvoicesByReservation(Guid reservationId)
		{
			if (reservationId == Guid.Empty)
				return BadRequest("Reservation ID is required");

			try
			{
				var invoices = await _invoiceRepository.GetAllByReservationIdAsync(reservationId, CurrentOrganizationId, CurrentOfficeAccess);
				var response = invoices.Select(i => new InvoiceResponseDto(i)).ToList();
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting invoices by reservation ID: {ReservationId}", reservationId);
				return ServerError("An error occurred while retrieving invoices");
			}
		}

		/// <summary>
		/// Get all invoices by property ID
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <returns>List of invoices</returns>
		[HttpGet("invoice/property/{propertyId}")]
		public async Task<IActionResult> GetAllInvoicesByProperty(Guid propertyId)
		{
			if (propertyId == Guid.Empty)
				return BadRequest("Property ID is required");

			try
			{
				var invoices = await _invoiceRepository.GetAllByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
				var response = invoices.Select(i => new InvoiceResponseDto(i)).ToList();
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting invoices by property ID: {PropertyId}", propertyId);
				return ServerError("An error occurred while retrieving invoices");
			}
		}

		/// <summary>
		/// Get invoice by ID
		/// </summary>
		/// <param name="invoiceId">Invoice ID</param>
		/// <returns>Invoice</returns>
		[HttpGet("invoice/{invoiceId}")]
		public async Task<IActionResult> GetInvoiceById(Guid invoiceId)
		{
			if (invoiceId == Guid.Empty)
				return BadRequest("Invoice ID is required");

			try
			{
				var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, CurrentOrganizationId);
				if (invoice == null)
					return NotFound("Invoice not found");

				var response = new InvoiceResponseDto(invoice);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting invoice by ID: {InvoiceId}", invoiceId);
				return ServerError("An error occurred while retrieving the invoice");
			}
		}

		#endregion

		#region LedgerLine GET Endpoints

		/// <summary>
		/// Get initial leger lines
		/// </summary>
		/// <param name="reservationId">Reservation ID</param>
		/// <returns>Ledger Line</returns>
		[HttpGet("ledgerline/reservation/{reservationId:guid}")]
		public async Task<IActionResult> GetLedgerLinesByReservationId(Guid reservationId)
		{
			if (reservationId == Guid.Empty)
				return BadRequest("Reservation ID is required");

			try
			{
				var reservation = await _reservationRepository.GetByIdAsync(reservationId, CurrentOrganizationId);
				if (reservation == null)
					return NotFound("Reservation not found");

				var ledgerLines = _accountingManager.GetLedgerLinesByReservationIdAsync(reservation);
				var invoice = reservation.ReservationCode + " " + (reservation.CurrentInvoiceNumber + 1).ToString("D3");
				var data = new InvoiceMonthlyData { Invoice = invoice, ReservationId = reservation.ReservationId, LedgerLines = ledgerLines };
				var response = new InvoiceMonthlyDataResponseDto(data);
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting ledger lines: {ReservationId}", reservationId);
				return ServerError("An error occurred while retrieving the ledger line");
			}
		}
		#endregion
	}
}
