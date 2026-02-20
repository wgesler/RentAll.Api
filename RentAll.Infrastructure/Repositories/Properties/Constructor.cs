using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Enums;
using RentAll.Infrastructure.Entities;
using RentAll.Domain.Models;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        private readonly string _dbConnectionString;

        public PropertyRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private Property ConvertEntityToModel(PropertyEntity e)
        {
            var response = new Property()
            {
                PropertyId = e.PropertyId,
                OrganizationId = e.OrganizationId,
                PropertyCode = e.PropertyCode,
                Owner1Id = e.Owner1Id,
                Owner2Id = e.Owner2Id,
                Owner3Id = e.Owner3Id,
                AvailableFrom = e.AvailableFrom,
                AvailableUntil = e.AvailableUntil,
                MinStay = e.MinStay,
                MaxStay = e.MaxStay,
                CheckInTime = (CheckInTime)e.CheckInTimeId,
                CheckOutTime = (CheckOutTime)e.CheckOutTimeId,
                PropertyStyle = (PropertyStyle)e.PropertyStyleId,
                PropertyType = (PropertyType)e.PropertyTypeId,
                PropertyStatus = (PropertyStatus)e.PropertyStatusId,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                BuildingId = e.BuildingId,
                RegionId = e.RegionId,
                AreaId = e.AreaId,
                MonthlyRate = e.MonthlyRate,
                DailyRate = e.DailyRate,
                DepartureFee = e.DepartureFee,
                MaidServiceFee = e.MaidServiceFee,
                PetFee = e.PetFee,
                ExtraFee = e.ExtraFee,
                ExtraFeeName = e.ExtraFeeName,
                Bedrooms = e.Bedrooms,
                Bathrooms = e.Bathrooms,
                Accomodates = e.Accomodates,
                SquareFeet = e.SquareFeet,
                Bedroom1 = (BedSizeType)e.BedroomId1,
                Bedroom2 = (BedSizeType)e.BedroomId2,
                Bedroom3 = (BedSizeType)e.BedroomId3,
                Bedroom4 = (BedSizeType)e.BedroomId4,
                Address1 = e.Address1,
                Address2 = e.Address2,
                Suite = e.Suite,
                City = e.City,
                State = e.State,
                Zip = e.Zip,
                Phone = e.Phone,
                Neighborhood = e.Neighborhood,
                CrossStreet = e.CrossStreet,
                View = e.View,
                Mailbox = e.Mailbox,
                Unfurnished = e.Unfurnished,
                Heating = e.Heating,
                Ac = e.Ac,
                Elevator = e.Elevator,
                Security = e.Security,
                Gated = e.Gated,
                PetsAllowed = e.PetsAllowed,
                DogsOkay = e.DogsOkay,
                CatsOkay = e.CatsOkay,
                PoundLimit = e.PoundLimit,
                Smoking = e.Smoking,
                Parking = e.Parking,
                ParkingNotes = e.ParkingNotes,
                Alarm = e.Alarm,
                AlarmCode = e.AlarmCode,
                KeypadAccess = e.KeypadAccess,
                MasterKeyCode = e.MasterKeyCode,
                TenantKeyCode = e.TenantKeyCode,
                Kitchen = e.Kitchen,
                Oven = e.Oven,
                Refrigerator = e.Refrigerator,
                Microwave = e.Microwave,
                Dishwasher = e.Dishwasher,
                Bathtub = e.Bathtub,
                WasherDryer = e.WasherDryer,
                Sofabeds = e.Sofabeds,
                Tv = e.Tv,
                Cable = e.Cable,
                Dvd = e.Dvd,
                Streaming = e.Streaming,
                FastInternet = e.FastInternet,
                InternetNetwork = e.InternetNetwork,
                InternetPassword = e.InternetPassword,
                Deck = e.Deck,
                Patio = e.Patio,
                Yard = e.Yard,
                Garden = e.Garden,
                CommonPool = e.CommonPool,
                PrivatePool = e.PrivatePool,
                Jacuzzi = e.Jacuzzi,
                Sauna = e.Sauna,
                Gym = e.Gym,
                TrashPickupId = e.TrashPickupId,
                TrashRemoval = e.TrashRemoval,
                Amenities = e.Amenities,
                Description = e.Description,
                Notes = e.Notes,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }

        private PropertyList ConvertEntityToModel(PropertyListEntity e)
        {
            return new PropertyList
            {
                PropertyId = e.PropertyId,
                PropertyCode = e.PropertyCode,
                ShortAddress = e.ShortAddress,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                Owner1Id = e.Owner1Id,
                OwnerName = e.OwnerName,
                Bedrooms = e.Bedrooms,
                Bathrooms = e.Bathrooms,
                Accomodates = e.Accomodates,
                SquareFeet = e.SquareFeet,
                MonthlyRate = e.MonthlyRate,
                DailyRate = e.DailyRate,
                DepartureFee = e.DepartureFee,
                PetFee = e.PetFee,
                MaidServiceFee = e.MaidServiceFee,
                PropertyStatus = (PropertyStatus)e.PropertyStatusId,
                IsActive = e.IsActive
            };
        }

        private PropertyHtml ConvertEntityToModel(PropertyHtmlEntity e)
        {
            var response = new PropertyHtml()
            {
                PropertyId = e.PropertyId,
                OrganizationId = e.OrganizationId,
                WelcomeLetter = e.WelcomeLetter,
                InspectionChecklist = e.InspectionChecklist,
                Lease = e.Lease,
                Invoice = e.Invoice,
                LetterOfResponsibility = e.LetterOfResponsibility,
                NoticeToVacate = e.NoticeToVacate,
                CreditAuthorization = e.CreditAuthorization,
                CreditApplicationBusiness = e.CreditApplicationBusiness,
                CreditApplicationIndividual = e.CreditApplicationIndividual,
                IsDeleted = e.IsDeleted,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }

        private PropertyLetter ConvertEntityToModel(PropertyLetterEntity e)
        {
            var response = new PropertyLetter()
            {
                PropertyId = e.PropertyId,
                OrganizationId = e.OrganizationId,
                ArrivalInstructions = e.ArrivalInstructions,
                MailboxInstructions = e.MailboxInstructions,
                PackageInstructions = e.PackageInstructions,
                ParkingInformation = e.ParkingInformation,
                Access = e.Access,
                Amenities = e.Amenities,
                Laundry = e.Laundry,
                ProvidedFurnishings = e.ProvidedFurnishings,
                Housekeeping = e.Housekeeping,
                TelevisionSource = e.TelevisionSource,
                InternetService = e.InternetService,
                KeyReturn = e.KeyReturn,
                Concierge = e.Concierge,
                MaintenanceEmail = e.MaintenanceEmail,
                EmergencyPhone = e.EmergencyPhone,
                AdditionalNotes = e.AdditionalNotes,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }

        private static PropertySelection ConvertEntityToModel(PropertySelectionEntity e)
        {
            return new PropertySelection
            {
                UserId = e.UserId,
                FromBeds = e.FromBeds,
                ToBeds = e.ToBeds,
                Accomodates = e.Accomodates,
                MaxRent = e.MaxRent,
                PropertyCode = e.PropertyCode,
                City = e.City,
                State = e.State,
                Unfurnished = e.Unfurnished,
                Cable = e.Cable,
                Streaming = e.Streaming,
                Pool = e.Pool,
                Jacuzzi = e.Jacuzzi,
                Security = e.Security,
                Parking = e.Parking,
                Pets = e.Pets,
                Smoking = e.Smoking,
                HighSpeedInternet = e.HighSpeedInternet,
                PropertyStatusId = e.PropertyStatusId,
                OfficeCode = e.OfficeCode,
                BuildingCode = e.BuildingCode,
                RegionCode = e.RegionCode,
                AreaCode = e.AreaCode
            };
        }
    }
}
