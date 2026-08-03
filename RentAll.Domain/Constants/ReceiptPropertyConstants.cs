namespace RentAll.Domain.Constants;

public static class ReceiptPropertyConstants
{
    /// <summary>
    /// Persisted in Receipt.Properties when the user selects Company at the receipt level.
    /// </summary>
    public static readonly Guid CompanyPropertyId = Guid.Empty;

    public static bool IsCompanyPropertyId(Guid propertyId) =>
        propertyId == CompanyPropertyId;
}
