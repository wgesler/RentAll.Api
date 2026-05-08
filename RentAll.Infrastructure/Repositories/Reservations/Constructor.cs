using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Serialization;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Reservations
{
    public partial class ReservationRepository : IReservationRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = SqlColumnJsonSerializerOptions.CaseInsensitive;

        private readonly string _dbConnectionString;

        public ReservationRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private static string SerializeReservationContactIds(IReadOnlyList<Guid> contactIds)
        {
            if (contactIds == null || contactIds.Count == 0)
                return "[]";
            return JsonSerializer.Serialize(contactIds.Where(id => id != Guid.Empty).Distinct().ToList());
        }

        private List<Guid> DeserializeReservationContactIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<Guid>();
            try
            {
                return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }

        private Reservation ConvertEntityToModel(ReservationEntity e)
        {
            List<ExtraFeeLine> extraFeeLines = new List<ExtraFeeLine>();
            if (!string.IsNullOrWhiteSpace(e.ExtraFeeLines))
            {
                try
                {
                    var entityLines = JsonSerializer.Deserialize<List<ExtraFeeLineEntity>>(e.ExtraFeeLines, JsonOptions) ?? new List<ExtraFeeLineEntity>();
                    extraFeeLines = entityLines.Select(ConvertExtraFeeLineEntityToModel).ToList();
                }
                catch
                {
                    extraFeeLines = new List<ExtraFeeLine>();
                }
            }

            return new Reservation
            {
                ReservationId = e.ReservationId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                ReservationCode = e.ReservationCode,
                AgentId = e.AgentId,
                PropertyId = e.PropertyId,
                ContactIds = DeserializeReservationContactIds(e.ContactIds),
                ContactName = e.ContactName,
                CompanyId = e.CompanyId,
                CompanyName = e.CompanyName,
                ReservationType = (ReservationType)e.ReservationTypeId,
                ReservationStatus = (ReservationStatus)e.ReservationStatusId,
                ReservationNotice = (ReservationNotice)e.ReservationNoticeId,
                NumberOfPeople = e.NumberOfPeople,
                TenantName = e.TenantName,
                ReferenceNo = e.ReferenceNo,
                ArrivalDate = e.ArrivalDate,
                DepartureDate = e.DepartureDate,
                CheckInTime = (CheckInTime)e.CheckInTimeId,
                CheckOutTime = (CheckOutTime)e.CheckOutTimeId,
                LockBoxCode = e.LockBoxCode,
                UnitTenantCode = e.UnitTenantCode,
                BillingMethod = (BillingMethod)e.BillingMethodId,
                ProrateType = (ProrateType)e.ProrateTypeId,
                BillingType = (BillingType)e.BillingTypeId,
                BillingRate = e.BillingRate,
                Deposit = e.Deposit,
                DepositType = (DepositType)e.DepositTypeId,
                DepartureFee = e.DepartureFee,
                HasPets = e.HasPets,
                PetFee = e.PetFee,
                NumberOfPets = e.NumberOfPets,
                PetDescription = e.PetDescription,
                MaidService = e.MaidService,
                MaidServiceFee = e.MaidServiceFee,
                Frequency = (FrequencyType)e.FrequencyId,
                MaidStartDate = e.MaidStartDate,
                MaidUserId = e.MaidUserId,
                Taxes = e.Taxes,
                Notes = e.Notes,
                ExtraFeeLines = extraFeeLines,
                AllowExtensions = e.AllowExtensions,
                aCleanerUserId = e.aCleanerUserId,
                aCleaningDate = e.aCleaningDate,
                aCarpetUserId = e.aCarpetUserId,
                aCarpetDate = e.aCarpetDate,
                aInspectorUserId = e.aInspectorUserId,
                aInspectingDate = e.aInspectingDate,
                dCleanerUserId = e.dCleanerUserId,
                dCleaningDate = e.dCleaningDate,
                dCarpetUserId = e.dCarpetUserId,
                dCarpetDate = e.dCarpetDate,
                dInspectorUserId = e.dInspectorUserId,
                dInspectingDate = e.dInspectingDate,
                CurrentInvoiceNo = e.CurrentInvoiceNo,
                IsActive = e.IsActive,
                CreatedBy = e.CreatedBy,
                CreatedOn = e.CreatedOn,
                ModifiedBy = e.ModifiedBy,
                ModifiedOn = e.ModifiedOn
            };
        }

        private ExtraFeeLine ConvertExtraFeeLineEntityToModel(ExtraFeeLineEntity e)
        {
            return new ExtraFeeLine
            {
                ExtraFeeLineId = e.ExtraFeeLineId,
                ReservationId = e.ReservationId,
                FeeDescription = e.FeeDescription,
                FeeAmount = e.FeeAmount,
                FeeFrequency = (FrequencyType)e.FeeFrequencyId,
                CostCodeId = e.CostCodeId
            };
        }

        private ReservationList ConvertEntityToModel(ReservationListEntity e)
        {
            return new ReservationList
            {
                ReservationId = e.ReservationId,
                ReservationCode = e.ReservationCode,
                PropertyId = e.PropertyId,
                PropertyCode = e.PropertyCode,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                ContactId = e.ContactId,
                ContactName = e.ContactName,
                CompanyId = e.CompanyId,
                TenantName = e.TenantName,
                CompanyName = e.CompanyName,
                AgentCode = e.AgentCode,
                MonthlyRate = e.MonthlyRate,
                DailyRate = e.DailyRate,
                ArrivalDate = e.ArrivalDate,
                DepartureDate = e.DepartureDate,
                ReservationType = (ReservationType)e.ReservationTypeId,
                ReservationStatus = (ReservationStatus)e.ReservationStatusId,
                CurrentInvoiceNo = e.CurrentInvoiceNo,
                HasPets = e.HasPets,
                MaidUserId = e.MaidUserId,
                MaidStartDate = e.MaidStartDate,
                Frequency = (FrequencyType)e.FrequencyId,
                MaidServiceFee = e.MaidServiceFee,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                aCleanerUserId = e.aCleanerUserId,
                aCleaningDate = e.aCleaningDate,
                aCarpetUserId = e.aCarpetUserId,
                aCarpetDate = e.aCarpetDate,
                aInspectorUserId = e.aInspectorUserId,
                aInspectingDate = e.aInspectingDate,
                dCleanerUserId = e.dCleanerUserId,
                dCleaningDate = e.dCleaningDate,
                dCarpetUserId = e.dCarpetUserId,
                dCarpetDate = e.dCarpetDate,
                dInspectorUserId = e.dInspectorUserId,
                dInspectingDate = e.dInspectingDate
            };
        }

        private LeaseInformation ConvertEntityToModel(LeaseInformationEntity e)
        {
            var response = new LeaseInformation()
            {
                LeaseInformationId = e.LeaseInformationId,
                PropertyId = e.PropertyId,
                OrganizationId = e.OrganizationId,
                ContactId = e.ContactId,
                RentalPayment = e.RentalPayment,
                SecurityDeposit = e.SecurityDeposit,
                SecurityDepositWaiver = e.SecurityDepositWaiver,
                CancellationPolicy = e.CancellationPolicy,
                KeyPickUpDropOff = e.KeyPickUpDropOff,
                PartialMonth = e.PartialMonth,
                DepartureNotification = e.DepartureNotification,
                Holdover = e.Holdover,
                DepartureServiceFee = e.DepartureServiceFee,
                CheckoutProcedure = e.CheckoutProcedure,
                Parking = e.Parking,
                RulesAndRegulations = e.RulesAndRegulations,
                OccupyingTenants = e.OccupyingTenants,
                UtilityAllowance = e.UtilityAllowance,
                MaidService = e.MaidService,
                Pets = e.Pets,
                Smoking = e.Smoking,
                Emergencies = e.Emergencies,
                HomeownersAssociation = e.HomeownersAssociation,
                Indemnification = e.Indemnification,
                DefaultClause = e.DefaultClause,
                AttorneyCollectionFees = e.AttorneyCollectionFees,
                ReservedRights = e.ReservedRights,
                PropertyUse = e.PropertyUse,
                Miscellaneous = e.Miscellaneous,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }

        #region Tracker Mapping
        private static TrackerResponse ConvertEntityToModel(TrackerResponseEntity e)
        {
            return new TrackerResponse
            {
                TrackerResponseId = e.TrackerResponseId,
                TrackerDefinitionId = e.TrackerDefinitionId,
                PropertyId = e.PropertyId,
                ReservationId = e.ReservationId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                TrackerContextId = (TrackerContextType)e.TrackerContextId,
                TrackerContextCode = e.TrackerContextCode,
                TrackerDisplayName = e.TrackerDisplayName,
                TrackerDescription = e.TrackerDescription,
                TrackerSortOrder = e.TrackerSortOrder,
                EntityTypeId = e.EntityTypeId,
                EntityTypeDescription = e.EntityTypeDescription,
                EntityId = e.EntityId,
                IsChecked = e.IsChecked,
                CheckedOn = e.CheckedOn,
                CheckedBy = e.CheckedBy,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };
        }

        private static TrackerResponseOption ConvertEntityToModel(TrackerResponseOptionEntity e)
        {
            return new TrackerResponseOption
            {
                TrackerResponseId = e.TrackerResponseId,
                TrackerDefinitionOptionId = e.TrackerDefinitionOptionId,
                PropertyId = e.PropertyId,
                ReservationId = e.ReservationId,
                TrackerDefinitionId = e.TrackerDefinitionId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                OfficeName = e.OfficeName,
                TrackerContextId = (TrackerContextType)e.TrackerContextId,
                TrackerContextCode = e.TrackerContextCode,
                TrackerDisplayName = e.TrackerDisplayName,
                TrackerDescription = e.TrackerDescription,
                TrackerSortOrder = e.TrackerSortOrder,
                Label = e.Label,
                OptionDescription = e.OptionDescription,
                OptionSortOrder = e.OptionSortOrder,
                EntityTypeId = e.EntityTypeId,
                EntityTypeDescription = e.EntityTypeDescription,
                EntityId = e.EntityId,
                IsChecked = e.IsChecked,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy
            };
        }
        #endregion
    }
}
