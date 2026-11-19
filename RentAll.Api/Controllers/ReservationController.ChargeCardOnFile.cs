using RentAll.Api.Dtos.Reservations.Reservations;
using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Controllers;

public partial class ReservationController
{
    /// <summary>
    /// Charges the card Number saved for this reservation, for an amount staff enter, and applies the
    /// approved amount to the reservation's unpaid invoices oldest first.
    /// </summary>
    [HttpPost("{reservationId:guid}/charge-card-on-file")]
    public async Task<IActionResult> ChargeCardOnFile(Guid reservationId, [FromBody] ChargeCardOnFileRequestDto dto)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");
        if (dto == null || dto.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var amount = decimal.Round(dto.Amount, 2, MidpointRounding.AwayFromZero);
        int? officeId = null;
        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");
            officeId = reservation.OfficeId;

            var organization = await _organizationRepository.GetOrganizationByIdAsync(CurrentOrganizationId);
            if (organization == null)
                return NotFound("Organization not found");

            var (consentId, cardLabel) = await ResolveCardOnFileAsync(reservation);
            if (consentId is not > 0)
                return BadRequest("This reservation has no card on file from Number.");

            // Check everything needed to record the payment before charging, so a card is never
            // charged for money that can't be applied.
            if (await ResolveCreditCardPaymentCostCodeAsync(reservation.OrganizationId, reservation.OfficeId) == null)
                return BadRequest("This office has no active Payment cost code for credit cards.");
            if ((await GetUnpaidInvoiceIdsAsync(reservation)).Count == 0)
                return BadRequest("This reservation has no unpaid invoices to apply the charge to.");

            var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, CurrentOrganizationId);
            var referenceNo = (office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode)?.Trim();

            var result = await WithNumberSessionAsync(organization, reservation.OfficeId,
                session => _numberService.ChargeStoredCardAsync(session, new NumberStoredCardCharge
                {
                    ConsentId = consentId.Value,
                    Amount = amount,
                    ServiceDescription = $"Reservation {referenceNo ?? reservation.ReservationCode}",
                    ClientRefId = referenceNo ?? reservation.ReservationCode,
                    RpGuid = reservation.ReservationId.ToString(),
                    User = CurrentUser.ToString()
                }, HttpContext.RequestAborted), HttpContext.RequestAborted);

            if (!result.Approved)
            {
                _logger.LogError("[NumberCharge] Step=Declined ReservationId={ReservationId} ConsentID={ConsentId} Amount={Amount} TxID={TxId} ErrCode={ErrCode} Message={Message}",
                    reservation.ReservationId, consentId, amount, result.TxId, result.ErrCode, result.Message);
                return BadRequest(new ChargeCardOnFileResponseDto
                {
                    Approved = false,
                    TransactionId = result.TxId,
                    RequestedAmount = amount,
                    Message = result.Message ?? "The card was declined."
                });
            }

            // Number says the approved amount can differ from the requested one; trust what it approved.
            var approved = result.ApprovedTotal > 0 ? result.ApprovedTotal
                : result.AuthorizedAmount > 0 ? result.AuthorizedAmount
                : result.IsPartialApproval ? 0 : amount;
            var response = new ChargeCardOnFileResponseDto
            {
                Approved = true,
                TransactionId = result.TxId,
                RequestedAmount = amount,
                ApprovedAmount = approved,
                IsPartialApproval = result.IsPartialApproval || (approved > 0 && approved < amount),
                Message = result.Message
            };

            if (approved <= 0)
            {
                _logger.LogError("[NumberCharge] Step=NotRecorded ReservationId={ReservationId} TxID={TxId} Reason=ApprovedAmountUnclear", reservation.ReservationId, result.TxId);
                response.Message = $"Number approved transaction {result.TxId} but did not report the approved amount. Record the payment by hand.";
                return Ok(response);
            }

            var payment = await RecordNumberPaymentAsync(reservation, new NumberPaymentToRecord(
                result.TxId, approved, amount, response.IsPartialApproval, cardLabel, result.TxnCode,
                "Number card on file charge", CurrentUser));
            response.PaymentId = payment?.PaymentId;
            response.PaymentCode = payment?.PaymentCode;
            if (payment == null)
                response.Message = $"Number approved transaction {result.TxId} for {approved:0.00}, but the payment could not be recorded. Record it by hand.";

            _logger.LogInformation("[NumberCharge] Step=Charged ReservationId={ReservationId} TxID={TxId} Requested={Requested} Approved={Approved} PaymentId={PaymentId}",
                reservation.ReservationId, result.TxId, amount, approved, payment?.PaymentId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "ChargeCardOnFile", officeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "ChargeCardOnFile", officeId);
            return ServerError("An error occurred while charging the card on file");
        }
    }

    // The reservation's saved Number card, falling back to its first contact's card on file.
    private async Task<(long? ConsentId, string? CardLabel)> ResolveCardOnFileAsync(Reservation reservation)
    {
        ContactCard? card = null;
        var contactId = reservation.ContactIds.FirstOrDefault();
        var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
        if (contact != null && contact.OrganizationId == reservation.OrganizationId && contact.ContactCardId is > 0)
            card = await _contactRepository.GetContactCardByIdAsync(contact.ContactCardId.Value, contact.OrganizationId, contact.OfficeId);

        var consentId = reservation.NumberConsentId is > 0 ? reservation.NumberConsentId : card?.NumberConsentId;
        // Only describe the card when it's the one being charged.
        var label = card != null && card.NumberConsentId == consentId && !string.IsNullOrWhiteSpace(card.LastFour)
            ? $"{(CardType)card.CardTypeId} ending {card.LastFour}"
            : null;
        return (consentId, label);
    }
}
