namespace RentAll.Domain.Enums;

public enum EntityType
{
	Unknown = 0,
	Organization = 1,
	Reservation = 2,
	Company = 3,
	Owner = 4,
    Tenant = 5,
	Vendor = 6,
    Hoa = 7
}

public static class EntityTypeExtensions
{
    private static readonly Dictionary<EntityType, string> EntityTypeCodes = new()
    {
		{ EntityType.Reservation, "R" },
		{ EntityType.Organization, "G" },
		{ EntityType.Company, "C" },
        { EntityType.Owner, "O" },
        { EntityType.Tenant, "T" },
		{ EntityType.Vendor, "V" },
		{ EntityType.Hoa, "H" }
	};

    private static readonly Dictionary<string, EntityType> CodeToEntityType = new()
    {
		{ "R", EntityType.Reservation },
		{ "G", EntityType.Organization },
		{ "C", EntityType.Company },
        { "O", EntityType.Owner },
        { "T", EntityType.Tenant },
		{ "V", EntityType.Vendor },
		{ "H", EntityType.Hoa }
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

