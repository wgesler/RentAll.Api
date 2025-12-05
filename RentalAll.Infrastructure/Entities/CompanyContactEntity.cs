namespace RentAll.Infrastructure.Entities;

public class CompanyContactEntity
{
    public Guid ContactId { get; set; }
    public Guid CompanyId { get; set; }
    public int IsActive { get; set; } = 1;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}



