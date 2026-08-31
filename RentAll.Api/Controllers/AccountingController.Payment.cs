using RentAll.Api.Dtos.Accounting.Payments;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpGet("payment/invoice")]
    public async Task<IActionResult> GetAllInvoicePayments()
    {
        try
        {
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, (int)PaymentKind.Invoice);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return ServerError("An error occurred while retrieving payments");
        }
    }

    [HttpGet("payment/invoice/office/{officeId:int}")]
    public async Task<IActionResult> GetInvoicePaymentsByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, officeAccess, (int)PaymentKind.Invoice);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return ServerError("An error occurred while retrieving payments");
        }
    }

    [HttpGet("payment/bill")]
    public async Task<IActionResult> GetAllBillPayments()
    {
        try
        {
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, (int)PaymentKind.Bill);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bill payments");
            return ServerError("An error occurred while retrieving bill payments");
        }
    }

    [HttpGet("payment/bill/office/{officeId:int}")]
    public async Task<IActionResult> GetBillPaymentsByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, officeAccess, (int)PaymentKind.Bill);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bill payments");
            return ServerError("An error occurred while retrieving bill payments");
        }
    }

    [HttpGet("payment/owner")]
    public async Task<IActionResult> GetAllOwnerPayments()
    {
        try
        {
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, (int)PaymentKind.Owner);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner payments");
            return ServerError("An error occurred while retrieving owner payments");
        }
    }

    [HttpGet("payment/owner/office/{officeId:int}")]
    public async Task<IActionResult> GetOwnerPaymentsByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _accountingRepository.GetPaymentsByOfficeIdsAsync(CurrentOrganizationId, officeAccess, (int)PaymentKind.Owner);
            var response = records.Select(o => new PaymentResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner payments");
            return ServerError("An error occurred while retrieving owner payments");
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

    [HttpPost("payment/invoice-allocations")]
    [HttpPost("payment/allocations")]
    public async Task<IActionResult> CreatePaymentWithInvoiceAllocations([FromBody] CreatePaymentWithInvoiceAllocationsDto dto)
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
            var created = await _accountingManager.CreatePaymentWithInvoiceAllocationsAsync(
                payment,
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

    [HttpPost("payment/bill-allocations")]
    public async Task<IActionResult> CreatePaymentWithBillAllocations([FromBody] CreatePaymentWithBillAllocationsDto dto)
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
            foreach (var allocation in dto.Allocations)
            {
                var bill = await _maintenanceRepository.GetReceiptByIdAsync(allocation.ReceiptId, CurrentOrganizationId);
                if (bill == null)
                    return NotFound($"Bill not found: {allocation.ReceiptId}");

                postingStatuses.Add(bill.PostingStatusId);
            }

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(StrictestPostingStatus(postingStatuses), "bill");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            var allocations = dto.Allocations.Select(allocation => allocation.ToModel()).ToList();
            var created = await _accountingManager.CreatePaymentWithBillAllocationsAsync(
                payment,
                allocations,
                CurrentUser);
            var response = new PaymentResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bill payment with bill allocations");
            return ServerError("An error occurred while creating the payment");
        }
    }

    [HttpPut("payment/bill-allocations")]
    public async Task<IActionResult> UpdatePaymentWithBillAllocations([FromBody] UpdatePaymentWithBillAllocationsDto dto)
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

            if (existing.PaymentKindId != (int)PaymentKind.Bill)
                return BadRequest("Invoice payments must be updated through payment/invoice-allocations.");

            var postingStatuses = new List<int?> { existing.PostingStatusId };
            foreach (var allocation in dto.Allocations)
            {
                var bill = await _maintenanceRepository.GetReceiptByIdAsync(allocation.ReceiptId, CurrentOrganizationId);
                if (bill == null)
                    return NotFound($"Bill not found: {allocation.ReceiptId}");

                postingStatuses.Add(bill.PostingStatusId);
            }

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(StrictestPostingStatus(postingStatuses), "payment");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            var allocations = dto.Allocations.Select(allocation => allocation.ToModel()).ToList();
            var updated = await _accountingManager.UpdatePaymentWithBillAllocationsAsync(
                payment,
                allocations,
                CurrentUser);
            var response = new PaymentResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bill payment with bill allocations: {PaymentId}", dto.PaymentId);
            return ServerError("An error occurred while updating the payment");
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

    [HttpPut("payment/invoice-allocations")]
    [HttpPut("payment/allocations")]
    public async Task<IActionResult> UpdatePaymentWithInvoiceAllocations([FromBody] UpdatePaymentWithInvoiceAllocationsDto dto)
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

            if (existing.PaymentKindId != (int)PaymentKind.Invoice)
                return BadRequest("Bill payments must be updated through payment/bill-allocations.");

            var postingStatuses = new List<int?> { existing.PostingStatusId };
            foreach (var allocation in dto.Allocations)
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(allocation.InvoiceId, CurrentOrganizationId);
                if (invoice == null)
                    return NotFound($"Invoice not found: {allocation.InvoiceId}");

                postingStatuses.Add(invoice.PostingStatusId);
            }

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(StrictestPostingStatus(postingStatuses), "payment");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            payment.PostingStatusId = existing.PostingStatusId;
            if (payment.DepositId is null || payment.DepositId == Guid.Empty)
                payment.DepositId = existing.DepositId;

            var allocations = dto.Allocations.Select(allocation => allocation.ToModel()).ToList();
            var updated = await _accountingManager.UpdatePaymentWithInvoiceAllocationsAsync(
                payment,
                allocations,
                CurrentOfficeAccess,
                CurrentUser);
            var response = new PaymentResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment with allocations: {PaymentId}", dto.PaymentId);
            return ServerError("An error occurred while updating the payment");
        }
    }

    [HttpPut("payment/invoice")]
    public async Task<IActionResult> UpdatePaymentInvoice([FromBody] UpdatePaymentInvoiceDto dto)
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

            if (existing.PaymentKindId != (int)PaymentKind.Invoice)
                return BadRequest("Bill payments must be updated through payment/bill.");

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(existing.PostingStatusId, "payment");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            payment.PostingStatusId = existing.PostingStatusId;
            if (payment.DepositId is null || payment.DepositId == Guid.Empty)
                payment.DepositId = existing.DepositId;

            var updated = await _accountingManager.UpdatePaymentInvoiceAsync(payment, CurrentUser);
            var response = new PaymentResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice payment header: {PaymentId}", dto.PaymentId);
            return ServerError("An error occurred while updating the payment");
        }
    }

    [HttpPut("payment/bill")]
    public async Task<IActionResult> UpdatePaymentBill([FromBody] UpdatePaymentBillDto dto)
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

            if (existing.PaymentKindId != (int)PaymentKind.Bill)
                return BadRequest("Invoice payments must be updated through payment/invoice.");

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(existing.PostingStatusId, "payment");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var payment = dto.ToModel(CurrentUser);
            payment.PostingStatusId = existing.PostingStatusId;
            payment.CostCodeId = existing.CostCodeId;

            var updated = await _accountingManager.UpdatePaymentBillAsync(payment, CurrentUser);
            var response = new PaymentResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bill payment header: {PaymentId}", dto.PaymentId);
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
            if (payment.PaymentKindId == (int)PaymentKind.Invoice)
            {
                var paymentLedgerLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(paymentId, CurrentOrganizationId);
                foreach (var invoiceId in paymentLedgerLines.Select(line => line.InvoiceId).Distinct())
                {
                    var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, CurrentOrganizationId);
                    if (invoice != null)
                        postingStatuses.Add(invoice.PostingStatusId);
                }
            }
            else if (payment.PaymentKindId == (int)PaymentKind.Bill)
            {
                var billAllocations = await _accountingRepository.GetBillAllocationsByPaymentIdAsync(paymentId, CurrentOrganizationId);
                foreach (var allocation in billAllocations)
                {
                    var bill = await _maintenanceRepository.GetReceiptByIdAsync(allocation.ReceiptId, CurrentOrganizationId);
                    if (bill != null)
                        postingStatuses.Add(bill.PostingStatusId);
                }
            }
            else if (payment.PaymentKindId != (int)PaymentKind.Owner)
            {
                return BadRequest($"Unsupported payment kind: {payment.PaymentKindId}");
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
            return BadRequest(ex.Message);
        }
    }

    #endregion
}
