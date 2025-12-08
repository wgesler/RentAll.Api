namespace RentAll.Infrastructure.Entities;

public class PropertyContactEntity
{
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}