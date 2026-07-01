namespace RentAll.Api.Dtos.Contacts;

public class AppendPropertyCodeToContactsDto
{
    public List<Guid> ContactIds { get; set; } = new();
    public string PropertyCode { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ContactIds == null || ContactIds.Count == 0)
            return (false, "At least one contact ID is required");

        if (ContactIds.Any(contactId => contactId == Guid.Empty))
            return (false, "Contact IDs cannot contain empty values");

        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "PropertyCode is required");

        return (true, null);
    }
}
