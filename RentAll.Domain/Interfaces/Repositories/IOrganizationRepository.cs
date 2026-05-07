using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
    #region Organization
    Task<IEnumerable<Organization>> GetOrganizationsAsync();
    Task<Organization?> GetOrganizationByIdAsync(Guid organizationId);
    Task<Organization?> GetByOrganizationCodeAsync(string organizationCode);
    Task<bool> ExistsByOrganizationCodeAsync(string organizationCode);

    Task<Organization> CreateAsync(Organization organization);
    Task<Organization> UpdateByIdAsync(Organization organization);
    Task DeleteOrganizationByIdAsync(Guid organizationId);
    #endregion

    #region Office
    Task<IEnumerable<Office>> GetOfficesByOrganizationIdAsync(Guid organizationId);
    Task<IEnumerable<Office>> GetOfficesByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Office?> GetOfficeByIdAsync(int officeId, Guid organizationId);
    Task<Office?> GetOfficeByOfficeCodeAsync(string officeCode, Guid organizationId);
    Task<bool> ExistsByOfficeCodeAsync(string officeCode, Guid organizationId);

    Task<Office> CreateAsync(Office office);
    Task<Office> UpdateByIdAsync(Office office);
    Task DeleteOfficeByIdAsync(int officeId);
    #endregion

    #region Accounting Offices
    Task<IEnumerable<AccountingOffice>> GetAccountingOfficesByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<AccountingOffice?> GetAccountingOfficeByIdAsync(Guid organizationId, int officeId);

    Task<AccountingOffice> CreateAccountingAsync(AccountingOffice accountingOffice);
    Task<AccountingOffice> UpdateAccountingAsync(AccountingOffice accountingOffice);
    Task DeleteAccountingOfficeByIdAsync(Guid organizationId, int officeId);
    #endregion

    #region Agents
    Task<IEnumerable<Agent>> GetAgentsByOrganizationIdAsync(Guid organizationId);
    Task<Agent?> GetAgentByIdAsync(Guid agentId, Guid organizationId);
    Task<Agent?> GetAgentByCodeAsync(string agentCode, Guid organizationId);
    Task<bool> ExistsAgentByCodeAsync(string agentCode, Guid organizationId);

    Task<Agent> CreateAgentAsync(Agent agent);
    Task<Agent> UpdateAgentByIdAsync(Agent agent);
    Task DeleteAgentByIdAsync(Guid agentId);
    #endregion

    #region Areas
    Task<IEnumerable<Area>> GetAreasByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Area?> GetAreaByIdAsync(int areaId, Guid organizationId);
    Task<Area?> GetAreaByCodeAsync(string areaCode, Guid organizationId, int? officeId);
    Task<bool> ExistsAreaByCodeAsync(string areaCode, Guid organizationId, int? officeId);

    Task<Area> CreateAreaAsync(Area area);
    Task<Area> UpdateAreaByIdAsync(Area area);
    Task DeleteAreaByIdAsync(int areaId);
    #endregion

    #region Buildings
    Task<IEnumerable<Building>> GetBuildingsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Building?> GetBuildingByIdAsync(int buildingId, Guid organizationId);
    Task<Building?> GetBuildingByCodeAsync(string buildingCode, Guid organizationId, int? officeId);
    Task<bool> ExistsBuildingByCodeAsync(string buildingCode, Guid organizationId, int? officeId);

    Task<Building> CreateBuildingAsync(Building building);
    Task<Building> UpdateBuildingByIdAsync(Building building);
    Task DeleteBuildingByIdAsync(int buildingId);
    #endregion

    #region Colors
    Task<IEnumerable<Colour>> GetColorsByOrganizationIdAsync(Guid organizationId);
    Task<Colour?> GetColorByIdAsync(int colorId, Guid organizationId);
    Task UpdateColorByIdAsync(Colour color);
    #endregion

    #region Branding
    Task<Branding?> GetBrandingByOrganizationIdAsync(Guid organizationId);
    Task<Branding> UpsertBrandingByOrganizationIdAsync(Branding branding, Guid modifiedBy);
    #endregion

    #region Regions
    Task<IEnumerable<Region>> GetRegionsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Region?> GetRegionByIdAsync(int regionId, Guid organizationId);
    Task<Region?> GetRegionByCodeAsync(string regionCode, Guid organizationId, int? officeId);
    Task<bool> ExistsRegionByCodeAsync(string regionCode, Guid organizationId, int? officeId);

    Task<Region> CreateRegionAsync(Region region);
    Task<Region> UpdateRegionByIdAsync(Region region);
    Task DeleteRegionByIdAsync(int regionId);
    #endregion

    #region Tracker Configuration
    Task<IEnumerable<TrackerContext>> GetTrackerContextsAsync();
    Task<TrackerContext?> GetTrackerContextByIdAsync(int trackerContextId);
    Task<TrackerContext> CreateTrackerContextAsync(TrackerContext trackerContext);
    Task<TrackerContext> UpdateTrackerContextByIdAsync(TrackerContext trackerContext);
    Task DeleteTrackerContextByIdAsync(int trackerContextId);

    Task<IEnumerable<TrackerDefinition>> GetTrackerDefinitionsByOfficeIdsAsync(Guid organizationId, string officeAccess, int? trackerContextId, bool includeInactive);
    Task<TrackerDefinition?> GetTrackerDefinitionByIdAsync(Guid trackerDefinitionId, Guid organizationId);
    Task<TrackerDefinition> CreateTrackerDefinitionAsync(TrackerDefinition trackerDefinition);
    Task<TrackerDefinition> UpdateTrackerDefinitionByIdAsync(TrackerDefinition trackerDefinition);
    Task DeleteTrackerDefinitionByIdAsync(Guid trackerDefinitionId, Guid organizationId);

    Task<IEnumerable<TrackerDefinitionOption>> GetTrackerDefinitionOptionsByOfficeIdsAsync(Guid organizationId, string officeAccess, int? trackerContextId, bool includeInactive);
    Task<IEnumerable<TrackerDefinitionOption>> GetTrackerDefinitionOptionsByTrackerDefinitionIdAsync(Guid trackerDefinitionId, bool includeInactive);
    Task<TrackerDefinitionOption?> GetTrackerDefinitionOptionByIdAsync(Guid trackerDefinitionOptionId);
    Task<TrackerDefinitionOption> CreateTrackerDefinitionOptionAsync(TrackerDefinitionOption trackerDefinitionOption);
    Task<TrackerDefinitionOption> UpdateTrackerDefinitionOptionByIdAsync(TrackerDefinitionOption trackerDefinitionOption);
    Task DeleteTrackerDefinitionOptionByIdAsync(Guid trackerDefinitionOptionId);
    #endregion
}
