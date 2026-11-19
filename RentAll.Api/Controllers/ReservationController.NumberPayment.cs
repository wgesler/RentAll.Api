using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
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
    public async Task<IActionResult> ReceiveNumberPayment()
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

        if (!Guid.TryParse(post.RPGUID, out var reservationId))
            return BadRequest("RPGUID is not a reservation id");

        int? officeId = null;
        try
        {
            // RPGUID was set to ReservationId on InitForm; that is how we find org and office.
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
            {
                _logger.LogError("[NumberPayment] Step=ReservationNotFound TxID={TxId} RPGUID={RpGuid}", post.TransactionID, post.RPGUID);
                return NotFound("Reservation not found");
            }

            officeId = reservation.OfficeId;
            var organization = await _organizationRepository.GetOrganizationByIdAsync(reservation.OrganizationId);
            if (organization == null)
                return NotFound("Organization not found");

            var transaction = await WithNumberSessionAsync(organization, reservation.OfficeId,
                session => _numberService.GetTransactionAsync(session, post.TransactionID, HttpContext.RequestAborted), HttpContext.RequestAborted);
            if (transaction == null)
            {
                _logger.LogError("[NumberPayment] Step=NotVerified TxID={TxId} Reason=NotFoundAtNumber", post.TransactionID);
                return BadRequest("Transaction could not be verified");
            }

            // Log every result field Number gives us: its docs don't define the status, flag or
            // partial-approval values, so these logs are how we learn what each test case returns.
            _logger.LogInformation("[NumberPayment] Step=Detail TxID={TxId} Status={Status} TxType={TxType} Flags={Flags} Amount={Amount} IsPartialApproval={IsPartialApproval} PartialAuthApproved={PartialAuthApproved} HAuthorizedAmount={HAuthorizedAmount} AVS={Avs} CVV={Cvv} TxnCode={TxnCode} ConsentID={ConsentId} CreatedOn={CreatedOn} PostedAmount={PostedAmount}",
                transaction.Id, transaction.Status, transaction.TxType, transaction.Flags, transaction.Amount, transaction.IsPartialApproval, transaction.PartialAuthApproved, transaction.HAuthorizedAmount,
                transaction.AvsResult, transaction.CvvResult, transaction.TxnCode, transaction.ConsentId, transaction.CreatedOn, post.Amount);

            var guidMatches = Guid.TryParse(transaction.RpGuid, out var txGuid) && txGuid == reservationId;
            var approvedAmount = ResolveApprovedAmount(transaction);
            // On a partial approval the post may carry either the requested or the approved amount.
            var amountMatches = post.Amount == transaction.Amount || (approvedAmount.HasValue && post.Amount == approvedAmount.Value);
            if (!guidMatches || !amountMatches)
            {
                _logger.LogError("[NumberPayment] Step=NotVerified TxID={TxId} GuidMatches={GuidMatches} AmountMatches={AmountMatches} NumberAmount={NumberAmount} ApprovedAmount={ApprovedAmount} PostedAmount={PostedAmount}",
                    post.TransactionID, guidMatches, amountMatches, transaction.Amount, approvedAmount, post.Amount);
                return BadRequest("Transaction does not match the payment post");
            }

            // A decline is a real answer from Number, not an error: return 200 so it isn't posted again.
            if (!IsApprovedStatus(transaction.Status))
            {
                _logger.LogError("[NumberPayment] Step=NotApproved TxID={TxId} ReservationId={ReservationId} Status={Status} Flags={Flags} Amount={Amount}",
                    transaction.Id, reservationId, transaction.Status, transaction.Flags, transaction.Amount);
                return Ok();
            }

            if (approvedAmount is not > 0)
            {
                _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} ReservationId={ReservationId} Reason=ApprovedAmountUnclear Amount={Amount} IsPartialApproval={IsPartialApproval} PartialAuthApproved={PartialAuthApproved} HAuthorizedAmount={HAuthorizedAmount}",
                    transaction.Id, reservationId, transaction.Amount, transaction.IsPartialApproval, transaction.PartialAuthApproved, transaction.HAuthorizedAmount);
                return Ok();
            }

            if (transaction.IsPartialApproval)
                _logger.LogError("[NumberPayment] Step=PartialApproval TxID={TxId} ReservationId={ReservationId} Requested={Requested} Approved={Approved} Remaining={Remaining}",
                    transaction.Id, reservationId, transaction.Amount, approvedAmount, transaction.Amount - approvedAmount.Value);

            _logger.LogInformation("[NumberPayment] Step=Verified TxID={TxId} ReservationId={ReservationId} Amount={Amount} ConsentID={ConsentId}",
                transaction.Id, reservationId, approvedAmount, transaction.ConsentId);

            await SaveNumberConsentAsync(reservation, transaction, post);
            await SaveNumberCardOnFileAsync(reservation, transaction, post);

            var payment = await RecordNumberPaymentAsync(reservation, transaction, post, approvedAmount.Value);
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
    private async Task<Payment?> RecordNumberPaymentAsync(Reservation reservation, NumberTransaction transaction, NumberPostDto post, decimal approvedAmount)
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
            _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} ReservationId={ReservationId} Amount={Amount} Reason=NoUnpaidInvoices", numberTxId, reservation.ReservationId, approvedAmount);
            return null;
        }

        var last4 = string.IsNullOrWhiteSpace(post.CardNumber) ? string.Empty : $" {post.CardType} ending {post.CardNumber}".TrimEnd();
        var partialNote = transaction.IsPartialApproval ? $", partial approval of {transaction.Amount:0.00} requested" : string.Empty;
        var payment = new Payment
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            PaymentDate = DateOnly.FromDateTime(DateTime.Today),
            Amount = approvedAmount,
            CostCodeId = costCode.CostCodeId,
            Description = $"Number card payment{last4} (TxID {numberTxId}{partialNote})",
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)PaymentType.CreditCard,
            NumberTxId = numberTxId,
            NumberTxnCode = transaction.TxnCode ?? post.ApprovalCode,
            IsActive = true,
            CreatedBy = SystemUserId
        };

        return await _accountingManager.ApplyInvoicePaymentAsync(payment, invoiceIds, null, officeAccess, SystemUserId);
    }

    // Number's docs only show FAILED for a decline; the other words cover voids and reversals.
    // Statuses not listed here (OPEN, SETTLED and so on) are treated as approved.
    private static readonly string[] NotApprovedStatusWords = ["FAIL", "DECLIN", "VOID", "REVERS", "ERROR", "REJECT", "CANCEL"];

    private static bool IsApprovedStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true;
        return !NotApprovedStatusWords.Any(word => status.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The amount the card was actually approved for. On a full approval that's AMOUNT. On a partial approval
    /// Number's docs don't say which field holds it, so HAuthorizedAmount is used when it's a positive amount
    /// below AMOUNT; otherwise null, and the payment is left for someone to record by hand.
    /// </summary>
    private static decimal? ResolveApprovedAmount(NumberTransaction transaction)
    {
        if (!transaction.IsPartialApproval)
            return transaction.Amount;

        if (transaction.HAuthorizedAmount is > 0 and var authorized && authorized < transaction.Amount)
            return authorized;

        return null;
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

    /// <summary>
    /// Saves the card Number kept on file as the card on file of the reservation's first contact (the payer
    /// on the payment form). Only Number's token, last four, type, name and expiration are stored.
    /// </summary>
    private async Task SaveNumberCardOnFileAsync(Reservation reservation, NumberTransaction transaction, NumberPostDto post)
    {
        var consentId = transaction.ConsentId > 0 ? transaction.ConsentId : post.ConsentID;
        if (consentId <= 0 || consentId > int.MaxValue)
            return;

        try
        {
            var contactId = reservation.ContactIds.FirstOrDefault();
            var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
            if (contact == null || contact.OrganizationId != reservation.OrganizationId)
            {
                _logger.LogError("[NumberPayment] Step=CardNotSaved TxID={TxId} ReservationId={ReservationId} Reason=NoContact", transaction.Id, reservation.ReservationId);
                return;
            }

            var cardTypeId = MapNumberCardType(post.CardType);
            if (cardTypeId == null)
                _logger.LogWarning("[NumberPayment] Step=CardTypeUnknown TxID={TxId} CardType={CardType}; saved as Visa", transaction.Id, post.CardType);

            var name = string.Join(' ', new[] { post.CardholderFirstName, post.CardholderLastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = string.Join(' ', new[] { post.CustomerFirstName, post.CustomerLastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();

            var saved = await _contactCardStore.SaveNumberCardAsync(contact, new ContactCard
            {
                CardTypeId = cardTypeId ?? (int)CardType.Visa,
                CardName = name,
                LastFour = ContactCardStore.ExtractLastFour(post.CardNumber),
                ExpirationDate = NormalizeExpiration(post.ExpireDate),
                NumberConsentId = (int)consentId
            }, SystemUserId);

            _logger.LogInformation("[NumberPayment] Step=CardSaved TxID={TxId} ContactId={ContactId} ContactCardId={ContactCardId} ConsentID={ConsentId}",
                transaction.Id, contact.ContactId, saved.ContactCardId, consentId);
        }
        catch (Exception ex)
        {
            // The payment still gets recorded; the card can be saved again on the next payment.
            _logger.LogError(ex, "[NumberPayment] Step=CardNotSaved TxID={TxId} ReservationId={ReservationId}", transaction.Id, reservation.ReservationId);
        }
    }

    private static int? MapNumberCardType(string? cardType)
    {
        var value = new string((cardType ?? string.Empty).Where(char.IsLetter).ToArray()).ToUpperInvariant();
        return value switch
        {
            "VISA" => (int)CardType.Visa,
            "MC" or "MASTERCARD" or "MASTER" => (int)CardType.MasterCard,
            "DISC" or "DISCOVER" => (int)CardType.Discover,
            "AMEX" or "AMERICANEXPRESS" or "AX" => (int)CardType.AmericanExpress,
            _ => null
        };
    }

    // Number posts "12/28"; its transaction detail shows "1228". Store MM/YY.
    private static string? NormalizeExpiration(string? expireDate)
    {
        var digits = new string((expireDate ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 6)
            digits = digits[..2] + digits[4..];
        return digits.Length == 4 ? $"{digits[..2]}/{digits[2..]}" : null;
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
