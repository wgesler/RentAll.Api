namespace RentAll.Api.Dtos.Organizations.Accounting;

public class AccountingOfficeWorkOrderNoResponseDto
{
    public int OfficeId { get; set; }
    public int WorkOrderNo { get; set; }

    public AccountingOfficeWorkOrderNoResponseDto(int officeId, int workOrderNo)
    {
        OfficeId = officeId;
        WorkOrderNo = workOrderNo;
    }
}
