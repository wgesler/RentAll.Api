using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>What the public pay-request page shows: who is asking, and for which reservation. It only saves a card on file; nothing is charged.</summary>
public class PayRequestResponseDto
{
    public string ReservationCode { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
    public bool IsCompleted { get; set; }
    public bool HasCardOnFile { get; set; }
    public bool CardAvailable { get; set; }
    public bool BankAvailable { get; set; }

    public PayRequestResponseDto(PaymentLink link, Reservation reservation, Organization organization, Office? office, string? logoUrl)
    {
        var referenceNo = office?.UseExternalRefNo == true ? reservation.ExternalRefNo : reservation.ReservationCode;
        ReservationCode = string.IsNullOrWhiteSpace(referenceNo) ? reservation.ReservationCode : referenceNo;
        OrganizationName = organization.Name ?? string.Empty;
        DisplayName = organization.DisplayName?.Trim() ?? string.Empty;
        OfficeName = office?.Name ?? string.Empty;
        LogoUrl = logoUrl;
        TenantName = (reservation.TenantName ?? reservation.ContactName ?? string.Empty).Trim();
        ExpiresOn = link.ExpiresOn;
        IsCompleted = link.CompletedOn != null;
        HasCardOnFile = reservation.NumberConsentId is > 0;
        CardAvailable = true;
        // ACH Debit: entered on our page and saved with Number's ACH/Combo (no debit).
        BankAvailable = true;
    }
}
