using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
    // Organization Creates
    Task<Organization> CreateAsync(Organization organization);

    // Organization Selects
    Task<IEnumerable<Organization>> GetAllAsync();
    Task<Organization?> GetByIdAsync(Guid organizationId);
    Task<Organization?> GetByOrganizationCodeAsync(string organizationCode);
    Task<bool> ExistsByOrganizationCodeAsync(string organizationCode);

    // Organization Updates
    Task<Organization> UpdateByIdAsync(Organization organization);
    Task<Organization> UpdateColorAsync(Guid organizationId, string rgb, Guid modifiedBy);

    // Organization Deletes
    Task DeleteByIdAsync(Guid organizationId);

    // Office Creates
    Task<Office> CreateAsync(Office office);

    // Office Selects
    Task<IEnumerable<Office>> GetAllAsync(Guid organizationId);
    Task<IEnumerable<Office>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Office?> GetByIdAsync(int officeId, Guid organizationId);
    Task<Office?> GetByOfficeCodeAsync(string officeCode, Guid organizationId);
    Task<bool> ExistsByOfficeCodeAsync(string officeCode, Guid organizationId);

    // Office Updates
    Task<Office> UpdateByIdAsync(Office office);

    // Office Deletes
    Task DeleteByIdAsync(int officeId);

    // Accounting Creates
    Task<AccountingOffice> CreateAccountingAsync(AccountingOffice accountingOffice);

    // Accounting Selects
    Task<IEnumerable<AccountingOffice>> GetAllAccountingByOfficeIdAsync(Guid organizationId, string officeIds);
    Task<AccountingOffice?> GetAccountingByIdAsync(Guid organizationId, int officeId);

    // Accounting Updates
    Task<AccountingOffice> UpdateAccountingAsync(AccountingOffice accountingOffice);

    // Accounting Deletes
    Task DeleteAccountingAsync(Guid organizationId, int officeId);

    // Agent Creates
    Task<Agent> CreateAgentAsync(Agent agent);

    // Agent Selects
    Task<IEnumerable<Agent>> GetAllAgentsAsync(Guid organizationId);
    Task<Agent?> GetAgentByIdAsync(Guid agentId, Guid organizationId);
    Task<Agent?> GetAgentByCodeAsync(string agentCode, Guid organizationId);
    Task<bool> ExistsAgentByCodeAsync(string agentCode, Guid organizationId);

    // Agent Updates
    Task<Agent> UpdateAgentByIdAsync(Agent agent);

    // Agent Deletes
    Task DeleteAgentByIdAsync(Guid agentId);

    // Area Creates
    Task<Area> CreateAreaAsync(Area area);

    // Area Selects
    Task<IEnumerable<Area>> GetAllAreasAsync(Guid organizationId);
    Task<IEnumerable<Area>> GetAllAreasByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Area?> GetAreaByIdAsync(int areaId, Guid organizationId);
    Task<Area?> GetAreaByCodeAsync(string areaCode, Guid organizationId, int? officeId);
    Task<bool> ExistsAreaByCodeAsync(string areaCode, Guid organizationId, int? officeId);

    // Area Updates
    Task<Area> UpdateAreaByIdAsync(Area area);

    // Area Deletes
    Task DeleteAreaByIdAsync(int areaId);

    // Building Creates
    Task<Building> CreateBuildingAsync(Building building);

    // Building Selects
    Task<IEnumerable<Building>> GetAllBuildingsAsync(Guid organizationId);
    Task<IEnumerable<Building>> GetAllBuildingsByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Building?> GetBuildingByIdAsync(int buildingId, Guid organizationId);
    Task<Building?> GetBuildingByCodeAsync(string buildingCode, Guid organizationId, int? officeId);
    Task<bool> ExistsBuildingByCodeAsync(string buildingCode, Guid organizationId, int? officeId);

    // Building Updates
    Task<Building> UpdateBuildingByIdAsync(Building building);

    // Building Deletes
    Task DeleteBuildingByIdAsync(int buildingId);

    // Color Selects
    Task<IEnumerable<Colour>> GetAllColorsAsync(Guid organizationId);
    Task<Colour?> GetColorByIdAsync(int colorId, Guid organizationId);

    // Color Updates
    Task UpdateColorByIdAsync(Colour color);

    // Region Creates
    Task<Region> CreateRegionAsync(Region region);

    // Region Selects
    Task<IEnumerable<Region>> GetAllRegionsAsync(Guid organizationId);
    Task<IEnumerable<Region>> GetAllRegionsByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Region?> GetRegionByIdAsync(int regionId, Guid organizationId);
    Task<Region?> GetRegionByCodeAsync(string regionCode, Guid organizationId, int? officeId);
    Task<bool> ExistsRegionByCodeAsync(string regionCode, Guid organizationId, int? officeId);

    // Region Updates
    Task<Region> UpdateRegionByIdAsync(Region region);

    // Region Deletes
    Task DeleteRegionByIdAsync(int regionId);
}


