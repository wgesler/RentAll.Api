namespace RentAll.Domain.Enums;

/// <summary>
/// Journal entry perspective. Tenant/Owner/Company ids match ReceiptType and WorkOrderType.
/// </summary>
public enum Perspective
{
    Tenant = 0,
    Owner = 1,
    Company = 2,
    System = 3
}
