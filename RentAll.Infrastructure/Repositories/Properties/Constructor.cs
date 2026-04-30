using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using System.Text.Json;

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
                PropertyLeaseType = (PropertyLeaseType)e.PropertyLeaseTypeId,
                Owner1Id = e.Owner1Id,
                Owner2Id = e.Owner2Id,
                Owner3Id = e.Owner3Id,
                VendorId = e.VendorId,
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
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                MonthlyRate = e.MonthlyRate,
                DailyRate = e.DailyRate,
                DepartureFee = e.DepartureFee,
                MaidServiceFee = e.MaidServiceFee,
                PetFee = e.PetFee,
                ExtraFee = e.ExtraFee,
                ExtraFeeName = e.ExtraFeeName,
                BldgNo = e.BldgNo,
                UnitLevel = e.UnitLevel,
                Bedrooms = e.Bedrooms,
                Bathrooms = e.Bathrooms,
                Accomodates = e.Accomodates,
                SquareFeet = e.SquareFeet,
                BedroomId1 = e.BedroomId1,
                BedroomId2 = e.BedroomId2,
                BedroomId3 = e.BedroomId3,
                BedroomId4 = e.BedroomId4,
                Sofabed = e.Sofabed,
                Address1 = e.Address1,
                Address2 = e.Address2,
                Suite = e.Suite,
                City = e.City,
                State = e.State,
                Zip = e.Zip,
                Phone = e.Phone,
                CommunityAddress = e.CommunityAddress,
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
                AlarmCode = e.AlarmCode,
                UnitMstrCode = e.UnitMstrCode,
                BldgMstrCode = e.BldgMstrCode,
                BldgTenantCode = e.BldgTenantCode,
                MailRoomCode = e.MailRoomCode,
                GarageCode = e.GarageCode,
                GateCode = e.GateCode,
                TrashCode = e.TrashCode,
                StorageCode = e.StorageCode,
                Kitchen = e.Kitchen,
                Oven = e.Oven,
                Refrigerator = e.Refrigerator,
                Microwave = e.Microwave,
                Dishwasher = e.Dishwasher,
                Bathtub = e.Bathtub,
                WasherDryerInUnit = e.WasherDryerInUnit,
                WasherDryerInBldg = e.WasherDryerInBldg,
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
                onCleanerUserId = e.onCleanerUserId,
                onCleaningDate = e.onCleaningDate,
                onCarpetUserId = e.onCarpetUserId,
                onCarpetDate = e.onCarpetDate,
                onInspectorUserId = e.onInspectorUserId,
                onInspectingDate = e.onInspectingDate,
                offCleanerUserId = e.offCleanerUserId,
                offCleaningDate = e.offCleaningDate,
                offCarpetUserId = e.offCarpetUserId,
                offCarpetDate = e.offCarpetDate,
                offInspectorUserId = e.offInspectorUserId,
                offInspectingDate = e.offInspectingDate,
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
                PropertyLeaseType = (PropertyLeaseType)e.PropertyLeaseTypeId,
                ShortAddress = e.ShortAddress,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                Owner1Id = e.Owner1Id,
                VendorId = e.VendorId,
                ContactName = e.ContactName,
                AvailableFrom = e.AvailableFrom,
                AvailableUntil = e.AvailableUntil,
                UnitLevel = e.UnitLevel,
                Bedrooms = e.Bedrooms,
                Bathrooms = e.Bathrooms,
                Accomodates = e.Accomodates,
                SquareFeet = e.SquareFeet,
                PropertyType = (PropertyType)e.PropertyTypeId,
                Unfurnished = e.Unfurnished,
                MonthlyRate = e.MonthlyRate,
                DailyRate = e.DailyRate,
                DepartureFee = e.DepartureFee,
                PetFee = e.PetFee,
                MaidServiceFee = e.MaidServiceFee,
                PropertyStatus = (PropertyStatus)e.PropertyStatusId,
                BedroomId1 = e.BedroomId1,
                BedroomId2 = e.BedroomId2,
                BedroomId3 = e.BedroomId3,
                BedroomId4 = e.BedroomId4,
                onCleanerUserId = e.onCleanerUserId,
                onCleaningDate = e.onCleaningDate,
                onCarpetUserId = e.onCarpetUserId,
                onCarpetDate = e.onCarpetDate,
                onInspectorUserId = e.onInspectorUserId,
                onInspectingDate = e.onInspectingDate,
                offCleanerUserId = e.offCleanerUserId,
                offCleaningDate = e.offCleaningDate,
                offCarpetUserId = e.offCarpetUserId,
                offCarpetDate = e.offCarpetDate,
                offInspectorUserId = e.offInspectorUserId,
                offInspectingDate = e.offInspectingDate,
                IsActive = e.IsActive
            };
        }

        private static PropertyAgreement ConvertEntityToModel(PropertyAgreementEntity e) => new()
        {
            PropertyId = e.PropertyId,
            OfficeId = e.OfficeId,
            ManagementFeeType = (ManagementFeeType)e.ManagementFeeTypeId,
            FlatRateAmount = e.FlatRateAmount,
            W9Path = e.W9Path,
            InsurancePath = e.InsurancePath,
            InsuranceExpiration = e.InsuranceExpiration,
            AgreementPath = e.AgreementPath,
            Markup = e.Markup,
            RevenueSplitOwner = e.RevenueSplitOwner,
            RevenueSplitOffice = e.RevenueSplitOffice,
            WorkingCapitalBalance = e.WorkingCapitalBalance,
            LinenAndTowelFee = e.LinenAndTowelFee,
            HourlyLaborCost = e.HourlyLaborCost,
            BankName = e.BankName,
            RoutingNumber = e.RoutingNumber,
            AccountNumber = e.AccountNumber,
            RentalIncomeCcId = e.RentalIncomeCcId,
            RentalExpenseCcId = e.RentalExpenseCcId,
            Notes = e.Notes
        };

        private PropertyHtml ConvertEntityToModel(PropertyHtmlEntity e)
        {
            var response = new PropertyHtml()
            {
                PropertyId = e.PropertyId,
                OrganizationId = e.OrganizationId,
                WelcomeLetter = e.WelcomeLetter,
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
            var buildingCodes = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.BuildingCodes))
            {
                try
                {
                    buildingCodes = JsonSerializer.Deserialize<List<string>>(e.BuildingCodes) ?? new List<string>();
                }
                catch
                {
                    buildingCodes = new List<string>();
                }
            }

            var regionCodes = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.RegionCodes))
            {
                try
                {
                    regionCodes = JsonSerializer.Deserialize<List<string>>(e.RegionCodes) ?? new List<string>();
                }
                catch
                {
                    regionCodes = new List<string>();
                }
            }

            var areaCodes = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.AreaCodes))
            {
                try
                {
                    areaCodes = JsonSerializer.Deserialize<List<string>>(e.AreaCodes) ?? new List<string>();
                }
                catch
                {
                    areaCodes = new List<string>();
                }
            }

            return new PropertySelection
            {
                UserId = e.UserId,
                FromUnitLevel = e.FromUnitLevel,
                ToUnitLevel = e.ToUnitLevel,
                FromBeds = e.FromBeds,
                ToBeds = e.ToBeds,
                Accomodates = e.Accomodates,
                MaxRent = e.MaxRent,
                PropertyCode = e.PropertyCode,
                City = e.City,
                State = e.State,
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
                BuildingCodes = buildingCodes,
                RegionCodes = regionCodes,
                AreaCodes = areaCodes
            };
        }
    }
}
