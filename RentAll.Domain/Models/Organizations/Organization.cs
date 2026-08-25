using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Organization
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OrganizationType OrganizationType { get; set; }
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsInternational { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public string? SendGridName { get; set; }
    public string? SuffixKeyName { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public string? GetKeyVaultSecretName(string prefix)
    {
        return GetKeyVaultSecretName(prefix, SuffixKeyName);
    }

    public string? GetKeyVaultSecretName(string prefix, string? suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            return null;

        return prefix + suffix.Trim();
    }

    public string? GetSendGridKeyVaultSecretName() => GetKeyVaultSecretName("sendgrid-api-key--");

    public string? GetExternalPropertyKeyVaultSecretName() => GetKeyVaultSecretName("external-property-api-key--");

    public string? GetExternalLeadRentalKeyVaultSecretName() => GetKeyVaultSecretName("external-lead-rental-api-key--");

    public string? GetExternalLeadOwnerKeyVaultSecretName() => GetKeyVaultSecretName("external-lead-owner-api-key--");

    public string? GetExternalLeadGeneralKeyVaultSecretName() => GetKeyVaultSecretName("external-lead-general-api-key--");

    public string? GetExternalTicketKeyVaultSecretName() => GetKeyVaultSecretName("external-ticket-api-key--");
}
