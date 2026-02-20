namespace RentAll.Domain.Interfaces.Managers
{
    public interface IContactManager
    {
        Task<string> GenerateContactCodeAsync(Guid organizationId, int contactTypeId);
    }
}
