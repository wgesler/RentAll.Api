namespace RentAll.Infrastructure.Entities;

public class DocumentEntity
{
	public Guid DocumentId { get; set; }
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public int DocumentTypeId { get; set; }
	public string FileName { get; set; } = string.Empty;
	public string FileExtension { get; set; } = string.Empty;
	public string ContentType { get; set; } = string.Empty;
	public string DocumentPath { get; set; } = string.Empty;
	public bool IsDeleted { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}


