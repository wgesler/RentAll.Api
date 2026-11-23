using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Models.Accounting;
using RentAll.Infrastructure.Services;

namespace RentAll.Api.Controllers;

/// <summary>
/// Public endpoints behind the payment pages (payments/pay-request and payments/pay-response). The payer has no
/// RentAll login; the emailed link's token (Accounting.PaymentLink) is what identifies the reservation.
/// </summary>
public partial class ReservationController
{
    /// <summary>What the pay-request page shows: who is asking, and for which reservation.</summary>
    [AllowAnonymous]
    [HttpGet("external/pay-request/{token}")]
    public async Task<IActionResult> GetPayRequest(string token)
    {
        var request = new PayRequestRequestDto(token);
        var (isValid, errorMessage) = request.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var (link, reservation, organization, error) = await LoadPaymentLinkAsync(request.Token);
            if (error != null)
                return error;

            var office = await _organizationRepository.GetOfficeByIdAsync(reservation!.OfficeId, reservation.OrganizationId);
            var logoUrl = StoragePathBuilder.PublicOfficeLogoUrl(_storageSettings.AzureBlobBaseUrl, reservation.OrganizationId, office?.Name);
            return Ok(new PayRequestResponseDto(link!, reservation, organization!, office, logoUrl));
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "GetPayRequest", null);
            return ServerError("The payment request could not be loaded.");
        }
    }

    /// <summary>Starts a fresh EasyPay card form for the pay-request page to show in its iFrame.</summary>
    [AllowAnonymous]
    [HttpPost("external/pay-request/{token}/card-form")]
    public async Task<IActionResult> CreatePayRequestCardForm(string token)
    {
        var request = new PayRequestRequestDto(token);
        var (isValid, errorMessage) = request.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        int? officeId = null;
        try
        {
            var (link, reservation, organization, error) = await LoadPaymentLinkAsync(request.Token);
            if (error != null)
                return error;
            officeId = reservation!.OfficeId;

            // EasyPay returns the payer to the organization's pay-response page (inside the iFrame) with ?TXID=..&CID=..
            var pages = await ResolvePaymentPagesAsync(reservation.OrganizationId, reservation.OfficeId);
            var redirectUrl = pages.ResponseUrl + "/" + Uri.EscapeDataString(link!.Token);
            var formUrl = await CreateEasyPayCardFormUrlAsync(organization!, reservation, redirectUrl, HttpContext.RequestAborted);
            return Ok(new PayRequestFormResponseDto(formUrl));
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "CreatePayRequestCardForm", officeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "CreatePayRequestCardForm", officeId);
            return ServerError("The card form could not be started.");
        }
    }

    /// <summary>
    /// The pay-response page sends what EasyPay returned (?TXID=..&amp;CID=..). The consent is confirmed with EasyPay,
    /// saved to the reservation and the contact's card, and its subscription put on hold; then the link is marked done.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("external/pay-response/{token}")]
    public async Task<IActionResult> ReceivePayResponse(string token, [FromQuery(Name = "TXID")] long txId = 0, [FromQuery(Name = "CID")] long consentId = 0)
    {
        var request = new PayResponseRequestDto(token, txId, consentId);
        var (isValid, errorMessage) = request.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var (link, reservation, organization, error) = await LoadPaymentLinkAsync(request.Token);
            if (error != null)
                return error;

            var result = await ProcessNumberReturnAsync(reservation!.ReservationId, request.TxId, request.ConsentId);
            var saved = request.ConsentId > 0 && result.ReservationConsentId == request.ConsentId;
            if (saved)
            {
                await _accountingRepository.CompletePaymentLinkByTokenAsync(link!.Token);
                // Only the first completion sends it, so reopening the pay-response page doesn't email the tenant again.
                if (link.CompletedOn == null)
                    await TrySendPaymentReceivedEmailAsync(link, reservation, organization!);
            }

            return Ok(new PayResponseResponseDto(saved, request.ConsentId, result.ReservationCode, result.Consent, result.Website));
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "ReceivePayResponse", null);
            return ServerError("The payment response could not be processed.");
        }
    }

    /// <summary>
    /// ACH Debit: saves the tenant's bank account with EasyPay (ACH/Combo at $0, so nothing is debited) and keeps the
    /// returned ConsentID on the tenant contact's bank account (ContactBank), with the account number encrypted.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("external/pay-request/{token}/ach")]
    public async Task<IActionResult> SavePayRequestAch(string token, [FromBody] PayRequestAchRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Bank account details are required.");
        dto.Token = (token ?? string.Empty).Trim();
        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        int? officeId = null;
        try
        {
            var (link, reservation, organization, error) = await LoadPaymentLinkAsync(dto.Token);
            if (error != null)
                return error;
            officeId = reservation!.OfficeId;

            var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, reservation.OrganizationId);
            var referenceNo = (office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode)?.Trim();
            var contactId = reservation.ContactIds.FirstOrDefault();
            var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
            if (contact != null && contact.OrganizationId != reservation.OrganizationId)
                contact = null;

            var ach = dto.ToModel();
            ach.Email = contact?.Email?.Trim();
            ach.Phone = contact?.Phone?.Trim();
            ach.Address1 = contact?.Address1?.Trim();
            ach.City = contact?.City?.Trim();
            ach.State = contact?.State?.Trim();
            ach.Zip = contact?.Zip?.Trim();
            ach.ServiceDescription = $"Reservation {referenceNo ?? reservation.ReservationCode}";
            ach.ClientRefId = referenceNo ?? reservation.ReservationCode;
            ach.RpGuid = reservation.ReservationId.ToString();

            var result = await WithNumberSessionAsync(organization!, reservation.OfficeId,
                session => _numberService.CreateAchConsentAsync(session, ach, HttpContext.RequestAborted), HttpContext.RequestAborted);
            if (!result.Saved || result.ConsentId > int.MaxValue)
            {
                _logger.LogError("[NumberAch] Step=NotSaved ReservationId={ReservationId} ErrCode={ErrCode} ErrMsg={ErrMsg} RespMsg={RespMsg}",
                    reservation.ReservationId, result.ErrCode, result.ErrMsg, result.RespMsg);
                return Ok(new PayRequestAchResponseDto(result));
            }

            var bank = new ContactBank
            {
                OrganizationId = reservation.OrganizationId,
                OfficeId = reservation.OfficeId,
                BankAccountTypeId = dto.AccountType,
                NameOnAccount = dto.NameOnAccount.Trim(),
                BankName = dto.BankName.Trim(),
                RoutingNumber = dto.RoutingDigits,
                LastFour = dto.AccountLast4,
                NumberConsentId = (int)result.ConsentId,
                ReservationId = reservation.ReservationId,
                AuthorizedOn = DateTimeOffset.UtcNow
            };
            // The tenant contact's bank account (ContactBank) is created or replaced and linked. Without a contact, the
            // account is kept on its own row for the reservation.
            var saved = contact != null
                ? await _contactBankStore.SaveAsync(contact, bank, dto.AccountDigits, link!.CreatedBy)
                : await _contactBankStore.UpsertAsync(reservation.OrganizationId, reservation.OfficeId, null, bank, dto.AccountDigits);
            await _accountingRepository.CompletePaymentLinkByTokenAsync(link!.Token);
            _logger.LogError("[NumberAch] Step=Saved ReservationId={ReservationId} ConsentID={ConsentId} AccountLast4={AccountLast4}",
                reservation.ReservationId, saved.NumberConsentId, saved.LastFour);
            return Ok(new PayRequestAchResponseDto(saved));
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "SavePayRequestAch", officeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "SavePayRequestAch", officeId);
            return ServerError("The bank account could not be saved.");
        }
    }

    /// <summary>Finds the link (token already checked by the request DTO), its reservation and organization; returns an error result when it can't be used.</summary>
    private async Task<(PaymentLink? Link, Reservation? Reservation, Organization? Organization, IActionResult? Error)> LoadPaymentLinkAsync(string token)
    {
        var link = await _accountingRepository.GetPaymentLinkByTokenAsync(token);
        if (link == null)
            return (null, null, null, NotFound("This payment link is not valid."));
        // The payer isn't signed in: tag this request's log rows (including [EasyPayLog]) with the link's organization so they show in its Application Log.
        SetApplicationLogContext(link.OrganizationId, link.OfficeId);
        if (link.ExpiresOn < DateTimeOffset.UtcNow)
            return (link, null, null, StatusCode(StatusCodes.Status410Gone, "This payment link has expired. Please ask for a new one."));

        var reservation = await _reservationRepository.GetReservationByIdAsync(link.ReservationId);
        if (reservation == null || reservation.OrganizationId != link.OrganizationId)
            return (link, null, null, NotFound("This payment link is not valid."));

        var organization = await _organizationRepository.GetOrganizationByIdAsync(link.OrganizationId);
        if (organization == null)
            return (link, reservation, null, NotFound("This payment link is not valid."));

        return (link, reservation, organization, null);
    }

}
