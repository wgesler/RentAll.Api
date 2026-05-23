using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class UpsertPublicOwnerContactDto
{
    public int? OfficeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }

    public Contact ToNewOwnerContactModel(LeadOwner owner, string contactCode, int resolvedOfficeId, Guid createdBy)
    {
        return new Contact
        {
            OwnerLeadId = owner.OwnerId,
            OrganizationId = owner.OrganizationId,
            OfficeId = resolvedOfficeId,
            OfficeAccess = new List<int> { resolvedOfficeId },
            ContactCode = contactCode,
            EntityType = EntityType.Owner,
            OwnerType = OwnerType.Individual,
            Properties = new List<string>(),
            FirstName = ResolvePreferredValue(FirstName, owner.FirstName),
            LastName = ResolvePreferredValue(LastName, owner.LastName),
            Address1 = ResolvePreferredValue(Address1, owner.Address),
            Address2 = ResolvePreferredValue(Address2, string.Empty),
            City = ResolvePreferredValue(City, owner.City),
            State = ResolvePreferredValue(State, owner.State),
            Zip = ResolvePreferredValue(Zip, owner.Zip),
            Phone = ResolvePreferredValue(Phone, owner.Phone),
            Email = ResolveRequiredValue(Email, owner.Email),
            Rating = 0,
            IsInternational = false,
            Markup = 25,
            RevenueSplitOwner = 75,
            RevenueSplitOffice = 25,
            WorkingCapitalBalance = 0,
            LinenAndTowelFee = 0,
            IsActive = true,
            CreatedBy = createdBy
        };
    }

    public void ApplyToExistingOwnerContact(Contact contact, LeadOwner owner, int resolvedOfficeId, Guid modifiedBy)
    {
        contact.OwnerLeadId ??= owner.OwnerId;
        contact.OfficeId = resolvedOfficeId;
        contact.OfficeAccess = contact.OfficeAccess != null && contact.OfficeAccess.Count > 0
            ? contact.OfficeAccess.Distinct().ToList()
            : new List<int> { resolvedOfficeId };
        if (!contact.OfficeAccess.Contains(resolvedOfficeId))
            contact.OfficeAccess.Add(resolvedOfficeId);

        contact.FirstName = ResolveIncomingOrKeepExisting(FirstName, contact.FirstName, owner.FirstName);
        contact.LastName = ResolveIncomingOrKeepExisting(LastName, contact.LastName, owner.LastName);
        contact.Email = ResolveRequiredValue(Email, ResolveIncomingOrKeepExisting(null, contact.Email, owner.Email));
        contact.Phone = ResolveIncomingOrKeepExisting(Phone, contact.Phone, owner.Phone);
        contact.Address1 = ResolveIncomingOrKeepExisting(Address1, contact.Address1, owner.Address);
        if (!string.IsNullOrWhiteSpace(Address2))
            contact.Address2 = Address2!.Trim();
        contact.City = ResolveIncomingOrKeepExisting(City, contact.City, owner.City);
        contact.State = ResolveIncomingOrKeepExisting(State, contact.State, owner.State);
        contact.Zip = ResolveIncomingOrKeepExisting(Zip, contact.Zip, owner.Zip);
        contact.ModifiedBy = modifiedBy;
    }

    private static string ResolveRequiredValue(string? preferred, string? fallback)
    {
        var resolved = ResolvePreferredValue(preferred, fallback);
        return string.IsNullOrWhiteSpace(resolved) ? string.Empty : resolved;
    }

    private static string? ResolvePreferredValue(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
            return preferred.Trim();
        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback.Trim();
        return string.Empty;
    }

    private static string? ResolveIncomingOrKeepExisting(string? incoming, string? existing, string? fallbackWhenExistingEmpty)
    {
        if (!string.IsNullOrWhiteSpace(incoming))
            return incoming.Trim();
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;
        if (!string.IsNullOrWhiteSpace(fallbackWhenExistingEmpty))
            return fallbackWhenExistingEmpty.Trim();
        return existing;
    }
}
