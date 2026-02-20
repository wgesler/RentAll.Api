using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Accounting.Invoices;

namespace RentAll.Api.Controllers
{
    public partial class AccountingController
    {
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
        [HttpPut("payment")]
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
    }
}
