using RentAll.Domain.Models;
using RentAll.Domain.Models.Properties;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyRepository
{
    #region Properties
    Task<IEnumerable<PropertyList>> GetPropertyListByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<PropertyList>> GetPropertyActiveListByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<PropertyList>> GetPropertyListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess);
    Task<IEnumerable<PropertyList>> GetActivePropertyListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess);
    Task<IEnumerable<PropertyList>> GetPropertyListByOwnerIdAsync(Guid ownerId, Guid organizationId, string officeAccess);
    Task<Property?> GetPropertyByIdAsync(Guid propertyId, Guid organizationId);
    Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId);

    Task<Property> CreateAsync(Property property);
    Task<Property> UpdateByIdAsync(Property property);
    Task DeletePropertyByIdAsync(Guid propertyId);
    #endregion

    #region Property Html
    Task<PropertyHtml?> GetPropertyHtmlByPropertyIdAsync(Guid propertyId, Guid organizationId);
    Task<PropertyHtml> CreatePropertyHtmlAsync(PropertyHtml propertyHtml);
    Task<PropertyHtml> UpdatePropertyHtmlByIdAsync(PropertyHtml propertyHtml);
    Task DeletePropertyHtmlByPropertyIdAsync(Guid propertyId);
    #endregion

    #region Property Information
    Task<PropertyInformation?> GetPropertyInformationByPropertyIdAsync(Guid propertyId, Guid organizationId);
    Task<PropertyInformation> CreatePropertyInformationAsync(PropertyInformation propertyInformation);
    Task<PropertyInformation> UpdatePropertyInformationByIdAsync(PropertyInformation propertyInformation);
    Task DeletePropertyInformationByPropertyIdAsync(Guid propertyId);
    #endregion

    #region Property Selection
    Task<PropertySelection?> GetPropertySelectionByUserIdAsync(Guid userId);
    Task<PropertySelection> UpsertPropertySelectionAsync(PropertySelection selection);
    #endregion

    #region Property Agreement
    Task<PropertyAgreement?> GetPropertyAgreementByPropertyIdAsync(Guid propertyId);
    Task<PropertyAgreement> CreatePropertyAgreementAsync(PropertyAgreement agreement);
    Task<PropertyAgreement> UpdatePropertyAgreementByPropertyIdAsync(PropertyAgreement agreement);
    Task DeletePropertyAgreementByPropertyIdAsync(Guid propertyId);
    #endregion

    #region Property Photos
    Task<PropertyPhoto?> GetPropertyPhotoByIdAsync(int photoId, Guid organizationId);
    Task<IEnumerable<PropertyPhoto>> GetPropertyPhotosByPropertyIdAsync(Guid propertyId);
    Task<PropertyPhoto> CreatePropertyPhotoAsync(PropertyPhoto photo);
    Task UpdatePropertyPhotoOrderAsync(int photoId, int order);
    Task DeletePropertyPhotoByIdAsync(int photoId);
    #endregion

    #region Property Listing Share
    Task<PropertyListingShare> UpsertPropertyListingShareByPropertyIdAsync(PropertyListingShare share);
    Task<PropertyListingShare?> GetPropertyListingShareByTokenHashAsync(string tokenHash);
    Task RevokePropertyListingShareByPropertyIdAsync(Guid propertyId);
    Task DeleteExpiredPropertyListingSharesAsync();
    #endregion

    #region Tracker Responses
    Task<IEnumerable<TrackerResponse>> GetTrackerResponsesByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<TrackerResponseOption>> GetTrackerResponseOptionsByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<TrackerResponse>> GetTrackerResponsesByOfficeIdsAsync(Guid organizationId, string officeAccess, bool includeInactive = false, bool excludeCompletedPropertyTracking = true);
    Task<IEnumerable<TrackerResponseOption>> GetTrackerResponseOptionsByOfficeIdsAsync(Guid organizationId, string officeAccess, bool includeInactive = false, bool excludeCompletedPropertyTracking = true);
    Task<TrackerResponse?> GetTrackerResponseByIdAsync(Guid trackerResponseId);
    Task<TrackerResponse> CreateTrackerResponseAsync(TrackerResponse trackerResponse);
    Task<TrackerResponse> UpdateTrackerResponseByIdAsync(TrackerResponse trackerResponse);
    Task DeleteTrackerResponsesByPropertyIdAsync(Guid propertyId);
    Task DeleteTrackerResponseByIdAsync(Guid trackerResponseId);

    Task<TrackerResponseOption?> GetTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId);
    Task<TrackerResponseOption> CreateTrackerResponseOptionAsync(TrackerResponseOption trackerResponseOption);
    Task<TrackerResponseOption> UpdateTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId, Guid newTrackerDefinitionOptionId);
    Task DeleteTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId);
    #endregion
}
