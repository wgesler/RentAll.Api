using RentAll.Api.Dtos.Reservations.Reservations;
using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Controllers;

public partial class ReservationController
{
    /// <summary>
    /// ACH Debit: debits the bank account Number saved for the reservation's tenant (ACH/ProcPayment), for an amount
    /// staff enter, and applies it to the reservation's unpaid invoices oldest first as an ACH payment.
    /// </summary>
    [HttpPost("{reservationId:guid}/charge-bank-on-file")]
    public async Task<IActionResult> ChargeBankOnFile(Guid reservationId, [FromBody] ChargeCardOnFileRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Debit data is required");
        dto.ReservationId = reservationId;
        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var amount = dto.Amount;
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

            var (consentId, bankLabel) = await ResolveBankOnFileAsync(reservation);
            if (consentId is not > 0)
                return BadRequest("This reservation's tenant has no bank account saved with EasyPay.");

            // Check everything needed to record the payment before debiting, so an account is never debited for
            // money that can't be applied.
            if (await ResolveAchPaymentCostCodeAsync(reservation.OrganizationId, reservation.OfficeId) == null)
                return BadRequest("This office has no active Payment cost code for ACH.");
            if ((await GetUnpaidInvoiceIdsAsync(reservation)).Count == 0)
                return BadRequest("This reservation has no unpaid invoices to apply the debit to.");

            var result = await WithNumberSessionAsync(organization, reservation.OfficeId,
                session => _numberService.ChargeBankAccountAsync(session, consentId.Value, amount, HttpContext.RequestAborted), HttpContext.RequestAborted);

            if (!result.Approved)
            {
                _logger.LogError("[EasyPay BankChargeDeclined] ReservationId={ReservationId} ConsentID={ConsentId} Amount={Amount} TxID={TxId} ErrCode={ErrCode} Message={Message}",
                    reservation.ReservationId, consentId, amount, result.TxId, result.ErrCode, result.Message);
                // A decline is a normal answer, not a failed request: the dialog shows Number's code and message.
                return Ok(new ChargeCardOnFileResponseDto(result, amount, 0, bankLabel));
            }

            var payment = await RecordNumberPaymentAsync(reservation, new NumberPaymentToRecord(
                result.TxId, amount, amount, false, bankLabel, result.TxnCode,
                "EasyPay ACH debit", CurrentUser, PaymentType.Ach));
            var response = new ChargeCardOnFileResponseDto(result, amount, amount, bankLabel, payment,
                payment == null ? $"EasyPay accepted ACH debit {result.TxId} for {amount:0.00}, but the payment could not be recorded. Record it by hand." : null);

            _logger.LogError("[EasyPay BankAccountCharged] ReservationId={ReservationId} TxID={TxId} Amount={Amount} PaymentId={PaymentId}",
                reservation.ReservationId, result.TxId, amount, payment?.PaymentId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "ChargeBankOnFile", officeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "ChargeBankOnFile", officeId);
            return ServerError("An error occurred while debiting the bank account on file");
        }
    }

    // The tenant contact's bank account saved with Number (ContactBank with an ACH consent).
    private async Task<(long? ConsentId, string? BankLabel)> ResolveBankOnFileAsync(Reservation reservation)
    {
        var contactId = reservation.ContactIds.FirstOrDefault();
        var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
        if (contact == null || contact.OrganizationId != reservation.OrganizationId || contact.ContactBankId is not > 0)
            return (null, null);

        var bank = await _contactRepository.GetContactBankByIdAsync(contact.ContactBankId.Value, contact.OrganizationId, contact.OfficeId);
        if (bank?.NumberConsentId is not > 0)
            return (null, null);

        var name = string.IsNullOrWhiteSpace(bank.BankName) ? "Bank account" : bank.BankName.Trim();
        var label = string.IsNullOrWhiteSpace(bank.LastFour) ? name : $"{name} ending {bank.LastFour}";
        return (bank.NumberConsentId, label);
    }
}
