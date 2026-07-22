using RentAll.Api.Dtos.Accounting.Payments;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpGet("payment")]
    public async Task<IActionResult> GetAllPayments()
    {
        try
        {
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return ServerError("An error occurred while retrieving payments");
        }
    }

    [HttpGet("payment/office/{officeId:int}")]
    public async Task<IActionResult> GetPaymentsByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, officeAccess);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return ServerError("An error occurred while retrieving payments");
        }
    }

    [HttpGet("payment/{paymentId:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        if (paymentId == Guid.Empty)
            return BadRequest("PaymentId is required");

        try
        {
            var record = await _accountingRepository.GetPaymentByIdAsync(paymentId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Payment record not found");

            var response = new PaymentResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by ID: {PaymentId}", paymentId);
            return ServerError("An error occurred while retrieving the payment");
        }
    }

    #endregion

    #region Post

    [HttpPost("payment")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (dto == null)
            return BadRequest("Payment data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var payment = dto.ToModel(CurrentUser);
            var created = await _accountingRepository.CreatePaymentAsync(payment);
            var response = new PaymentResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return ServerError("An error occurred while creating the payment");
        }
    }

    [HttpPost("payment/allocations")]
    public async Task<IActionResult> CreatePaymentWithAllocations([FromBody] CreatePaymentWithAllocationsDto dto)
    {
        if (dto == null)
            return BadRequest("Payment data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var payment = dto.ToModel(CurrentUser);
            var allocations = dto.Allocations.Select(allocation => allocation.ToModel()).ToList();
            var created = await _accountingManager.ApplyInvoicePaymentAsync(
                payment,
                null,
                allocations,
                CurrentOfficeAccess,
                CurrentUser);
            var response = new PaymentResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment with allocations");
            return ServerError("An error occurred while creating the payment");
        }
    }

    [HttpPost("payment/apply-invoices")]
    public async Task<IActionResult> ApplyInvoicePayment([FromBody] ApplyInvoicePaymentDto dto)
    {
        if (dto == null)
            return BadRequest("Payment data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var postingStatuses = new List<int?>();
            foreach (var invoiceId in dto.ResolveInvoiceIdsForPostingCheck())
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, CurrentOrganizationId);
                if (invoice == null)
                    return NotFound($"Invoice not found: {invoiceId}");

                postingStatuses.Add(invoice.PostingStatusId);
            }

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(StrictestPostingStatus(postingStatuses), "invoice");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            var created = await _accountingManager.ApplyInvoicePaymentAsync(
                payment,
                dto.UsesExplicitAllocations ? null : dto.Invoices,
                dto.UsesExplicitAllocations
                    ? dto.Allocations.Select(allocation => allocation.ToModel()).ToList()
                    : null,
                CurrentOfficeAccess,
                CurrentUser);
            var response = new PaymentResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying payment to invoices");
            return ServerError("An error occurred while applying payment to invoices");
        }
    }

    #endregion

    #region Put

    [HttpPut("payment")]
    public async Task<IActionResult> UpdatePayment([FromBody] UpdatePaymentDto dto)
    {
        if (dto == null)
            return BadRequest("Payment data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _accountingRepository.GetPaymentByIdAsync(dto.PaymentId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Payment record not found");

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(existing.PostingStatusId, "payment");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            payment.PostingStatusId = existing.PostingStatusId;
            if (payment.DepositId is null || payment.DepositId == Guid.Empty)
                payment.DepositId = existing.DepositId;
            var updated = await _accountingRepository.UpdatePaymentAsync(payment);
            var response = new PaymentResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment: {PaymentId}", dto.PaymentId);
            return ServerError("An error occurred while updating the payment");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("payment/{paymentId:guid}")]
    public async Task<IActionResult> DeletePaymentById(Guid paymentId)
    {
        if (paymentId == Guid.Empty)
            return BadRequest("PaymentId is required");

        try
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, CurrentOrganizationId);
            if (payment == null)
                return NotFound("Payment record not found");

            var postingStatuses = new List<int?> { payment.PostingStatusId };
            foreach (var invoiceId in payment.LedgerLines.Select(line => line.InvoiceId).Distinct())
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, CurrentOrganizationId);
                if (invoice != null)
                    postingStatuses.Add(invoice.PostingStatusId);
            }

            var postingStatusCheck = RefuseIfDocumentDeleteNotAllowed(StrictestPostingStatus(postingStatuses), "payment");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            await _accountingManager.DeletePaymentAsync(paymentId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment: {PaymentId}", paymentId);
            return ServerError("An error occurred while deleting the payment");
        }
    }

    #endregion
}
