namespace RentAll.Domain.Interfaces.Repositories;

public interface ICodeSequenceRepository
{
    Task<int> GetNextAsync(Guid organizationId, int entityTypeId, string entityType);
}




