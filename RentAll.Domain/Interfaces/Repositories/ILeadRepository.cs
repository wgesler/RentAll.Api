using RentAll.Domain.Models.Leads;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILeadRepository
{
    #region Rental leads
    Task<IEnumerable<LeadRental>> GetRentalsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<LeadRental?> GetRentalByIdAsync(int rentalId);
    Task<LeadRental> CreateRentalAsync(LeadRental rental);
    Task<LeadRental> UpdateRentalByIdAsync(LeadRental rental);
    Task DeleteRentalByIdAsync(int rentalId);
    #endregion

    #region Owner leads
    Task<IEnumerable<LeadOwner>> GetOwnersByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<LeadOwner?> GetOwnerByIdAsync(int ownerId);
    Task<LeadOwner> CreateOwnerAsync(LeadOwner owner);
    Task<LeadOwner> UpdateOwnerByIdAsync(LeadOwner owner);
    Task DeleteOwnerByIdAsync(int ownerId);
    Task<LeadOwnerFormShare> UpsertOwnerFormShareByOwnerIdAsync(LeadOwnerFormShare share);
    Task<LeadOwnerFormShare?> GetOwnerFormShareByTokenHashAsync(string tokenHash);
    Task DeleteExpiredOwnerFormSharesAsync();
    Task<OwnerHtml?> GetOwnerHtmlByPropertyIdAsync(Guid propertyId, Guid organizationId);
    Task<OwnerAgreementInformation?> GetOwnerAgreementInformationByIdAsync(Guid ownerAgreementInformationId, Guid organizationId);
    Task<OwnerAgreementInformation?> GetOwnerAgreementInformationByScopeAsync(Guid organizationId, int? officeId, Guid? propertyId);
    Task<OwnerAgreementInformation?> GetOwnerAgreementInformationByExactScopeAsync(Guid organizationId, int? officeId, Guid? propertyId);
    Task<OwnerAgreementInformation> CreateOwnerAgreementInformationAsync(OwnerAgreementInformation ownerAgreementInformation);
    Task<OwnerAgreementInformation> UpdateOwnerAgreementInformationByIdAsync(OwnerAgreementInformation ownerAgreementInformation);
    Task DeleteOwnerAgreementInformationByIdAsync(Guid ownerAgreementInformationId, Guid organizationId, Guid modifiedBy);
    Task<OwnerInventoryInformation?> GetOwnerInventoryInformationByOwnerIdAsync(int ownerId, Guid organizationId);
    Task<OwnerInventoryInformation> CreateOwnerInventoryInformationAsync(OwnerInventoryInformation ownerInventoryInformation);
    Task<OwnerInventoryInformation> UpdateOwnerInventoryInformationByIdAsync(OwnerInventoryInformation ownerInventoryInformation);
    Task DeleteOwnerInventoryInformationByIdAsync(int ownerId, Guid organizationId, Guid modifiedBy);
    #endregion

    #region General leads
    Task<IEnumerable<LeadGeneral>> GetGeneralsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<LeadGeneral?> GetGeneralByIdAsync(int generalId);
    Task<LeadGeneral> CreateGeneralAsync(LeadGeneral lead);
    Task<LeadGeneral> UpdateGeneralByIdAsync(LeadGeneral lead);
    Task DeleteGeneralByIdAsync(int generalId);
    #endregion
}
