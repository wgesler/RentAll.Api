using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Accounting.Invoices;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get

        /// <summary>
        /// Get all invoices by offices
        /// </summary>
        /// <returns>List of invoices</returns>
        [HttpGet("invoices/invoice")]
        public async Task<IActionResult> GetAllInvoices()
        {
            try
            {
                var invoices = await _accountingRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
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
        [HttpGet("invoices/invoice/office/{officeId:int}")]
        public async Task<IActionResult> GetAllInvoicesByOffice(int officeId)
        {
            try
            {
                if (!CurrentOfficeAccess.Contains(officeId.ToString()))
                    return Unauthorized("No access to this office");

                var invoices = await _accountingRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, officeId.ToString());
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
        [HttpGet("invoices/invoice/reservation/{reservationId}")]
        public async Task<IActionResult> GetAllInvoicesByReservation(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                var invoices = await _accountingRepository.GetAllByReservationIdAsync(reservationId, CurrentOrganizationId, CurrentOfficeAccess);
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
        [HttpGet("invoices/invoice/property/{propertyId}")]
        public async Task<IActionResult> GetAllInvoicesByProperty(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var invoices = await _accountingRepository.GetAllByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
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
        [HttpGet("invoices/invoice/{invoiceId}")]
        public async Task<IActionResult> GetInvoiceById(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return BadRequest("Invoice ID is required");

            try
            {
                var invoice = await _accountingRepository.GetByIdAsync(invoiceId, CurrentOrganizationId);
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

        #region Post

        /// <summary>
        /// Create a new invoice
        /// </summary>
        /// <param name="dto">Invoice data</param>
        /// <returns>Created invoice</returns>
        [HttpPost("invoices/invoice")]
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
                var createdInvoice = await _accountingRepository.CreateAsync(invoice);

                var response = new InvoiceResponseDto(createdInvoice);
                return CreatedAtAction(nameof(GetInvoiceById), new { invoiceId = createdInvoice.InvoiceId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return ServerError("An error occurred while creating the invoice");
            }
        }

        /// <summary>
        /// Create initial leger lines
        /// </summary>
        /// <returns>Ledger Line</returns>
        [HttpPost("invoices/ledger-line/reservation")]
        public async Task<IActionResult> CreateLedgerLinesByReservationId([FromBody] CreateInvoiceMonthlyDataDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice data is required");

            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(dto.ReservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var ledgerLines = await _accountingManager.CreateLedgerLinesForReservationIdAsync(reservation, dto.StartDate, dto.EndDate);
                var data = new InvoiceMonthlyData { InvoiceCode = dto.InvoiceCode, ReservationId = dto.ReservationId, LedgerLines = ledgerLines };
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

        #region Put

        /// <summary>
        /// Update an existing invoice
        /// </summary>
        /// <param name="dto">Invoice data</param>
        /// <returns>Updated invoice</returns>
        [HttpPut("invoices/invoice")]
        public async Task<IActionResult> UpdateInvoice([FromBody] UpdateInvoiceDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid invoice data");

            try
            {
                var existingInvoice = await _accountingRepository.GetByIdAsync(dto.InvoiceId, CurrentOrganizationId);
                if (existingInvoice == null)
                    return NotFound("Invoice not found");

                var invoice = dto.ToModel(CurrentUser);
                invoice.OrganizationId = CurrentOrganizationId;

                var updatedInvoice = await _accountingRepository.UpdateByIdAsync(invoice);

                var response = new InvoiceResponseDto(updatedInvoice);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice: {InvoiceId}", dto.InvoiceId);
                return ServerError("An error occurred while updating the invoice");
            }
        }

        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="dto">Reservation data</param>
        /// <returns>Updated reservation</returns>
        [HttpPut("invoices/payment")]
        public async Task<IActionResult> ApplyPayment([FromBody] InvoicePaymentRequestDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice payment data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var invoicePayment = await _accountingManager.ApplyPaymentToInvoicesAsync(dto.Invoices, CurrentOrganizationId, CurrentOfficeAccess,
                    dto.CostCodeId, dto.Description, dto.Amount, CurrentUser);
                var response = new InvoicePaymentResponseDto(invoicePayment);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying payments to invoices");
                return ServerError("An error occurred while applying payments to invoices");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete an invoice
        /// </summary>
        /// <param name="invoiceId">Invoice ID</param>
        /// <returns>No content</returns>
        [HttpDelete("invoices/invoice/{invoiceId}")]
        public async Task<IActionResult> DeleteInvoice(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return BadRequest("Invoice ID is required");

            try
            {
                var existingInvoice = await _accountingRepository.GetByIdAsync(invoiceId, CurrentOrganizationId);
                if (existingInvoice == null)
                    return NotFound("Invoice not found");

                await _accountingRepository.DeleteByIdAsync(invoiceId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice: {InvoiceId}", invoiceId);
                return ServerError("An error occurred while deleting the invoice");
            }
        }

        #endregion
    }
}
