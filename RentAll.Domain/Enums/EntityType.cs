namespace RentAll.Domain.Enums;

public enum EntityType
{
	Unknown = 0,
	Organization = 1,
	Company = 2,
	Owner = 3,
    Tenant = 4,
	Vendor = 5
}

public static class EntityTypeExtensions
{
    private static readonly Dictionary<EntityType, string> EntityTypeCodes = new()
    {
		{ EntityType.Organization, "ORG" },
		{ EntityType.Company, "COM" },
        { EntityType.Owner, "OWN" },
        { EntityType.Tenant, "TEN" },
		{ EntityType.Vendor, "VEN" }
	};

    private static readonly Dictionary<string, EntityType> CodeToEntityType = new()
    {
		{ "ORG", EntityType.Organization },
		{ "COM", EntityType.Company },
        { "OWN", EntityType.Owner },
        { "TEN", EntityType.Tenant },
		{ "VEN", EntityType.Vendor }
	};

    public static string ToCode(this EntityType entityType)
    {
        return EntityTypeCodes.TryGetValue(entityType, out var code) ? code : string.Empty;
    }

    public static EntityType FromCode(string code)
    {
        return CodeToEntityType.TryGetValue(code?.ToUpper() ?? string.Empty, out var entityType) 
            ? entityType 
            : EntityType.Unknown;
    }
}

