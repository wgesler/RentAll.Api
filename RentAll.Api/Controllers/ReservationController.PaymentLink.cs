namespace RentAll.Api.Controllers;

using RentAll.Domain.Models.Accounting;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Services;

public partial class ReservationController
{
    [HttpPost("{reservationId:guid}/payment-link")]
    public async Task<IActionResult> CreateReservationPaymentLink(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");

        int? officeId = null;
        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            officeId = reservation?.OfficeId;
            var paymentUrl = await CreatePaymentUrlForReservationAsync(reservationId, HttpContext.RequestAborted);
            return Ok(new ReservationPaymentLinkResponseDto(paymentUrl));
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "CreatePaymentLink", officeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "CreatePaymentLink", officeId);
            return ServerError("An error occurred while creating the payment link");
        }
    }

    [HttpPost("{reservationId:guid}/payment-request-email")]
    public async Task<IActionResult> SendReservationPaymentRequestEmail(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");

        Reservation? reservation = null;
        try
        {
            if (string.IsNullOrWhiteSpace(_sendGridSettings.PaymentInformationRequestTemplateId))
                return BadRequest("SendGridSettings:PaymentInformationRequestTemplateId is not set.");

            reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var organization = await _organizationRepository.GetOrganizationByIdAsync(CurrentOrganizationId);
            if (organization == null)
                return NotFound("Organization not found");

            var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, CurrentOrganizationId);
            var referenceNo = office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode;
            if (string.IsNullOrWhiteSpace(referenceNo))
                return BadRequest(office?.UseExternalRefNo == true ? "This reservation does not have an external reference number." : "Reservation code is required.");

            var contactId = reservation.ContactIds.FirstOrDefault();
            var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
            if (contact != null && contact.OrganizationId != CurrentOrganizationId)
                contact = null;

            var toEmail = contact?.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(toEmail))
                return BadRequest("The tenant does not have an email address.");

            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, reservation.OfficeId);
            var paymentUrl = await CreatePaymentUrlForReservationAsync(reservationId, HttpContext.RequestAborted);
            var payerName = PayerName(contact, reservation);
            var toName = string.Join(' ', new[] { payerName.First, payerName.Last }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
            if (string.IsNullOrWhiteSpace(toName))
                toName = (reservation.TenantName ?? reservation.ContactName ?? toEmail).Trim();
            var salutationName = toName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? toName;
            var address = AccountingOfficeAddress(accountingOffice);

            var email = new Email
            {
                OrganizationId = CurrentOrganizationId,
                OfficeId = reservation.OfficeId,
                OfficeName = office?.Name,
                PropertyId = reservation.PropertyId,
                ReservationId = reservation.ReservationId,
                FromRecipient = EmailAddress.Create(string.Empty, string.Empty),
                ToRecipients = [EmailAddress.Create(toEmail, toName)],
                Subject = "Your Payment Details Are Needed",
                PlainTextContent = string.Empty,
                HtmlContent = $"Payment request sent with SendGrid template {_sendGridSettings.PaymentInformationRequestTemplateId}. PaymentUrl={paymentUrl}",
                TemplateId = _sendGridSettings.PaymentInformationRequestTemplateId,
                TemplateData = new Dictionary<string, object?>
                {
                    ["salutationName"] = salutationName,
                    ["companyName"] = organization.Name ?? string.Empty,
                    ["reservationCode"] = referenceNo.Trim(),
                    ["paymentUrl"] = paymentUrl,
                    ["accountingPhone"] = FormatPhone(accountingOffice?.Phone),
                    ["accountingOfficeAddressSingleLine"] = address,
                    ["logoUrl"] = StoragePathBuilder.PublicOfficeLogoUrl(
                        _storageSettings.AzureBlobBaseUrl,
                        CurrentOrganizationId,
                        office?.Name)
                },
                EmailType = EmailType.PaymentRequest,
                EmailStatus = EmailStatus.Attempting,
                CreatedBy = CurrentUser
            };

            if (string.IsNullOrWhiteSpace(email.FromRecipient.Email))
                email.FromRecipient = EmailAddress.Create(_sendGridSettings.FromEmail, _sendGridSettings.FromName);

            var result = await _emailManager.SendEmail(organization.GetSendGridKeyVaultSecretName(), email);
            if (result.EmailStatus == EmailStatus.Succeeded)
                return Ok(new ReservationPaymentLinkResponseDto(paymentUrl));
            if (result.EmailStatus == EmailStatus.Failed)
                return BadRequest(string.IsNullOrWhiteSpace(result.LastError) ? "Payment request email failed." : result.LastError);
            return Accepted(new ReservationPaymentLinkResponseDto(paymentUrl));
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "SendPaymentRequestEmail", reservation?.OfficeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "SendPaymentRequestEmail", reservation?.OfficeId);
            return ServerError("An error occurred while sending the payment request email");
        }
    }

    private async Task<string> CreatePaymentUrlForReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId)
            ?? throw new InvalidOperationException("Reservation not found");

        var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, CurrentOrganizationId);
        var referenceNo = office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode;
        if (string.IsNullOrWhiteSpace(referenceNo))
            throw new InvalidOperationException(office?.UseExternalRefNo == true ? "This reservation does not have an external reference number." : "Reservation code is required.");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(CurrentOrganizationId)
            ?? throw new InvalidOperationException("Organization not found");

        var contactId = reservation.ContactIds.FirstOrDefault();
        var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
        if (contact != null && contact.OrganizationId != CurrentOrganizationId)
            contact = null;

        var payerName = PayerName(contact, reservation);
        var amount = await GetUnpaidInvoiceAmountAsync(reservation);
        if (amount <= 0)
            amount = 20m;

        var payForm = new NumberPayForm
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            RedirectUrl = organization.Website?.Trim(),
            ReferenceId = referenceNo.Trim(),
            ReferenceGuid = reservation.ReservationId,
            Amount = amount,
            FirstName = payerName.First,
            LastName = payerName.Last,
            Email = contact?.Email?.Trim() ?? string.Empty,
            Phone = contact?.Phone?.Trim() ?? string.Empty,
            StreetAddress = contact?.Address1?.Trim() ?? string.Empty,
            City = contact?.City?.Trim() ?? string.Empty,
            State = contact?.State?.Trim() ?? string.Empty,
            Zip = contact?.Zip?.Trim() ?? string.Empty
        };
        return await WithNumberSessionAsync(organization, reservation.OfficeId,
            session => _numberService.CreatePayFormUrlAsync(session, payForm, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Runs a Number call with the office's stored session key. If Number rejects the key (5030 expired,
    /// 5050 IP changed), authenticates once more and retries once. Only once: Number locks the IP after
    /// 6 failed sign-ins in a row.
    /// </summary>
    private async Task<T> WithNumberSessionAsync<T>(Organization organization, int officeId, Func<NumberSession, Task<T>> call, CancellationToken cancellationToken)
    {
        var session = await GetStoredNumberSessionAsync(organization, officeId, forceRefresh: false, cancellationToken);
        try
        {
            return await call(session);
        }
        catch (NumberSessionExpiredException ex)
        {
            _logger.LogWarning("[NumberSession] Step=Rejected OfficeId={OfficeId} ErrCode={ErrCode}; authenticating again", officeId, ex.ErrCode);
            session = await GetStoredNumberSessionAsync(organization, officeId, forceRefresh: true, cancellationToken);
            return await call(session);
        }
    }

    // The stored procedure deletes a key once its ExpiresOn passes, so a null read means sign in again.
    private async Task<NumberSession> GetStoredNumberSessionAsync(Organization organization, int officeId, bool forceRefresh, CancellationToken cancellationToken)
    {
        var credential = await _accountingRepository.GetNumberCredentialByOfficeIdAsync(organization.OrganizationId, officeId);
        if (credential == null || forceRefresh)
        {
            var created = await _numberService.CreateSessionAsync(organization.GetNumberAcctCodeKeyVaultSecretName(), organization.GetNumberTokenKeyVaultSecretName(), cancellationToken);
            var fresh = new NumberCredential
            {
                OrganizationId = organization.OrganizationId,
                OfficeId = officeId,
                SessKey = created.SessKey,
                MerchId = created.MerchId,
                ExpiresOn = created.ExpiresOn
            };
            credential = credential == null
                ? await _accountingRepository.AddNumberCredentialAsync(fresh)
                : await _accountingRepository.UpdateNumberCredentialByOfficeIdAsync(fresh);
        }

        if (string.IsNullOrWhiteSpace(credential.SessKey))
            throw new InvalidOperationException("Number session was not stored for this office.");

        return new NumberSession { SessKey = credential.SessKey, MerchId = credential.MerchId, ExpiresOn = credential.ExpiresOn };
    }

    private static (string First, string Last) PayerName(Contact? contact, Reservation reservation)
    {
        var first = contact?.FirstName?.Trim() ?? string.Empty;
        var last = contact?.LastName?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(first) || !string.IsNullOrWhiteSpace(last))
            return (first, last);

        var name = (contact?.FullName ?? reservation.TenantName ?? reservation.ContactName ?? string.Empty).Trim();
        var space = name.IndexOf(' ');
        if (space < 0)
            return (name, string.Empty);
        return (name[..space], name[(space + 1)..].Trim());
    }

    private static string AccountingOfficeAddress(AccountingOffice? accountingOffice)
    {
        if (accountingOffice == null)
            return string.Empty;

        var cityState = string.Join(", ", new[] { accountingOffice.City, accountingOffice.State }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.Join(' ', new[]
        {
            accountingOffice.Address1,
            accountingOffice.Address2,
            accountingOffice.Suite,
            cityState,
            accountingOffice.Zip
        }.Select(part => part?.Trim()).Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>
    /// Matches UI FormatterService.phoneNumber: US 10-digit as (XXX) XXX-XXXX; otherwise original.
    /// </summary>
    private static string FormatPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var trimmed = phone.Trim();
        if (trimmed.StartsWith('+'))
        {
            var internationalDigits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (!(internationalDigits.Length == 11 && internationalDigits.StartsWith('1')))
                return phone;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits.StartsWith('1'))
            digits = digits[1..];

        if (digits.Length == 10)
            return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";

        return phone;
    }

    private async Task<decimal> GetUnpaidInvoiceAmountAsync(Reservation reservation)
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

        var amount = invoices.Sum(invoice => invoice.TotalAmount - invoice.PaidAmount);
        return amount < 0 ? 0 : decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
