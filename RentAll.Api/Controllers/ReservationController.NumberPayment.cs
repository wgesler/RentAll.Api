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
    /// Number's PayForm PostURL. Number posts here after the payer submits the form: a save-card form
    /// sends a ConsentID with no transaction; a payment form sends a TransactionID (and a ConsentID if it
    /// also saved the card). The post carries no credentials, so each ID is confirmed with Number first.
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
            // Not JSON: may be the PFS pipe format (TXID|5|CARDNO|4864|...|CONSENTID|1).
            post = NumberPostDto.FromPipePost(raw);
        }
        post?.ApplyWebhookShape();

        _logger.LogError("[NumberPayment] Step=Raw RemoteIp={RemoteIp} ContentType={ContentType} Body={Body}", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.ContentType, raw.Length > 4000 ? raw[..4000] : raw);

        if (post == null)
        {
            _logger.LogError("[NumberPayment] Step=Unreadable ContentType={ContentType} Length={Length} Query={Query}",
                Request.ContentType, raw.Length, Request.QueryString.Value);
            return BadRequest("Unreadable Number payment post");
        }

        var (isValid, errorMessage) = post.IsValid();
        if (!isValid)
        {
            if (!post.HasIds && !string.IsNullOrWhiteSpace(post.Type))
            {
                // A webhook we have nothing to do with (no transaction or saved card): acknowledge it so Number stops retrying.
                _logger.LogInformation("[NumberPayment] Step=IgnoredWebhook Type={Type} RPGUID={RpGuid}", post.Type, post.RPGUID);
                return Ok();
            }
            _logger.LogError("[NumberPayment] Step=Invalid Reason={Reason} TxID={TxId} ConsentID={ConsentId} RPGUID={RpGuid}", errorMessage, post.TransactionID, post.ConsentID, post.RPGUID);
            return BadRequest(errorMessage ?? "Invalid request data");
        }

        _logger.LogInformation("[NumberPayment] Step=Received TxID={TxId} ConsentID={ConsentId} RPGUID={RpGuid} REFID={RefId} Amount={Amount} Approval={Approval}",
            post.TransactionID, post.ConsentID, post.RPGUID, post.REFID, post.Amount, post.ApprovalCode);
        var reservationId = post.ReservationId;

        int? officeId = null;
        try
        {
            // RPGUID was set to ReservationId on InitForm; that is how we find org and office.
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
            {
                _logger.LogError("[NumberPayment] Step=ReservationNotFound TxID={TxId} ConsentID={ConsentId} RPGUID={RpGuid}", post.TransactionID, post.ConsentID, post.RPGUID);
                return NotFound("Reservation not found");
            }

            officeId = reservation.OfficeId;
            var organization = await _organizationRepository.GetOrganizationByIdAsync(reservation.OrganizationId);
            if (organization == null)
                return NotFound("Organization not found");

            return post.TransactionID > 0
                ? await ReceiveNumberTransactionAsync(organization, reservation, post)
                : await ReceiveNumberConsentAsync(organization, reservation, post);
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
    /// Handles what EasyPay returned to a pay-response page (?TXID=..&amp;CID=..): confirms the IDs with EasyPay, saves
    /// the consent to the reservation and contact card (same path as the webhook, so it is saved once), holds the
    /// subscription, and reports what happened.
    /// </summary>
    private async Task<NumberReturnResult> ProcessNumberReturnAsync(Guid reservationId, long txId, long consentId)
    {
        _logger.LogError("[NumberPayment] Step=Return ReservationId={ReservationId} TXID={TxId} CID={ConsentId} Query={Query}",
            reservationId, txId, consentId, Request.QueryString.Value);

        var result = new NumberReturnResult
        {
            ReservationId = reservationId,
            TxId = txId,
            ConsentId = consentId,
            Query = Request.Query.ToDictionary(pair => pair.Key, pair => pair.Value.ToString())
        };

        Organization? organization = null;
        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
            if (reservation == null)
            {
                _logger.LogError("[NumberPayment] Step=ReturnReservationNotFound ReservationId={ReservationId}", reservationId);
                result.Outcome = "Reservation not found";
                return result;
            }
            result.ReservationCode = reservation.ReservationCode;
            organization = await _organizationRepository.GetOrganizationByIdAsync(reservation.OrganizationId);
            if (organization == null)
            {
                result.Outcome = "Organization not found";
                return result;
            }

            var post = new NumberPostDto { RPGUID = reservationId.ToString(), ConsentID = consentId, TransactionID = txId };
            IActionResult? outcome = null;
            if (consentId > 0)
            {
                outcome = await ReceiveNumberConsentAsync(organization, reservation, post);
            }
            else if (txId > 0)
            {
                // The redirect carries no amount; take it from Number so the transaction check can match it.
                var transaction = await WithNumberSessionAsync(organization, reservation.OfficeId,
                    session => _numberService.GetTransactionAsync(session, txId, HttpContext.RequestAborted), HttpContext.RequestAborted);
                result.Transaction = transaction;
                post.Amount = transaction?.Amount ?? 0;
                outcome = await ReceiveNumberTransactionAsync(organization, reservation, post);
            }
            else
            {
                result.Outcome = "EasyPay sent no TXID or CID";
            }

            if (outcome != null)
                result.Outcome = outcome is ObjectResult objectResult
                    ? $"{objectResult.StatusCode ?? 200}: {objectResult.Value}"
                    : (outcome as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode is int status ? status.ToString() : outcome.GetType().Name;
            result.Consent = HttpContext.Items[NumberConsentItemKey] as NumberConsent;
            result.ContactCardId = HttpContext.Items[NumberCardItemKey] as int?;
            result.SubscriptionHeld = HttpContext.Items[NumberHoldItemKey] as bool?;

            var saved = await _reservationRepository.GetReservationByIdAsync(reservationId);
            result.ReservationConsentId = saved?.NumberConsentId;
        }
        catch (Exception ex)
        {
            // Never strand the payer on an error page; the webhook (production) is the backup.
            LogApplicationError(ex, "ReturnFromNumberForm", null);
            result.Outcome = "Error: " + ex.Message;
        }

        var website = organization?.Website?.Trim();
        if (!string.IsNullOrWhiteSpace(website))
            result.Website = website.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || website.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? website
                : "https://" + website.TrimStart('/');
        return result;
    }

    // HttpContext.Items keys: the consent/card/hold steps record what they did so the return page can show it.
    private const string NumberConsentItemKey = "Number.Consent";
    private const string NumberCardItemKey = "Number.ContactCardId";
    private const string NumberHoldItemKey = "Number.SubscriptionHeld";

    private sealed class NumberReturnResult
    {
        public Guid ReservationId { get; set; }
        public string? ReservationCode { get; set; }
        /// <summary>Every query value EasyPay put on the redirect.</summary>
        public Dictionary<string, string> Query { get; set; } = new();
        public long TxId { get; set; }
        public long ConsentId { get; set; }
        public string? Outcome { get; set; }
        public NumberConsent? Consent { get; set; }
        public NumberTransaction? Transaction { get; set; }
        public int? ReservationConsentId { get; set; }
        public int? ContactCardId { get; set; }
        public bool? SubscriptionHeld { get; set; }
        public string? Website { get; set; }
    }


    /// <summary>A PFS subscription exists only to save the card; hold it so it never bills on its own.</summary>
    private async Task TryHoldNumberSubscriptionAsync(Organization organization, Reservation reservation, long consentId)
    {
        try
        {
            HttpContext.Items[NumberHoldItemKey] = await WithNumberSessionAsync(organization, reservation.OfficeId,
                session => _numberService.HoldSubscriptionAsync(session, consentId, HttpContext.RequestAborted), HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NumberPayment] Step=SubscriptionHoldFailed ConsentID={ConsentId} ReservationId={ReservationId}", consentId, reservation.ReservationId);
        }
    }

    /// <summary>Save-card form: no charge, just a saved card to keep on the reservation and its contact.</summary>
    private async Task<IActionResult> ReceiveNumberConsentAsync(Organization organization, Reservation reservation, NumberPostDto post)
    {
        var consent = await WithNumberSessionAsync(organization, reservation.OfficeId,
            session => _numberService.GetConsentAsync(session, post.ConsentID, HttpContext.RequestAborted), HttpContext.RequestAborted);
        HttpContext.Items[NumberConsentItemKey] = consent;
        if (consent == null)
        {
            _logger.LogError("[NumberPayment] Step=NotVerified ConsentID={ConsentId} Reason=ConsentNotFoundAtNumber", post.ConsentID);
            return BadRequest("Saved card could not be verified");
        }

        _logger.LogInformation("[NumberPayment] Step=ConsentDetail ConsentID={ConsentId} IsEnabled={IsEnabled} RPGUID={RpGuid} CustomerRefID={CustomerRefId} CardType={CardType} LastFour={LastFour} Exp={Exp}",
            consent.Id, consent.IsEnabled, consent.RpGuid, consent.CustomerRefId, consent.CardType, consent.LastFour, consent.ExpDate);

        // Number's sample consent has a blank RPGUID, so a blank one is accepted; a different one is not.
        if (!string.IsNullOrWhiteSpace(consent.RpGuid)
            && !(Guid.TryParse(consent.RpGuid, out var consentGuid) && consentGuid == reservation.ReservationId))
        {
            _logger.LogError("[NumberPayment] Step=NotVerified ConsentID={ConsentId} Reason=ReservationMismatch ConsentRPGUID={ConsentRpGuid} ReservationId={ReservationId}",
                consent.Id, consent.RpGuid, reservation.ReservationId);
            return BadRequest("Saved card does not match the reservation");
        }
        if (!consent.IsEnabled)
        {
            _logger.LogError("[NumberPayment] Step=ConsentDisabled ConsentID={ConsentId} ReservationId={ReservationId}", consent.Id, reservation.ReservationId);
            return Ok();
        }

        await SaveNumberConsentAsync(reservation, consent.Id);
        await SaveNumberCardOnFileAsync(reservation, consent.Id, new NumberCardDetails(
            consent.CardType ?? post.CardType,
            consent.LastFour ?? post.CardNumber,
            consent.ExpDate ?? post.ExpireDate,
            consent.FirstName ?? post.CardholderFirstName ?? post.CustomerFirstName,
            consent.LastName ?? post.CardholderLastName ?? post.CustomerLastName));
        await TryHoldNumberSubscriptionAsync(organization, reservation, consent.Id);
        return Ok();
    }

    /// <summary>Payment form: verify the transaction, keep any saved card, and record the payment.</summary>
    private async Task<IActionResult> ReceiveNumberTransactionAsync(Organization organization, Reservation reservation, NumberPostDto post)
    {
        var reservationId = reservation.ReservationId;
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

        var consentId = transaction.ConsentId > 0 ? transaction.ConsentId : post.ConsentID;
        if (consentId > 0)
        {
            await SaveNumberConsentAsync(reservation, consentId);
            await SaveNumberCardOnFileAsync(reservation, consentId, new NumberCardDetails(
                post.CardType, post.CardNumber, post.ExpireDate,
                post.CardholderFirstName ?? post.CustomerFirstName, post.CardholderLastName ?? post.CustomerLastName));
            await TryHoldNumberSubscriptionAsync(organization, reservation, consentId);
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

        var cardLabel = string.IsNullOrWhiteSpace(post.CardNumber) ? null : $"{post.CardType} ending {post.CardNumber}".Trim();
        var payment = await RecordNumberPaymentAsync(reservation, new NumberPaymentToRecord(
            transaction.Id, approvedAmount.Value, transaction.Amount, transaction.IsPartialApproval,
            cardLabel, transaction.TxnCode ?? post.ApprovalCode, "Number card payment", SystemUserId));
        if (payment != null)
            _logger.LogInformation("[NumberPayment] Step=Recorded TxID={TxId} PaymentId={PaymentId} PaymentCode={PaymentCode}", transaction.Id, payment.PaymentId, payment.PaymentCode);

        return Ok();
    }

    private sealed record NumberPaymentToRecord(
        long TxId, decimal ApprovedAmount, decimal RequestedAmount, bool IsPartialApproval,
        string? CardLabel, string? TxnCode, string DescriptionPrefix, Guid CreatedBy);

    private sealed record NumberCardDetails(string? CardType, string? CardNumber, string? ExpireDate, string? FirstName, string? LastName);

    /// <summary>
    /// Applies an approved Number card payment to the reservation's unpaid invoices, oldest first,
    /// the same way the invoice list's Apply Payment does. Returns null when it was already recorded
    /// or can't be applied.
    /// </summary>
    private async Task<Payment?> RecordNumberPaymentAsync(Reservation reservation, NumberPaymentToRecord toRecord)
    {
        if (toRecord.TxId > int.MaxValue)
        {
            _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} Reason=TxIdTooLarge", toRecord.TxId);
            return null;
        }
        var numberTxId = (int)toRecord.TxId;
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

        var invoiceIds = await GetUnpaidInvoiceIdsAsync(reservation);
        if (invoiceIds.Count == 0)
        {
            _logger.LogError("[NumberPayment] Step=NotRecorded TxID={TxId} ReservationId={ReservationId} Amount={Amount} Reason=NoUnpaidInvoices", numberTxId, reservation.ReservationId, toRecord.ApprovedAmount);
            return null;
        }

        var cardText = string.IsNullOrWhiteSpace(toRecord.CardLabel) ? string.Empty : $" {toRecord.CardLabel}";
        var partialNote = toRecord.IsPartialApproval ? $", partial approval of {toRecord.RequestedAmount:0.00} requested" : string.Empty;
        var payment = new Payment
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            PaymentDate = DateOnly.FromDateTime(DateTime.Today),
            Amount = toRecord.ApprovedAmount,
            CostCodeId = costCode.CostCodeId,
            Description = $"{toRecord.DescriptionPrefix}{cardText} (TxID {numberTxId}{partialNote})",
            PaymentKindId = (int)PaymentKind.Invoice,
            PaymentTypeId = (int)PaymentType.CreditCard,
            NumberTxId = numberTxId,
            NumberTxnCode = toRecord.TxnCode,
            IsActive = true,
            CreatedBy = toRecord.CreatedBy
        };

        return await _accountingManager.ApplyInvoicePaymentAsync(payment, invoiceIds, null, officeAccess, toRecord.CreatedBy);
    }

    private async Task<List<Guid>> GetUnpaidInvoiceIdsAsync(Reservation reservation)
    {
        var invoices = await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = reservation.OrganizationId,
            OfficeIds = reservation.OfficeId.ToString(),
            ReservationId = reservation.ReservationId,
            IsActive = true,
            IncludeInactive = false,
            IncludePaid = false
        });
        return invoices.Select(invoice => invoice.InvoiceId).ToList();
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
    private async Task SaveNumberConsentAsync(Reservation reservation, long consentId)
    {
        if (consentId <= 0)
            return;
        if (consentId > int.MaxValue)
        {
            _logger.LogError("[NumberPayment] Step=ConsentNotSaved ConsentID={ConsentId} Reason=ConsentIdTooLarge", consentId);
            return;
        }
        if (reservation.NumberConsentId == (int)consentId)
            return;

        try
        {
            reservation.NumberConsentId = (int)consentId;
            reservation.ModifiedBy = SystemUserId;
            await _reservationRepository.UpdateByIdAsync(reservation);
            _logger.LogInformation("[NumberPayment] Step=ConsentSaved ReservationId={ReservationId} ConsentID={ConsentId}", reservation.ReservationId, consentId);
        }
        catch (Exception ex)
        {
            // The rest still runs; the card on file can be set again later.
            _logger.LogError(ex, "[NumberPayment] Step=ConsentNotSaved ReservationId={ReservationId} ConsentID={ConsentId}", reservation.ReservationId, consentId);
        }
    }

    /// <summary>
    /// Saves the card Number kept on file as the card on file of the reservation's first contact (the payer
    /// on the payment form). Only Number's token, last four, type, name and expiration are stored.
    /// </summary>
    private async Task SaveNumberCardOnFileAsync(Reservation reservation, long consentId, NumberCardDetails card)
    {
        if (consentId <= 0 || consentId > int.MaxValue)
            return;

        try
        {
            var contactId = reservation.ContactIds.FirstOrDefault();
            var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
            if (contact == null || contact.OrganizationId != reservation.OrganizationId)
            {
                _logger.LogError("[NumberPayment] Step=CardNotSaved ConsentID={ConsentId} ReservationId={ReservationId} Reason=NoContact", consentId, reservation.ReservationId);
                return;
            }

            var cardTypeId = MapNumberCardType(card.CardType);
            if (cardTypeId == null)
                _logger.LogWarning("[NumberPayment] Step=CardTypeUnknown ConsentID={ConsentId} CardType={CardType}; saved as Visa", consentId, card.CardType);

            var name = string.Join(' ', new[] { card.FirstName, card.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

            var saved = await _contactCardStore.SaveNumberCardAsync(contact, new ContactCard
            {
                CardTypeId = cardTypeId ?? (int)CardType.Visa,
                CardName = name,
                LastFour = ContactCardStore.ExtractLastFour(card.CardNumber),
                ExpirationDate = NormalizeExpiration(card.ExpireDate),
                NumberConsentId = (int)consentId
            }, SystemUserId);

            HttpContext.Items[NumberCardItemKey] = saved.ContactCardId;
            _logger.LogInformation("[NumberPayment] Step=CardSaved ConsentID={ConsentId} ContactId={ContactId} ContactCardId={ContactCardId}",
                consentId, contact.ContactId, saved.ContactCardId);
        }
        catch (Exception ex)
        {
            // The token is still on the reservation; the card can be saved again later.
            _logger.LogError(ex, "[NumberPayment] Step=CardNotSaved ConsentID={ConsentId} ReservationId={ReservationId}", consentId, reservation.ReservationId);
        }
    }

    // Number uses both names (Visa) and codes (VI, MC, DS, AX) depending on the call.
    private static int? MapNumberCardType(string? cardType)
    {
        var value = new string((cardType ?? string.Empty).Where(char.IsLetter).ToArray()).ToUpperInvariant();
        return value switch
        {
            "VISA" or "VI" => (int)CardType.Visa,
            "MC" or "MASTERCARD" or "MASTER" => (int)CardType.MasterCard,
            "DS" or "DI" or "DISC" or "DISCOVER" => (int)CardType.Discover,
            "AMEX" or "AMERICANEXPRESS" or "AX" or "AE" => (int)CardType.AmericanExpress,
            _ => null
        };
    }

    // Number posts "12/28"; its detail queries show "1228". Store MM/YY.
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

}
