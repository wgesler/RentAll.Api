using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Invoices;

namespace RentAll.Api.Controllers
{
	public partial class AccountingController
	{
		#region Invoice GET Endpoints

		/// <summary>
		/// Get all invoices by offices
		/// </summary>
		/// <returns>List of invoices</returns>
		[HttpGet("invoice")]
		public async Task<IActionResult> GetAllInvoices()
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
		/// Get all invoices by offices
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <returns>List of invoices</returns>
		[HttpGet("invoice/office/{officeId:int}")]
		public async Task<IActionResult> GetAllInvoicesByOffice(int officeId)
		{
			try
			{
				if (!CurrentOfficeAccess.Contains(officeId.ToString()))
					return Unauthorized("No access to this office");

				var invoices = await _invoiceRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, officeId.ToString());
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
	}
}
