using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;
using System.Text.RegularExpressions;

namespace RentAll.Api.Dtos.Emails;

public class CreateEmailDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ReservationId { get; set; }
	public EmailAddress FromRecipient { get; set; } = new();
	public List<EmailAddress> ToRecipients { get; set; } = [];
	public List<EmailAddress> CcRecipients { get; set; } = [];
	public List<EmailAddress> BccRecipients { get; set; } = [];
	public string Subject { get; set; } = string.Empty;
	public string PlainTextContent { get; set; } = string.Empty;
	public string HtmlContent { get; set; } = string.Empty;
	public int EmailTypeId { get; set; }
	public FileDetails? FileDetails { get; set; }


    public (bool IsValid, string? ErrorMessage) IsValid(Guid organization, string officeAccess)
	{
		ToRecipients ??= [];
		CcRecipients ??= [];
		BccRecipients ??= [];
		FromRecipient ??= new EmailAddress();

		if (OrganizationId == Guid.Empty || OrganizationId != organization)
			return (false, "OrganizationId not valid");

		var officeIds = (officeAccess ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(value => int.TryParse(value, out var officeId) ? officeId : -1)
			.Where(officeId => officeId > 0)
			.ToHashSet();
		if (!officeIds.Contains(OfficeId))
			return (false, "OfficeId not valid");

		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (ReservationId == Guid.Empty)
			return (false, "ReservationId is required");

		if (ToRecipients.Count == 0)
			return (false, "At least one ToRecipient is required");

		if (string.IsNullOrWhiteSpace(FromRecipient.Email))
			return (false, "FromRecipient.Email is required");

		if (!ToRecipients.All(recipient => IsValidEmail(recipient.Email)))
			return (false, "One or more ToRecipients have invalid email addresses");

		if (!IsValidEmail(FromRecipient.Email))
			return (false, "FromRecipient.Email is not a valid email address");

		if (!CcRecipients.All(recipient => string.IsNullOrWhiteSpace(recipient.Email) || IsValidEmail(recipient.Email)))
			return (false, "One or more CcRecipients have invalid email addresses");

		if (!BccRecipients.All(recipient => string.IsNullOrWhiteSpace(recipient.Email) || IsValidEmail(recipient.Email)))
			return (false, "One or more BccRecipients have invalid email addresses");

		if (string.IsNullOrWhiteSpace(Subject))
			return (false, "Subject is required");

		if (string.IsNullOrWhiteSpace(PlainTextContent) && string.IsNullOrWhiteSpace(HtmlContent))
			return (false, "Either PlainTextContent or HtmlContent is required");

		if (!Enum.IsDefined(typeof(EmailType), EmailTypeId))
			return (false, $"Invalid EmailType value: {EmailTypeId}");

		return (true, null);
	}

	private static bool IsValidEmail(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return false;

		var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
		try
		{
			return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public Email ToModel(Guid currentUser)
	{
		return new Email
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			PropertyId = PropertyId,
			ReservationId = ReservationId,
			FromRecipient = new EmailAddress
			{
				Email = FromRecipient.Email,
				Name = FromRecipient.Name
			},
			ToRecipients = ToRecipients
				.Select(recipient => new EmailAddress
				{
					Email = recipient.Email,
					Name = recipient.Name
				})
				.ToList(),
			CcRecipients = CcRecipients
				.Select(recipient => new EmailAddress
				{
					Email = recipient.Email,
					Name = recipient.Name
				})
				.ToList(),
			BccRecipients = BccRecipients
				.Select(recipient => new EmailAddress
				{
					Email = recipient.Email,
					Name = recipient.Name
				})
				.ToList(),
			Subject = Subject,
			PlainTextContent = PlainTextContent,
			HtmlContent = HtmlContent,
			EmailType = (EmailType)EmailTypeId,
			FileDetails = FileDetails,
			EmailStatus = EmailStatus.Attempting,
			CreatedBy = currentUser
		};
	}
}
