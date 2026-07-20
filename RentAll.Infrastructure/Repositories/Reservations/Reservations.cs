using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Reservations
{
    public partial class ReservationRepository
    {
        #region Selects
        public async Task<IEnumerable<ReservationList>> GetReservationListByOfficeIdAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationListEntity>("Property.Reservation_GetListByOfficeId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<ReservationDeparture>> GetMonthlyDepartedReservationsAsync(Guid organizationId, string officeAccess, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationDepartureEntity>("Property.Reservation_GetMonthlyDepartedReservations", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess,
                StartDate = startDate,
                EndDate = endDate
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationDeparture>();

            return res.Select(ConvertDepartureEntityToModel);
        }

        public async Task<IEnumerable<ReservationDeparture>> GetUnreturnedSecurityDepositsAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationDepartureEntity>("Property.Reservation_GetUnreturnedSecurityDeposits", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationDeparture>();

            return res.Select(ConvertDepartureEntityToModel);
        }

        public async Task<IEnumerable<ReservationList>> GetReservationListByOwnerIdAsync(Guid ownerId, Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationListEntity>("Property.Reservation_GetListByOwnerId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess,
                OwnerId = ownerId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<ReservationList>> GetReservationActiveListByOfficeIdAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationListEntity>("Property.Reservation_GetActiveListByOfficeId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<ReservationCodes>> GetReservationActiveCodesByOfficeIdsAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationCodes>("Property.Reservation_GetCodesByOfficeIds", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            }, commandTimeout: 120);

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationCodes>();

            return res;
        }

        public async Task<IEnumerable<ReservationList>> GetReservationListByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationListEntity>("Property.Reservation_GetListByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<ReservationList>> GetReservationActiveListByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationListEntity>("Property.Reservation_GetActiveListByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<ReservationList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<bool> WasRentedThisMonthAsync(Guid propertyId, Guid organizationId, DateOnly? asOfDate = null)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryAsync<WasRentedThisMonthEntity>("Property.Reservation_WasRentedThisMonth", new
            {
                OrganizationId = organizationId,
                PropertyId = propertyId,
                AsOfDate = asOfDate
            });

            return result?.FirstOrDefault()?.WasRentedThisMonth ?? false;
        }

        public async Task<bool> WasRentedPreviousMonthAsync(Guid propertyId, Guid organizationId, DateOnly? asOfDate = null)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryAsync<WasRentedPreviousMonthEntity>("Property.Reservation_WasRentedPreviousMonth", new
            {
                OrganizationId = organizationId,
                PropertyId = propertyId,
                AsOfDate = asOfDate
            });

            return result?.FirstOrDefault()?.WasRentedPreviousMonth ?? false;
        }

        public async Task<Reservation?> GetReservationByIdAsync(Guid reservationId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            return await LoadReservationByIdAsync(db, null, reservationId, organizationId);
        }

        private async Task<Reservation?> LoadReservationByIdAsync(
            SqlConnection db,
            IDbTransaction? transaction,
            Guid reservationId,
            Guid organizationId)
        {
            var (headers, extraFeeLines) = await db.DapperProcQueryMultipleAsync<ReservationEntity, ExtraFeeLineEntity>("Property.Reservation_GetById", new
            {
                ReservationId = reservationId,
                OrganizationId = organizationId
            }, transaction: transaction);

            return MapReservationsWithExtraFeeLineEntities(headers, extraFeeLines).FirstOrDefault();
        }
        #endregion

        #region Creates
        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.OpenAsync();
            await using var transaction = await db.BeginTransactionAsync();

            try
            {
                var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_Add", new
                {
                    OrganizationId = reservation.OrganizationId,
                    ReservationCode = reservation.ReservationCode,
                    AgentId = reservation.AgentId,
                    PropertyId = reservation.PropertyId,
                    ContactIds = SerializeReservationContactIds(reservation.ContactIds),
                    CompanyId = reservation.CompanyId,
                    ReservationTypeId = (int)reservation.ReservationType,
                    ReservationStatusId = (int)reservation.ReservationStatus,
                    ReservationNoticeId = (int)reservation.ReservationNotice,
                    NumberOfPeople = reservation.NumberOfPeople,
                    TenantName = reservation.TenantName,
                    ReferenceNo = reservation.ReferenceNo,
                    ArrivalDate = reservation.ArrivalDate,
                    DepartureDate = reservation.DepartureDate,
                    BillingStartDate = reservation.BillingStartDate,
                    BillingEndDate = reservation.BillingEndDate,
                    CheckInTimeId = (int)reservation.CheckInTime,
                    CheckOutTimeId = (int)reservation.CheckOutTime,
                    LockBoxCode = reservation.LockBoxCode,
                    UnitTenantCode = reservation.UnitTenantCode,
                    GarageCode = reservation.GarageCode,
                    BillingMethodId = (int)reservation.BillingMethod,
                    ProrateTypeId = (int)reservation.ProrateType,
                    BillingTypeId = (int)reservation.BillingType,
                    BillingRate = reservation.BillingRate,
                    Deposit = reservation.Deposit,
                    DepositTypeId = (int)reservation.DepositType,
                    DepositReturned = reservation.DepositReturned,
                    DepartureFee = reservation.DepartureFee,
                    HasPets = reservation.HasPets,
                    PetFee = reservation.PetFee,
                    NumberOfPets = reservation.NumberOfPets,
                    PetDescription = reservation.PetDescription,
                    MaidService = reservation.MaidService,
                    MaidServiceFee = reservation.MaidServiceFee,
                    FrequencyId = (int)reservation.Frequency,
                    MaidStartDate = reservation.MaidStartDate,
                    MaidUserId = reservation.MaidUserId,
                    Taxes = reservation.Taxes,
                    Notes = reservation.Notes,
                    AllowExtensions = reservation.AllowExtensions,
                    CollapseCharges = reservation.CollapseCharges,
                    InvoiceMethodId = (int)reservation.InvoiceMethod,
                    aCleanerUserId = reservation.aCleanerUserId,
                    aCleaningDate = reservation.aCleaningDate,
                    aCarpetUserId = reservation.aCarpetUserId,
                    aCarpetDate = reservation.aCarpetDate,
                    aInspectorUserId = reservation.aInspectorUserId,
                    aInspectingDate = reservation.aInspectingDate,
                    dCleanerUserId = reservation.dCleanerUserId,
                    dCleaningDate = reservation.dCleaningDate,
                    dCarpetUserId = reservation.dCarpetUserId,
                    dCarpetDate = reservation.dCarpetDate,
                    dInspectorUserId = reservation.dInspectorUserId,
                    dInspectingDate = reservation.dInspectingDate,
                    IsActive = reservation.IsActive,
                    CreatedBy = reservation.CreatedBy
                }, transaction: transaction);

                if (res == null || !res.Any())
                    throw new Exception("Reservation not created");

                var createdReservation = ConvertEntityToModel(res.FirstOrDefault()!);

                if (reservation.ExtraFeeLines != null && reservation.ExtraFeeLines.Any())
                {
                    foreach (var line in reservation.ExtraFeeLines)
                    {
                        await db.DapperProcQueryAsync<ExtraFeeLineEntity>("Property.ExtraFeeLine_Add", new
                        {
                            ReservationId = createdReservation.ReservationId,
                            FeeDescription = line.FeeDescription,
                            FeeAmount = line.FeeAmount,
                            FeeFrequencyId = (int)line.FeeFrequency,
                            CostCodeId = line.CostCodeId
                        }, transaction: transaction);
                    }
                }

                var populatedRes = await LoadReservationByIdAsync(db, transaction, createdReservation.ReservationId, createdReservation.OrganizationId);
                if (populatedRes == null)
                    throw new Exception("Reservation not found");

                await transaction.CommitAsync();
                return populatedRes;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        #endregion

        #region Updates
        public async Task<Reservation> UpdateByIdAsync(Reservation reservation)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.OpenAsync();
            await using var transaction = await db.BeginTransactionAsync();

            try
            {
                var currentReservation = await LoadReservationByIdAsync(db, transaction, reservation.ReservationId, reservation.OrganizationId);
                if (currentReservation == null)
                    throw new Exception("Reservation not found");

                var currentExtraFeeLineIds = currentReservation.ExtraFeeLines.Select(efl => efl.ExtraFeeLineId).ToHashSet();
                var incomingExtraFeeLineIds = reservation.ExtraFeeLines.Where(efl => efl.ExtraFeeLineId != 0).Select(efl => efl.ExtraFeeLineId).ToHashSet();

                var response = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_UpdateById", new
                {
                    ReservationId = reservation.ReservationId,
                    OrganizationId = reservation.OrganizationId,
                    ReservationCode = reservation.ReservationCode,
                    AgentId = reservation.AgentId,
                    PropertyId = reservation.PropertyId,
                    ContactIds = SerializeReservationContactIds(reservation.ContactIds),
                    CompanyId = reservation.CompanyId,
                    ReservationTypeId = (int)reservation.ReservationType,
                    ReservationStatusId = (int)reservation.ReservationStatus,
                    ReservationNoticeId = (int)reservation.ReservationNotice,
                    NumberOfPeople = reservation.NumberOfPeople,
                    TenantName = reservation.TenantName,
                    ReferenceNo = reservation.ReferenceNo,
                    ArrivalDate = reservation.ArrivalDate,
                    DepartureDate = reservation.DepartureDate,
                    BillingStartDate = reservation.BillingStartDate,
                    BillingEndDate = reservation.BillingEndDate,
                    CheckInTimeId = (int)reservation.CheckInTime,
                    CheckOutTimeId = (int)reservation.CheckOutTime,
                    LockBoxCode = reservation.LockBoxCode,
                    UnitTenantCode = reservation.UnitTenantCode,
                    GarageCode = reservation.GarageCode,
                    BillingMethodId = (int)reservation.BillingMethod,
                    ProrateTypeId = (int)reservation.ProrateType,
                    BillingTypeId = (int)reservation.BillingType,
                    BillingRate = reservation.BillingRate,
                    Deposit = reservation.Deposit,
                    DepositTypeId = (int)reservation.DepositType,
                    DepositReturned = reservation.DepositReturned,
                    DepartureFee = reservation.DepartureFee,
                    HasPets = reservation.HasPets,
                    PetFee = reservation.PetFee,
                    NumberOfPets = reservation.NumberOfPets,
                    PetDescription = reservation.PetDescription,
                    MaidService = reservation.MaidService,
                    MaidServiceFee = reservation.MaidServiceFee,
                    FrequencyId = (int)reservation.Frequency,
                    MaidStartDate = reservation.MaidStartDate,
                    MaidUserId = reservation.MaidUserId,
                    Taxes = reservation.Taxes,
                    Notes = reservation.Notes,
                    AllowExtensions = reservation.AllowExtensions,
                    CollapseCharges = reservation.CollapseCharges,
                    InvoiceMethodId = (int)reservation.InvoiceMethod,
                    aCleanerUserId = reservation.aCleanerUserId,
                    aCleaningDate = reservation.aCleaningDate,
                    aCarpetUserId = reservation.aCarpetUserId,
                    aCarpetDate = reservation.aCarpetDate,
                    aInspectorUserId = reservation.aInspectorUserId,
                    aInspectingDate = reservation.aInspectingDate,
                    dCleanerUserId = reservation.dCleanerUserId,
                    dCleaningDate = reservation.dCleaningDate,
                    dCarpetUserId = reservation.dCarpetUserId,
                    dCarpetDate = reservation.dCarpetDate,
                    dInspectorUserId = reservation.dInspectorUserId,
                    dInspectingDate = reservation.dInspectingDate,
                    IsActive = reservation.IsActive,
                    ModifiedBy = reservation.ModifiedBy
                }, transaction: transaction);

                if (response == null || !response.Any())
                    throw new Exception("Reservation not updated");

                if (reservation.ExtraFeeLines != null && reservation.ExtraFeeLines.Any())
                {
                    foreach (var line in reservation.ExtraFeeLines)
                    {
                        if (line.ExtraFeeLineId == 0)
                        {
                            await db.DapperProcQueryAsync<ExtraFeeLineEntity>("Property.ExtraFeeLine_Add", new
                            {
                                ReservationId = reservation.ReservationId,
                                FeeDescription = line.FeeDescription,
                                FeeAmount = line.FeeAmount,
                                FeeFrequencyId = (int)line.FeeFrequency,
                                CostCodeId = line.CostCodeId
                            }, transaction: transaction);
                        }
                        else if (currentExtraFeeLineIds.Contains(line.ExtraFeeLineId))
                        {
                            await db.DapperProcQueryAsync<ExtraFeeLineEntity>("Property.ExtraFeeLine_UpdateById", new
                            {
                                ExtraFeeLineId = line.ExtraFeeLineId,
                                ReservationId = reservation.ReservationId,
                                FeeDescription = line.FeeDescription,
                                FeeAmount = line.FeeAmount,
                                FeeFrequencyId = (int)line.FeeFrequency,
                                CostCodeId = line.CostCodeId
                            }, transaction: transaction);
                        }
                    }
                }

                var extraFeeLinesToDelete = currentExtraFeeLineIds.Except(incomingExtraFeeLineIds).ToList();
                foreach (var extraFeeLineId in extraFeeLinesToDelete)
                {
                    await db.DapperProcExecuteAsync("Property.ExtraFeeLine_DeleteById", new
                    {
                        ExtraFeeLineId = extraFeeLineId
                    }, transaction: transaction);
                }

                var updatedReservation = await LoadReservationByIdAsync(db, transaction, reservation.ReservationId, reservation.OrganizationId);
                if (updatedReservation == null)
                    throw new Exception("Reservation not updated");

                await transaction.CommitAsync();
                return updatedReservation;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Reservation> IncrementCurrentInvoiceAsync(Guid reservationId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_IncrementInvoiceById", new
            {
                ReservationId = reservationId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                throw new Exception("Reservation not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        public async Task MarkDepositReturnedAsync(Guid reservationId, Guid organizationId, Guid modifiedBy)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.Reservation_MarkDepositReturned", new
            {
                ReservationId = reservationId,
                OrganizationId = organizationId,
                ModifiedBy = modifiedBy
            });
        }
        #endregion

        #region Deletes
        public async Task DeleteReservationByIdAsync(Guid reservationId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.Reservation_DeleteById", new
            {
                ReservationId = reservationId,
                OrganizationId = organizationId
            });
        }
        #endregion
    }
}
