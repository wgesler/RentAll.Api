using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Selects
        public async Task<PropertyInformation?> GetPropertyInformationByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyInformationEntity>("Property.PropertyInformation_GetByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Creates
        public async Task<PropertyInformation> CreatePropertyInformationAsync(PropertyInformation propertyInformation)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyInformationEntity>("Property.PropertyInformation_Add", new
            {
                PropertyId = propertyInformation.PropertyId,
                OrganizationId = propertyInformation.OrganizationId,
                ArrivalInstructions = propertyInformation.ArrivalInstructions,
                MailboxInstructions = propertyInformation.MailboxInstructions,
                PackageInstructions = propertyInformation.PackageInstructions,
                ParkingInformation = propertyInformation.ParkingInformation,
                Access = propertyInformation.Access,
                Laundry = propertyInformation.Laundry,
                ProvidedFurnishings = propertyInformation.ProvidedFurnishings,
                Housekeeping = propertyInformation.Housekeeping,
                TelevisionSource = propertyInformation.TelevisionSource,
                InternetService = propertyInformation.InternetService,
                KeyReturn = propertyInformation.KeyReturn,
                Concierge = propertyInformation.Concierge,
                MaintenanceEmail = propertyInformation.MaintenanceEmail,
                EmergencyPhone = propertyInformation.EmergencyPhone,
                AdditionalNotes = propertyInformation.AdditionalNotes,
                CreatedBy = propertyInformation.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("PropertyInformation not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Updates
        public async Task<PropertyInformation> UpdatePropertyInformationByIdAsync(PropertyInformation propertyInformation)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyInformationEntity>("Property.PropertyInformation_UpdateByPropertyId", new
            {
                PropertyId = propertyInformation.PropertyId,
                OrganizationId = propertyInformation.OrganizationId,
                ArrivalInstructions = propertyInformation.ArrivalInstructions,
                MailboxInstructions = propertyInformation.MailboxInstructions,
                PackageInstructions = propertyInformation.PackageInstructions,
                ParkingInformation = propertyInformation.ParkingInformation,
                Access = propertyInformation.Access,
                Laundry = propertyInformation.Laundry,
                ProvidedFurnishings = propertyInformation.ProvidedFurnishings,
                Housekeeping = propertyInformation.Housekeeping,
                TelevisionSource = propertyInformation.TelevisionSource,
                InternetService = propertyInformation.InternetService,
                KeyReturn = propertyInformation.KeyReturn,
                Concierge = propertyInformation.Concierge,
                MaintenanceEmail = propertyInformation.MaintenanceEmail,
                EmergencyPhone = propertyInformation.EmergencyPhone,
                AdditionalNotes = propertyInformation.AdditionalNotes,
                ModifiedBy = propertyInformation.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("PropertyInformation not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeletePropertyInformationByPropertyIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.PropertyInformation_DeleteByPropertyId", new
            {
                PropertyId = propertyId
            });
        }
        #endregion
    }
}
