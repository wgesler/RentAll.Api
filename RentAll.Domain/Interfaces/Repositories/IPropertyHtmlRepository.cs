using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyHtmlRepository
{
	// Creates
	Task<PropertyHtml> CreateAsync(PropertyHtml propertyHtml);

	// Selects
	Task<PropertyHtml?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId);

	// Updates
	Task<PropertyHtml> UpdateByIdAsync(PropertyHtml propertyHtml);

	// Deletes
	Task DeleteByPropertyIdAsync(Guid propertyId);
}


