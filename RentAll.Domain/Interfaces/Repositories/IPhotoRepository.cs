using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPhotoRepository
{
    Task<Photo> CreateAsync(Photo photo);
    Task<Photo?> GetByIdAsync(Guid photoId, Guid organizationId);
    Task DeleteByIdAsync(Guid photoId, Guid organizationId);
}
