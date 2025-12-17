using RentAll.Domain.Enums;

namespace RentAll.Domain.Interfaces.Managers
{
	public interface IOrganizationManager
	{
		Task<string> GenerateEntityCodeAsync();
		Task<string> GenerateEntityCodeAsync(Guid organizationId, EntityType entityType);
	}
}
