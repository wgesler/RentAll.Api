using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Models.Accounting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentAll.Api.Controllers;

public partial class ReservationController
{
    private static readonly JsonSerializerOptions NumberPostJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// Number's PayForm PostURL. Number posts the payment result here after the payer submits the form.
    /// The post carries no credentials, so the transaction is confirmed with Number's
    /// Transaction_FullDetail query before anything is trusted.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("external/number-payment")]
    public async Task<IActionResult> ReceiveNumberPayment([FromQuery] Guid organizationId, [FromQuery] int officeId)
    {
        string raw;
        using (var reader = new StreamReader(Request.Body))
            raw = await reader.ReadToEndAsync(HttpContext.RequestAborted);

        NumberPostDto? post = null;
        try
        {
            post = JsonSerializer.Deserialize<NumberPostDto>(raw, NumberPostJsonOptions);
        }
        catch (JsonException)
        {
            // Not JSON: the form may be set to the encrypted or query-string post instead.
        }

        if (post == null || post.TransactionID <= 0)
        {
            _logger.LogError("[NumberPayment] Step=Unreadable ContentType={ContentType} Length={Length} Query={Query}",
                Request.ContentType, raw.Length, Request.QueryString.Value);
            return BadRequest("Unreadable Number payment post");
        }

        _logger.LogInformation("[NumberPayment] Step=Received TxID={TxId} ConsentID={ConsentId} RPGUID={RpGuid} REFID={RefId} Amount={Amount} Approval={Approval}",
            post.TransactionID, post.ConsentID, post.RPGUID, post.REFID, post.Amount, post.ApprovalCode);

        if (organizationId == Guid.Empty || officeId <= 0)
            return BadRequest("organizationId and officeId are required");
        if (!Guid.TryParse(post.RPGUID, out var reservationId))
            return BadRequest("RPGUID is not a reservation id");

        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId);
            if (reservation == null || reservation.OfficeId != officeId)
            {
                _logger.LogError("[NumberPayment] Step=ReservationNotFound TxID={TxId} RPGUID={RpGuid}", post.TransactionID, post.RPGUID);
                return NotFound("Reservation not found");
            }

            var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
                return NotFound("Organization not found");

            var session = await GetStoredNumberSessionAsync(organization, officeId, HttpContext.RequestAborted);
            var transaction = await _numberService.GetTransactionAsync(session, post.TransactionID, HttpContext.RequestAborted);
            if (transaction == null)
            {
                _logger.LogError("[NumberPayment] Step=NotVerified TxID={TxId} Reason=NotFoundAtNumber", post.TransactionID);
                return BadRequest("Transaction could not be verified");
            }

            var guidMatches = Guid.TryParse(transaction.RpGuid, out var txGuid) && txGuid == reservationId;
            var amountMatches = transaction.Amount == post.Amount;
            if (!guidMatches || !amountMatches)
            {
                _logger.LogError("[NumberPayment] Step=NotVerified TxID={TxId} GuidMatches={GuidMatches} AmountMatches={AmountMatches} NumberAmount={NumberAmount} PostedAmount={PostedAmount}",
                    post.TransactionID, guidMatches, amountMatches, transaction.Amount, post.Amount);
                return BadRequest("Transaction does not match the payment post");
            }

            _logger.LogInformation("[NumberPayment] Step=Verified TxID={TxId} ReservationId={ReservationId} Amount={Amount} ConsentID={ConsentId} Status={Status} TxType={TxType} CreatedOn={CreatedOn}",
                transaction.Id, reservationId, transaction.Amount, transaction.ConsentId, transaction.Status, transaction.TxType, transaction.CreatedOn);

            await SaveNumberConsentAsync(reservation, transaction, post);

            var payment = await RecordNumberPaymentAsync(reservation, transaction, post);
            if (payment != null)
                _logger.LogInformation("[NumberPayment] Step=Recorded TxID={TxId} PaymentId={PaymentId} PaymentCode={PaymentCode}", transaction.Id, payment.PaymentId, payment.PaymentCode);

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "ReceiveNumberPayment", officeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "ReceiveNumberPayment", officeId);
            return ServerError("An error occurred while receiving the Number payment");
        }
    }

    /// <summary>
    /// Applies a verified Number card payment to the reservation's unpaid invoices, oldest first,
    /// the same way the invoice list's Apply Payment does. Returns null when it was already recorded
    /// or can't be applied; either way Number gets a 200 so it doesn't post again.
    /// </summary>
    private async Task<Payment?> RecordNumberPaymentAsync(Reservation reservation, NumberTransaction transaction, NumberPostDto post)
    {
        if (transaction.Id > int.MaxValue)
        {
            _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} Reason=TxIdTooLarge", transaction.Id);
            return null;
        }
        var numberTxId = (int)transaction.Id;
        var officeAccess = reservation.OfficeId.ToString();

        // Number can post the same result more than once.
        var existing = await _accountingRepository.GetPaymentsByOfficeIdsAsync(reservation.OrganizationId, officeAccess, (int)PaymentKind.Invoice);
        var duplicate = existing.FirstOrDefault(p => p.NumberTxId == numberTxId);
        if (duplicate != null)
        {
            _logger.LogInformation("[NumberPayment] Step=AlreadyRecorded TxID={TxId} PaymentId={PaymentId}", numberTxId, duplicate.PaymentId);
            return null;
        }

        var costCode = await ResolveCreditCardPaymentCostCodeAsync(reservation.OrganizationId, reservation.OfficeId);
        if (costCode == null)
        {
            _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} ReservationId={ReservationId} Reason=NoCreditCardPaymentCostCode", numberTxId, reservation.ReservationId);
            return null;
        }

        var invoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = reservation.OrganizationId,
            OfficeIds = officeAccess,
            ReservationId = reservation.ReservationId,
            IsActive = true,
            IncludeInactive = false,
            IncludePaid = false
        });
        var invoiceIds = invoices.Select(invoice => invoice.InvoiceId).ToList();
        if (invoiceIds.Count == 0)
        {
            _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} ReservationId={ReservationId} Amount={Amount} Reason=NoUnpaidInvoices", numberTxId, reservation.ReservationId, transaction.Amount);
            return null;
        }

        var last4 = string.IsNullOrWhiteSpace(post.CardNumber) ? string.Empty : $" {post.CardType} ending {post.CardNumber}".TrimEnd();
        var payment = new Payment
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            PaymentDate = DateOnly.FromDateTime(DateTime.Today),
            Amount = transaction.Amount,
            CostCodeId = costCode.CostCodeId,
            Description = $"Number card payment{last4} (TxID {numberTxId})",
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)PaymentType.CreditCard,
            NumberTxId = numberTxId,
            NumberTxnCode = transaction.TxnCode ?? post.ApprovalCode,
            IsActive = true,
            CreatedBy = SystemUserId
        };

        return await _accountingManager.ApplyInvoicePaymentAsync(payment, invoiceIds, null, officeAccess, SystemUserId);
    }

    // A ConsentID means Number kept the card on file; store it so later charges can use it.
    private async Task SaveNumberConsentAsync(Reservation reservation, NumberTransaction transaction, NumberPostDto post)
    {
        var consentId = transaction.ConsentId > 0 ? transaction.ConsentId : post.ConsentID;
        if (consentId <= 0)
            return;
        if (consentId > int.MaxValue)
        {
            _logger.LogError("[NumberPayment] Step=ConsentNotSaved TxID={TxId} ConsentID={ConsentId} Reason=ConsentIdTooLarge", transaction.Id, consentId);
            return;
        }
        if (reservation.NumberConsentId == (int)consentId)
            return;

        try
        {
            reservation.NumberConsentId = (int)consentId;
            reservation.ModifiedBy = SystemUserId;
            await _reservationRepository.UpdateByIdAsync(reservation);
            _logger.LogInformation("[NumberPayment] Step=ConsentSaved TxID={TxId} ReservationId={ReservationId} ConsentID={ConsentId}", transaction.Id, reservation.ReservationId, consentId);
        }
        catch (Exception ex)
        {
            // The payment still gets recorded; the card on file can be set again later.
            _logger.LogError(ex, "[NumberPayment] Step=ConsentNotSaved TxID={TxId} ReservationId={ReservationId} ConsentID={ConsentId}", transaction.Id, reservation.ReservationId, consentId);
        }
    }

    // The invoice list offers the office's active Payment cost codes; this picks the credit card one.
    private async Task<CostCode?> ResolveCreditCardPaymentCostCodeAsync(Guid organizationId, int officeId)
    {
        var paymentCodes = (await _accountingRepository.GetCostCodesByOfficeIdAsync(organizationId, officeId))
            .Where(code => code.IsActive && code.TransactionType == TransactionType.Payment)
            .Where(code => !code.Description.Contains("Refund", StringComparison.OrdinalIgnoreCase))
            .OrderBy(code => code.CostCodeId)
            .ToList();

        return paymentCodes.FirstOrDefault(code => code.Description.Contains("Credit Card", StringComparison.OrdinalIgnoreCase))
            ?? paymentCodes.FirstOrDefault(code => code.Description.Split(' ', '-', '/').Any(word => word.Equals("CC", StringComparison.OrdinalIgnoreCase)));
    }

    // Field names as Number's PayForm JSON POST sends them.
    private sealed class NumberPostDto
    {
        public long ConsentID { get; set; }
        public long TransactionID { get; set; }
        public string? CardNumber { get; set; }
        public string? CardType { get; set; }
        public string? ExpireDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Surcharge { get; set; }
        public string? CardholderFirstName { get; set; }
        public string? CardholderLastName { get; set; }
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string? Email { get; set; }
        public string? REFID { get; set; }
        public string? RPGUID { get; set; }
        public string? ApprovalCode { get; set; }
    }
}
