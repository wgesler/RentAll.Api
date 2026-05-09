using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Reservations
{
    public partial class ReservationRepository
    {
        #region Selects
        public async Task<LeaseInformation?> GetLeaseInformationByIdAsync(Guid leaseInformationId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("Property.LeaseInformation_GetById", new
            {
                LeaseInformationId = leaseInformationId,
                OrganizationId = organizationId,
                OfficeId = (int?)null,
                PropertyId = (Guid?)null
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<LeaseInformation?> GetLeaseInformationByPropertyIdAsync(Guid propertyId, Guid organizationId, int? officeId = null)
        {
            return await GetLeaseInformationByScopeAsync(organizationId, officeId, propertyId);
        }

        public async Task<LeaseInformation?> GetLeaseInformationByScopeAsync(Guid organizationId, int? officeId, Guid? propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("Property.LeaseInformation_GetByPropertyId", new
            {
                PropertyId = propertyId,
                OfficeId = officeId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Creates
        public async Task<LeaseInformation> CreateLeaseInformationAsync(LeaseInformation leaseInformation)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("Property.LeaseInformation_Add", new
            {
                PropertyId = leaseInformation.PropertyId,
                OfficeId = leaseInformation.OfficeId,
                OrganizationId = leaseInformation.OrganizationId,
                RentalPayment = leaseInformation.RentalPayment,
                SecurityDeposit = leaseInformation.SecurityDeposit,
                SecurityDepositWaiver = leaseInformation.SecurityDepositWaiver,
                CancellationPolicy = leaseInformation.CancellationPolicy,
                KeyPickUpDropOff = leaseInformation.KeyPickUpDropOff,
                PartialMonth = leaseInformation.PartialMonth,
                DepartureNotification = leaseInformation.DepartureNotification,
                Holdover = leaseInformation.Holdover,
                DepartureServiceFee = leaseInformation.DepartureServiceFee,
                CheckoutProcedure = leaseInformation.CheckoutProcedure,
                Parking = leaseInformation.Parking,
                RulesAndRegulations = leaseInformation.RulesAndRegulations,
                OccupyingTenants = leaseInformation.OccupyingTenants,
                UtilityAllowance = leaseInformation.UtilityAllowance,
                MaidService = leaseInformation.MaidService,
                Pets = leaseInformation.Pets,
                Smoking = leaseInformation.Smoking,
                Emergencies = leaseInformation.Emergencies,
                HomeownersAssociation = leaseInformation.HomeownersAssociation,
                Indemnification = leaseInformation.Indemnification,
                DefaultClause = leaseInformation.DefaultClause,
                AttorneyCollectionFees = leaseInformation.AttorneyCollectionFees,
                ReservedRights = leaseInformation.ReservedRights,
                PropertyUse = leaseInformation.PropertyUse,
                Miscellaneous = leaseInformation.Miscellaneous,
                CreatedBy = leaseInformation.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("LeaseInformation not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Updates
        public async Task<LeaseInformation> UpdateLeaseInformationByIdAsync(LeaseInformation leaseInformation)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("Property.LeaseInformation_UpdateById", new
            {
                PropertyId = leaseInformation.PropertyId,
                OfficeId = leaseInformation.OfficeId,
                OrganizationId = leaseInformation.OrganizationId,
                RentalPayment = leaseInformation.RentalPayment,
                SecurityDeposit = leaseInformation.SecurityDeposit,
                SecurityDepositWaiver = leaseInformation.SecurityDepositWaiver,
                CancellationPolicy = leaseInformation.CancellationPolicy,
                KeyPickUpDropOff = leaseInformation.KeyPickUpDropOff,
                PartialMonth = leaseInformation.PartialMonth,
                DepartureNotification = leaseInformation.DepartureNotification,
                Holdover = leaseInformation.Holdover,
                DepartureServiceFee = leaseInformation.DepartureServiceFee,
                CheckoutProcedure = leaseInformation.CheckoutProcedure,
                Parking = leaseInformation.Parking,
                RulesAndRegulations = leaseInformation.RulesAndRegulations,
                OccupyingTenants = leaseInformation.OccupyingTenants,
                UtilityAllowance = leaseInformation.UtilityAllowance,
                MaidService = leaseInformation.MaidService,
                Pets = leaseInformation.Pets,
                Smoking = leaseInformation.Smoking,
                Emergencies = leaseInformation.Emergencies,
                HomeownersAssociation = leaseInformation.HomeownersAssociation,
                Indemnification = leaseInformation.Indemnification,
                DefaultClause = leaseInformation.DefaultClause,
                AttorneyCollectionFees = leaseInformation.AttorneyCollectionFees,
                ReservedRights = leaseInformation.ReservedRights,
                PropertyUse = leaseInformation.PropertyUse,
                Miscellaneous = leaseInformation.Miscellaneous,
                ModifiedBy = leaseInformation.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("LeaseInformation not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeleteLeaseInformationByIdAsync(Guid leaseInformationId, Guid organizationId, Guid modifiedBy)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.LeaseInformation_DeleteById", new
            {
                LeaseInformationId = leaseInformationId,
                OrganizationId = organizationId,
                ModifiedBy = modifiedBy
            });
        }
        #endregion
    }
}
