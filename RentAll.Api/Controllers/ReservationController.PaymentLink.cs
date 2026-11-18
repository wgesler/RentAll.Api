namespace RentAll.Api.Controllers;

using RentAll.Domain.Models.Accounting;

public partial class ReservationController
{
    [HttpPost("{reservationId:guid}/payment-link")]
    public async Task<IActionResult> CreateReservationPaymentLink(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            return BadRequest("ReservationId is required");

        Reservation? reservation = null;
        try
        {
            reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, CurrentOrganizationId);
            if (reservation == null)
                return NotFound("Reservation not found");

            var office = await _organizationRepository.GetOfficeByIdAsync(reservation.OfficeId, CurrentOrganizationId);
            var referenceNo = office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode;
            if (string.IsNullOrWhiteSpace(referenceNo))
                return BadRequest(office?.UseExternalRefNo == true ? "This reservation does not have an external reference number." : "Reservation code is required.");

            var organization = await _organizationRepository.GetOrganizationByIdAsync(CurrentOrganizationId);
            if (organization == null)
                return NotFound("Organization not found");

            var contactId = reservation.ContactIds.FirstOrDefault();
            var contact = contactId == Guid.Empty ? null : await _contactRepository.GetContactByIdAsync(contactId);
            if (contact != null && contact.OrganizationId != CurrentOrganizationId)
                contact = null;

            var payerName = PayerName(contact, reservation);
            var amount = await GetUnpaidInvoiceAmountAsync(reservation);
            if (amount <= 0)
                amount = 20m;

            var session = await GetStoredNumberSessionAsync(organization, reservation.OfficeId, HttpContext.RequestAborted);
            var payForm = new NumberPayForm
            {
                OrganizationId = reservation.OrganizationId,
                OfficeId = reservation.OfficeId,
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
            var paymentUrl = await _numberService.CreatePayFormUrlAsync(session, payForm, HttpContext.RequestAborted);
            return Ok(new ReservationPaymentLinkResponseDto(paymentUrl));
        }
        catch (InvalidOperationException ex)
        {
            LogApplicationError(ex, "CreatePaymentLink", reservation?.OfficeId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogApplicationError(ex, "CreatePaymentLink", reservation?.OfficeId);
            return ServerError("An error occurred while creating the payment link");
        }
    }

    private async Task<NumberSession> GetStoredNumberSessionAsync(Organization organization, int officeId, CancellationToken cancellationToken)
    {
        var credential = await _accountingRepository.GetNumberCredentialByOfficeIdAsync(organization.OrganizationId, officeId);
        if (credential == null)
        {
            var created = await _numberService.CreateSessionAsync(organization.GetNumberAcctCodeKeyVaultSecretName(), organization.GetNumberTokenKeyVaultSecretName(), cancellationToken);
            await _accountingRepository.AddNumberCredentialAsync(new NumberCredential
            {
                OrganizationId = organization.OrganizationId,
                OfficeId = officeId,
                SessKey = created.SessKey,
                MerchId = created.MerchId,
                ExpiresOn = created.ExpiresOn
            });
            credential = await _accountingRepository.GetNumberCredentialByOfficeIdAsync(organization.OrganizationId, officeId);
        }

        if (credential == null || string.IsNullOrWhiteSpace(credential.SessKey))
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
