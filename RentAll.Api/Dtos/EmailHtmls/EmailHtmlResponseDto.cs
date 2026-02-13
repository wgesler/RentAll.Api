using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.EmailHtmls;

public class EmailHtmlResponseDto
{
	public Guid OrganizationId { get; set; }
	public string WelcomeLetter { get; set; } = string.Empty;
	public string CorporateLetter { get; set; } = string.Empty;
	public string Lease { get; set; } = string.Empty;
	public string Invoice { get; set; } = string.Empty;
	public string LetterSubject { get; set; } = string.Empty;
	public string LeaseSubject { get; set; } = string.Empty;
	public string InvoiceSubject { get; set; } = string.Empty;
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

	public EmailHtmlResponseDto(EmailHtml emailHtml)
	{
		OrganizationId = emailHtml.OrganizationId;
		WelcomeLetter = emailHtml.WelcomeLetter;
		CorporateLetter = emailHtml.CorporateLetter;
		Lease = emailHtml.Lease;
		Invoice = emailHtml.Invoice;
		LetterSubject = emailHtml.LetterSubject;
		LeaseSubject = emailHtml.LeaseSubject;
		InvoiceSubject = emailHtml.InvoiceSubject;
		CreatedOn = emailHtml.CreatedOn;
		CreatedBy = emailHtml.CreatedBy;
		ModifiedOn = emailHtml.ModifiedOn;
		ModifiedBy = emailHtml.ModifiedBy;
	}
}
