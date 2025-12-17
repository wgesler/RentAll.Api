using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Domain.Managers;

public class OrganizationManager : IOrganizationManager
{
    private readonly Guid systemOrganizationId = Guid.Empty; 
	private readonly ICodeSequenceRepository _codeSequenceRepository;

    public OrganizationManager(
        ICodeSequenceRepository codeSequenceRepository)
    {
        _codeSequenceRepository = codeSequenceRepository;
    }

    public async Task<string> GenerateEntityCodeAsync()
    {
		EntityType entityType = EntityType.Organization;
		int entityTypeId = (int)entityType;

		var prefix = entityType.ToCode();
        int nextNumber = await _codeSequenceRepository.GetNextAsync(systemOrganizationId, entityTypeId, entityType.ToString());
        var code = $"{prefix}-{nextNumber:D6}";

        return code;
    }
	public async Task<string> GenerateEntityCodeAsync(Guid organizationId, EntityType entityType)
	{
		var prefix = entityType.ToCode();
		int nextNumber = await _codeSequenceRepository.GetNextAsync(organizationId, (int)entityType, entityType.ToString());
		var code = $"{prefix}-{nextNumber:D6}";

		return code;
	}
}

