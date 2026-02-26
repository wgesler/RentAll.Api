using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Create
        public async Task<PropertyLetter> CreatePropertyLetterAsync(PropertyLetter propertyLetter)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyLetterEntity>("Property.PropertyInformation_Add", new
            {
                PropertyId = propertyLetter.PropertyId,
                OrganizationId = propertyLetter.OrganizationId,
                ArrivalInstructions = propertyLetter.ArrivalInstructions,
                MailboxInstructions = propertyLetter.MailboxInstructions,
                PackageInstructions = propertyLetter.PackageInstructions,
                ParkingInformation = propertyLetter.ParkingInformation,
                Access = propertyLetter.Access,
                Amenities = propertyLetter.Amenities,
                Laundry = propertyLetter.Laundry,
                ProvidedFurnishings = propertyLetter.ProvidedFurnishings,
                Housekeeping = propertyLetter.Housekeeping,
                TelevisionSource = propertyLetter.TelevisionSource,
                InternetService = propertyLetter.InternetService,
                KeyReturn = propertyLetter.KeyReturn,
                Concierge = propertyLetter.Concierge,
                MaintenanceEmail = propertyLetter.MaintenanceEmail,
                EmergencyPhone = propertyLetter.EmergencyPhone,
                AdditionalNotes = propertyLetter.AdditionalNotes,
                CreatedBy = propertyLetter.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("PropertyLetter not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Select
        public async Task<PropertyLetter?> GetPropertyLetterByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyLetterEntity>("Property.PropertyInformation_GetByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Update
        public async Task<PropertyLetter> UpdatePropertyLetterByIdAsync(PropertyLetter propertyLetter)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyLetterEntity>("Property.PropertyInformation_UpdateByPropertyId", new
            {
                PropertyId = propertyLetter.PropertyId,
                OrganizationId = propertyLetter.OrganizationId,
                ArrivalInstructions = propertyLetter.ArrivalInstructions,
                MailboxInstructions = propertyLetter.MailboxInstructions,
                PackageInstructions = propertyLetter.PackageInstructions,
                ParkingInformation = propertyLetter.ParkingInformation,
                Access = propertyLetter.Access,
                Amenities = propertyLetter.Amenities,
                Laundry = propertyLetter.Laundry,
                ProvidedFurnishings = propertyLetter.ProvidedFurnishings,
                Housekeeping = propertyLetter.Housekeeping,
                TelevisionSource = propertyLetter.TelevisionSource,
                InternetService = propertyLetter.InternetService,
                KeyReturn = propertyLetter.KeyReturn,
                Concierge = propertyLetter.Concierge,
                MaintenanceEmail = propertyLetter.MaintenanceEmail,
                EmergencyPhone = propertyLetter.EmergencyPhone,
                AdditionalNotes = propertyLetter.AdditionalNotes,
                ModifiedBy = propertyLetter.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("PropertyLetter not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Delete
        public async Task DeletePropertyLetterByPropertyIdAsync(Guid propertyId)
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
