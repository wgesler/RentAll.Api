using RentAll.Domain.Models.ESignature;
using System.Text.RegularExpressions;

namespace RentAll.Api.Dtos.ESignature;

public class SendDocumentForSignatureDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public int DocumentTypeId { get; set; }
    public string HtmlContent { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public List<DocuSignSignerDto> Signers { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organizationId, string officeAccess)
    {
        Signers ??= [];

        if (OrganizationId == Guid.Empty || OrganizationId != organizationId)
            return (false, "OrganizationId not valid");

        var officeIds = (officeAccess ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : -1)
            .Where(officeId => officeId > 0)
            .ToHashSet();
        if (!officeIds.Contains(OfficeId))
            return (false, "OfficeId not valid");

        if (string.IsNullOrWhiteSpace(HtmlContent))
            return (false, "HTML content is required");

        if (!Enum.IsDefined(typeof(DocumentType), DocumentTypeId))
            return (false, $"Invalid DocumentType value: {DocumentTypeId}");

        if (DocumentTypeId is not ((int)DocumentType.ReservationLeases) and not ((int)DocumentType.Agreements))
            return (false, "Document type is not supported for DocuSign.");

        if (string.IsNullOrWhiteSpace(Subject))
            return (false, "Subject is required");

        if (string.IsNullOrWhiteSpace(ReturnUrl) || !Uri.TryCreate(ReturnUrl, UriKind.Absolute, out var returnUri)
            || (returnUri.Scheme != Uri.UriSchemeHttp && returnUri.Scheme != Uri.UriSchemeHttps))
            return (false, "A valid return URL is required");

        if (string.IsNullOrWhiteSpace(SenderEmail) || !IsValidEmail(SenderEmail))
            return (false, "Sender email is required");

        if (string.IsNullOrWhiteSpace(SenderName))
            return (false, "Sender name is required");

        if (Signers.Count == 0)
            return (false, "At least one signer is required");

        foreach (var signer in Signers)
        {
            if (string.IsNullOrWhiteSpace(signer.Email) || !IsValidEmail(signer.Email))
                return (false, "One or more signers have invalid email addresses");

            if (string.IsNullOrWhiteSpace(signer.Name))
                return (false, "Signer name is required");

            if (signer.RoutingOrder <= 0)
                return (false, "Signer routing order must be greater than zero");
        }

        return (true, null);
    }

    public IReadOnlyList<DocuSignSigner> ToSigners()
    {
        return Signers
            .Select(signer => new DocuSignSigner
            {
                Email = signer.Email.Trim(),
                Name = signer.Name.Trim(),
                RoutingOrder = signer.RoutingOrder
            })
            .ToList();
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        const string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
    }
}

public class DocuSignSignerDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RoutingOrder { get; set; } = 1;
}
