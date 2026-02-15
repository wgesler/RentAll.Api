using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Emails;

public class CreateEmailDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public Guid ReservationId { get; set; }
	public string FromEmail { get; set; } = string.Empty;
	public string FromName { get; set; } = string.Empty;
	public string ToEmail { get; set; } = string.Empty;
	public string ToName { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string PlainTextContent { get; set; } = string.Empty;
	public string HtmlContent { get; set; } = string.Empty;
    public FileDetails? FileDetails { get; set; }


    public (bool IsValid, string? ErrorMessage) IsValid(Guid organization, string officeAccess)
	{
		if (OrganizationId == Guid.Empty || OrganizationId != organization)
			return (false, "OrganizationId not valid");

		var officeIds = (officeAccess ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(value => int.TryParse(value, out var officeId) ? officeId : -1)
			.Where(officeId => officeId > 0)
			.ToHashSet();
		if (!officeIds.Contains(OfficeId))
			return (false, "OfficeId not valid");

		if (ReservationId == Guid.Empty)
			return (false, "ReservationId is required");

		if (string.IsNullOrWhiteSpace(ToEmail))
			return (false, "ToEmail is required");

		if (string.IsNullOrWhiteSpace(ToName))
			return (false, "ToName is required");

		if (string.IsNullOrWhiteSpace(FromEmail))
			return (false, "FromEmail is required");

		if (string.IsNullOrWhiteSpace(FromName))
			return (false, "FromName is required");

		if (string.IsNullOrWhiteSpace(Subject))
			return (false, "Subject is required");

		if (string.IsNullOrWhiteSpace(PlainTextContent) && string.IsNullOrWhiteSpace(HtmlContent))
			return (false, "Either PlainTextContent or HtmlContent is required");

		return (true, null);
	}

	public Email ToModel(Guid currentUser)
	{
		return new Email
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			ReservationId = ReservationId,
			FromEmail = FromEmail,
			FromName = FromName,
			ToEmail = ToEmail,
			ToName = ToName,
			Subject = Subject,
			PlainTextContent = PlainTextContent,
			HtmlContent = HtmlContent,
			FileDetails = FileDetails,
			EmailStatus = EmailStatus.Attempting,
			CreatedBy = currentUser
		};
	}
}
