using RentAll.Api.Dtos.Accounting.Invoices;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
        #region Get

        [HttpPost("invoice/search")]
        public async Task<IActionResult> SearchInvoices([FromBody] GetInvoiceDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice search criteria is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var criteria = dto.ToCriteria(CurrentOrganizationId);
                var invoices = await _accountingRepository.GetInvoicesAsync(criteria);
                var response = invoices.Select(i => new InvoiceResponseDto(i)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching invoices");
                return ServerError("An error occurred while retrieving invoices");
            }
        }

        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GetInvoiceById(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return BadRequest("Invoice ID is required");

            try
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, CurrentOrganizationId);
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
        [HttpPost("invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid invoice data");

            var periodCheck = await RefuseIfAccountingPeriodClosedAsync(_accountingRepository, CurrentOrganizationId, dto.OfficeId, dto.AccountingPeriod, "create the invoice");
            if (periodCheck != null)
                return periodCheck;

            try
            {
                var invoice = dto.ToModel(CurrentUser);
                invoice.OrganizationId = CurrentOrganizationId;
                await _accountingManager.EnrichInvoiceBeforeSaveAsync(invoice);
                var createdInvoice = await _accountingRepository.CreateAsync(invoice);

                await _accountingManager.CreateJournalEntryFromInvoiceAsync(createdInvoice, CurrentUser);

                var response = new InvoiceResponseDto(createdInvoice);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return ServerError("An error occurred while creating the invoice");
            }
        }

        [HttpPost("invoice/ledger-line/reservation")]
        public async Task<IActionResult> CreateLedgerLinesByReservationId([FromBody] CreateInvoiceMonthlyDataDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice data is required");

            try
            {
                var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId, CurrentOrganizationId);
                if (reservation == null)
                    return NotFound("Reservation not found");

                var ledgerLines = await _accountingManager.CreateLedgerLinesForReservationIdAsync(reservation, dto.InvoiceDate, dto.StartDate, dto.EndDate);
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

        [HttpPost("invoice/ledger-line/organization")]
        public async Task<IActionResult> CreateLedgerLinesByOrganizationId([FromBody] CreateBillingMonthlyDataDto dto)
        {
            if (dto == null)
                return BadRequest("Invoice data is required");

            try
            {
                var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
                if (organization == null)
                    return NotFound("Organization not found");

                var ledgerLines = await _accountingManager.CreateLedgerLinesForOrganizationIdAsync(organization, dto.StartDate, dto.EndDate);
                var data = new BillingMonthlyData { InvoiceCode = dto.InvoiceCode, OrganizationId = dto.OrganizationId, LedgerLines = ledgerLines };
                var response = new BillingMonthlyDataResponseDto(data);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ledger lines: {OrganizationId}", dto.OrganizationId);
                return ServerError("An error occurred while retrieving the ledger line");
            }
        }

        #endregion

        #region Put

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
                var existingInvoice = await _accountingRepository.GetInvoiceByIdAsync(dto.InvoiceId, CurrentOrganizationId);
                if (existingInvoice == null)
                    return NotFound("Invoice not found");

                var hardClosedResult = RefuseIfJournalEntryHardClosed(existingInvoice.PostingStatusId, "invoice");
                if (hardClosedResult != null)
                    return hardClosedResult;

                var invoice = dto.ToModel(CurrentUser);
                var updatedInvoice = await _accountingManager.UpdateInvoiceAsync(invoice);

                var response = new InvoiceResponseDto(updatedInvoice);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice: {InvoiceId}", dto.InvoiceId);
                return ServerError("An error occurred while updating the invoice");
            }
        }

        [HttpPut("invoice/reservation/{reservationId}/deactivate")]
        public async Task<IActionResult> DeactivateInvoicesByReservationId(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                var deactivatedCount = await _accountingRepository.DeactivateInvoicesByReservationIdAsync(
                    CurrentOrganizationId, reservationId, CurrentUser);
                return Ok(new { deactivatedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating invoices for reservation: {ReservationId}", reservationId);
                return ServerError("An error occurred while deactivating invoices for the reservation");
            }
        }

        [HttpPut("invoice/reservation/{reservationId}/reactivate")]
        public async Task<IActionResult> ReactivateInvoicesByReservationId(Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                return BadRequest("Reservation ID is required");

            try
            {
                var reactivatedCount = await _accountingRepository.ReactivateInvoicesByReservationIdAsync(
                    CurrentOrganizationId, reservationId, CurrentUser);
                return Ok(new { reactivatedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating invoices for reservation: {ReservationId}", reservationId);
                return ServerError("An error occurred while reactivating invoices for the reservation");
            }
        }

        [HttpPut("invoice/payment")]
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
                    dto.CostCodeId, dto.Description, dto.Amount, dto.PaymentDate, CurrentUser);

                await _accountingManager.CreateJournalEntriesFromInvoicePaymentAsync(invoicePayment, CurrentUser);

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

        [HttpDelete("invoice/{invoiceId}")]
        public async Task<IActionResult> DeleteInvoiceByIdAsync(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty)
                return BadRequest("Invoice ID is required");

            try
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, CurrentOrganizationId);
                if (invoice == null)
                    return NotFound("Invoice not found");

                if (invoice.PaidAmount != 0)
                    return BadRequest("Invoices with payments applied may not be deleted.");

                await _accountingManager.DeleteJournalEntriesForInvoiceAsync(invoice);
                await _accountingRepository.DeleteInvoiceByIdAsync(invoiceId, CurrentOrganizationId);
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
