using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyRepository
{
    // Property Creates
    Task<Property> CreateAsync(Property property);

    // Property Selects
    Task<IEnumerable<PropertyList>> GetListByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<PropertyList>> GetListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess);
    Task<IEnumerable<PropertyList>> GetListByOwnerIdAsync(Guid ownerId, Guid organizationId, string officeAccess);

    Task<Property?> GetByIdAsync(Guid propertyId, Guid organizationId);
    Task<Property?> GetByPropertyCodeAsync(string propertyCode, Guid organizationId);
    Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId);

    // Property Updates
    Task<Property> UpdateByIdAsync(Property property);

    // Property Deletes
    Task DeleteByIdAsync(Guid propertyId);

    // PropertyHtml Creates
    Task<PropertyHtml> CreatePropertyHtmlAsync(PropertyHtml propertyHtml);

    // PropertyHtml Selects
    Task<PropertyHtml?> GetPropertyHtmlByPropertyIdAsync(Guid propertyId, Guid organizationId);

    // PropertyHtml Updates
    Task<PropertyHtml> UpdatePropertyHtmlByIdAsync(PropertyHtml propertyHtml);

    // PropertyHtml Deletes
    Task DeletePropertyHtmlByPropertyIdAsync(Guid propertyId);

    // PropertyLetter Creates
    Task<PropertyLetter> CreatePropertyLetterAsync(PropertyLetter propertyLetter);

    // PropertyLetter Selects
    Task<PropertyLetter?> GetPropertyLetterByPropertyIdAsync(Guid propertyId, Guid organizationId);

    // PropertyLetter Updates
    Task<PropertyLetter> UpdatePropertyLetterByIdAsync(PropertyLetter propertyLetter);

    // PropertyLetter Deletes
    Task DeletePropertyLetterByPropertyIdAsync(Guid propertyId);

    // PropertySelection Selects
    Task<PropertySelection?> GetPropertySelectionByUserIdAsync(Guid userId);

    // PropertySelection Upserts
    Task<PropertySelection> UpsertPropertySelectionAsync(PropertySelection selection);
}





