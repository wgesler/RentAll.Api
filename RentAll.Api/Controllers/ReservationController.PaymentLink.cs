namespace RentAll.Api.Controllers;

using RentAll.Domain.Models.Accounting;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Services;

public partial class ReservationController
{
    [HttpPost("{reservationId:guid}/payment-link")]
    public async Task<IActionResult> CreateReservationPaymentLink(Guid reservationId)
    {
        var request = new ReservationPaymentLinkRequestDto(reservationId);
        var (isValid, errorMessage) = request.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        int? officeId = null;
        try
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");
            officeId = reservation.OfficeId;
            var (paymentUrl, _) = await CreatePayRequestUrlAsync(reservation, CurrentUser);
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
        var request = new ReservationPaymentLinkRequestDto(reservationId);
        var (isValid, errorMessage) = request.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        Reservation? reservation = null;
        try
        {
            reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            // The organization's SendGrid template, set in Settings > Email > SendGrid Templates.
            var storedTemplate = await _emailRepository.GetEmailTemplateByNameAsync(CurrentOrganizationId, reservation.OfficeId, EmailTemplateNames.PaymentRequest);
            var templateId = storedTemplate?.TemplateId?.Trim();
            if (string.IsNullOrWhiteSpace(templateId))
                return BadRequest("No Payment Request template is set. Add its SendGrid template ID in Settings > Email > SendGrid Templates.");

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
            var (paymentUrl, validationToken) = await CreatePayRequestUrlAsync(reservation, CurrentUser);
            var payerName = PayerName(contact, reservation);
            var toName = string.Join(' ', new[] { payerName.First, payerName.Last }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
            if (string.IsNullOrWhiteSpace(toName))
                toName = (reservation.TenantName ?? reservation.ContactName ?? toEmail).Trim();
            var salutationName = toName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? toName;
            var address = AccountingOfficeAddress(accountingOffice);
            // The staff member who clicked Send Payment Email signs it.
            var sender = await _userRepository.GetUserByIdAsync(CurrentUser);
            var senderName = string.Join(' ', new[] { sender?.FirstName, sender?.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

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
                HtmlContent = $"Payment request sent with SendGrid template {templateId}. PaymentUrl={paymentUrl}",
                TemplateId = templateId,
                TemplateData = new Dictionary<string, object?>
                {
                    ["salutationName"] = salutationName,
                    ["senderName"] = senderName,
                    ["companyName"] = organization.Name ?? string.Empty,
                    ["companyOffice"] = string.Join(' ', new[] { organization.Name, office?.Name }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim())),
                    ["displayName"] = organization.DisplayName?.Trim() ?? string.Empty,
                    ["reservationCode"] = referenceNo.Trim(),
                    ["paymentUrl"] = paymentUrl,
                    ["validationToken"] = validationToken,
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

    /// <summary>How long an emailed payment link stays usable.</summary>
    private static readonly TimeSpan PaymentLinkLifetime = TimeSpan.FromDays(3);

    /// <summary>
    /// Creates a payment link (Accounting.PaymentLink) for the reservation and returns the address of the organization's
    /// pay-request page with its token, plus the token itself. Nothing is sent to Number yet: the page asks for a fresh form each time it opens.
    /// </summary>
    private async Task<(string Url, string Token)> CreatePayRequestUrlAsync(Reservation reservation, Guid createdBy)
    {
        var pages = await ResolvePaymentPagesAsync(reservation.OrganizationId, reservation.OfficeId);
        var link = await _accountingRepository.AddPaymentLinkAsync(new PaymentLink
        {
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            ReservationId = reservation.ReservationId,
            ExpiresOn = DateTimeOffset.UtcNow.Add(PaymentLinkLifetime),
            CreatedBy = createdBy
        });
        return (pages.RequestUrl + "?token=" + Uri.EscapeDataString(link.Token), link.Token);
    }

    /// <summary>
    /// Emails the tenant the Payment Received template once their card is saved through a payment link. A missing template
    /// or a failed send goes to the Application Log and never fails the card save.
    /// </summary>
    private async Task TrySendPaymentReceivedEmailAsync(PaymentLink link, Reservation reservation, Organization organization)
    {
        try
        {
            var storedTemplate = await _emailRepository.GetEmailTemplateByNameAsync(reservation.OrganizationId, reservation.OfficeId, EmailTemplateNames.PaymentReceived);
            var templateId = storedTemplate?.TemplateId?.Trim();
            if (string.IsNullOrWhiteSpace(templateId))
                throw new InvalidOperationException("No Payment Received template is set. Add its SendGrid template ID in Settings > Email > SendGrid Templates.");

            var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, reservation.OrganizationId);
            var referenceNo = office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode;
            var contactId = reservation.ContactIds.FirstOrDefault();
            var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
            if (contact != null && contact.OrganizationId != reservation.OrganizationId)
                contact = null;

            var toEmail = contact?.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("The tenant does not have an email address, so the Payment Received email was not sent.");

            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(reservation.OrganizationId, reservation.OfficeId);
            var payerName = PayerName(contact, reservation);
            var toName = string.Join(' ', new[] { payerName.First, payerName.Last }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
            if (string.IsNullOrWhiteSpace(toName))
                toName = (reservation.TenantName ?? reservation.ContactName ?? toEmail).Trim();
            var salutationName = toName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? toName;

            var email = new Email
            {
                OrganizationId = reservation.OrganizationId,
                OfficeId = reservation.OfficeId,
                OfficeName = office?.Name,
                PropertyId = reservation.PropertyId,
                ReservationId = reservation.ReservationId,
                FromRecipient = EmailAddress.Create(_sendGridSettings.FromEmail, _sendGridSettings.FromName),
                ToRecipients = [EmailAddress.Create(toEmail, toName)],
                Subject = "Payment Information Received",
                PlainTextContent = string.Empty,
                HtmlContent = $"Payment received sent with SendGrid template {templateId}.",
                TemplateId = templateId,
                TemplateData = new Dictionary<string, object?>
                {
                    ["salutationName"] = salutationName,
                    ["companyName"] = organization.Name ?? string.Empty,
                    ["companyOffice"] = string.Join(' ', new[] { organization.Name, office?.Name }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim())),
                    ["displayName"] = organization.DisplayName?.Trim() ?? string.Empty,
                    ["reservationCode"] = referenceNo?.Trim() ?? string.Empty,
                    ["validationToken"] = link.Token,
                    ["accountingPhone"] = FormatPhone(accountingOffice?.Phone),
                    ["accountingOfficeAddressSingleLine"] = AccountingOfficeAddress(accountingOffice),
                    ["logoUrl"] = StoragePathBuilder.PublicOfficeLogoUrl(_storageSettings.AzureBlobBaseUrl, reservation.OrganizationId, office?.Name)
                },
                EmailType = EmailType.PaymentReceived,
                EmailStatus = EmailStatus.Attempting,
                CreatedBy = link.CreatedBy
            };

            var result = await _emailManager.SendEmail(organization.GetSendGridKeyVaultSecretName(), email);
            if (result.EmailStatus == EmailStatus.Failed)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.LastError) ? "Payment Received email failed." : result.LastError);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "SendPaymentReceivedEmail", reservation.OfficeId);
        }
    }

    /// <summary>
    /// The organization's pay-request and pay-response pages: the office's row, the organization's row, or RentAll's own
    /// (system default). Relative URLs are RentAll pages, resolved against NumberSettings:PaymentPagesBaseUrl.
    /// </summary>
    private async Task<(string RequestUrl, string ResponseUrl)> ResolvePaymentPagesAsync(Guid organizationId, int officeId)
    {
        var page = await _accountingRepository.GetPaymentPageByOfficeIdAsync(organizationId, officeId)
            ?? throw new InvalidOperationException("No payment pages are set up (Accounting.PaymentPage has no system default).");

        string Resolve(string url)
        {
            url = url.Trim();
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url.TrimEnd('/');
            var baseUrl = _numberSettings.PaymentPagesBaseUrl?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("NumberSettings:PaymentPagesBaseUrl is not set, so RentAll's payment pages have no address.");
            return baseUrl + "/" + url.TrimStart('/').TrimEnd('/');
        }

        return (Resolve(page.RequestPageUrl), Resolve(page.ResponsePageUrl));
    }

    /// <summary>
    /// Starts an Number card form for the reservation: $0 today plus a subscription block (the only way Number's PFS
    /// form creates a saved card); the subscription is put on hold as soon as the consent comes back. The payer is
    /// returned to redirectUrl with ?TXID=..&amp;CID=.. added.
    /// </summary>
    private async Task<string> CreateNumberCardFormUrlAsync(Organization organization, Reservation reservation, string redirectUrl, CancellationToken cancellationToken)
    {
        var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, reservation.OrganizationId);
        var referenceNo = office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode;
        if (string.IsNullOrWhiteSpace(referenceNo))
            throw new InvalidOperationException(office?.UseExternalRefNo == true ? "This reservation does not have an external reference number." : "Reservation code is required.");

        var contactId = reservation.ContactIds.FirstOrDefault();
        var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
        if (contact != null && contact.OrganizationId != reservation.OrganizationId)
            contact = null;

        var payerName = PayerName(contact, reservation);
        var payForm = new NumberPayForm
        {
            OrganizationId = reservation.OrganizationId,
            OfficeId = reservation.OfficeId,
            RedirectUrl = redirectUrl,
            ReferenceId = referenceNo.Trim(),
            ReferenceGuid = reservation.ReservationId,
            Amount = 0,
            FirstName = payerName.First,
            LastName = payerName.Last,
            Email = contact?.Email?.Trim() ?? string.Empty,
            Phone = contact?.Phone?.Trim() ?? string.Empty,
            StreetAddress = contact?.Address1?.Trim() ?? string.Empty,
            City = contact?.City?.Trim() ?? string.Empty,
            State = contact?.State?.Trim() ?? string.Empty,
            Zip = contact?.Zip?.Trim() ?? string.Empty,
            // Never bills: held as soon as the consent arrives. A real amount is required; $0 makes the form do nothing.
            Subscription = new NumberSubscription { Amount = 20m, Period = "WEEKLY", FirstPayDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)) }
        };
        return await WithNumberSessionAsync(organization, reservation.OfficeId,
            session => _numberService.CreatePfsFormUrlAsync(session, payForm, cancellationToken), cancellationToken);
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
            _logger.LogWarning("[EasyPay SessionRejected] OfficeId={OfficeId} ErrCode={ErrCode}; authenticating again", officeId, ex.ErrCode);
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
