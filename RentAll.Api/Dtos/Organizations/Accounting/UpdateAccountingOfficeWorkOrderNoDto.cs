namespace RentAll.Api.Dtos.Organizations.Accounting;

public class UpdateAccountingOfficeWorkOrderNoDto
{
    public int WorkOrderNo { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (WorkOrderNo < 0)
            return (false, "WorkOrderNo cannot be negative");

        return (true, null);
    }
}
