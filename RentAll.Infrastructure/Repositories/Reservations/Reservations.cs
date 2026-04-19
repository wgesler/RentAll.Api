using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

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

        public async Task<IEnumerable<Reservation>> GetReservationListByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetListByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Reservation>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<Reservation>> GetReservationActiveListByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetActiveListByPropertyId", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Reservation>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Reservation?> GetReservationByIdAsync(Guid reservationId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
            {
                ReservationId = reservationId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
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
                CheckInTimeId = (int)reservation.CheckInTime,
                CheckOutTimeId = (int)reservation.CheckOutTime,
                LockBoxCode = reservation.LockBoxCode,
                UnitTenantCode = reservation.UnitTenantCode,
                BillingMethodId = (int)reservation.BillingMethod,
                ProrateTypeId = (int)reservation.ProrateType,
                BillingTypeId = (int)reservation.BillingType,
                BillingRate = reservation.BillingRate,
                Deposit = reservation.Deposit,
                DepositTypeId = (int)reservation.DepositType,
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
                PaymentReceived = reservation.PaymentReceived,
                WelcomeLetterChecked = reservation.WelcomeLetterChecked,
                WelcomeLetterSent = reservation.WelcomeLetterSent,
                ReadyForArrival = reservation.ReadyForArrival,
                Code = reservation.Code,
                DepartureLetterChecked = reservation.DepartureLetterChecked,
                DepartureLetterSent = reservation.DepartureLetterSent,
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
                CurrentInvoiceNo = reservation.CurrentInvoiceNo,
                CreditDue = reservation.CreditDue,
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

            var populatedRes = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
            {
                ReservationId = createdReservation.ReservationId,
                OrganizationId = createdReservation.OrganizationId
            }, transaction: transaction);

            if (populatedRes == null || !populatedRes.Any())
                throw new Exception("Reservation not found");

            await transaction.CommitAsync();
            return ConvertEntityToModel(populatedRes.FirstOrDefault()!);
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
                var currentReservationResult = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
                {
                    ReservationId = reservation.ReservationId,
                    OrganizationId = reservation.OrganizationId
                }, transaction: transaction);

                if (currentReservationResult == null || !currentReservationResult.Any())
                    throw new Exception("Reservation not found");

                var currentReservation = ConvertEntityToModel(currentReservationResult.FirstOrDefault()!);
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
                    CheckInTimeId = (int)reservation.CheckInTime,
                    CheckOutTimeId = (int)reservation.CheckOutTime,
                    LockBoxCode = reservation.LockBoxCode,
                    UnitTenantCode = reservation.UnitTenantCode,
                    BillingMethodId = (int)reservation.BillingMethod,
                    ProrateTypeId = (int)reservation.ProrateType,
                    BillingTypeId = (int)reservation.BillingType,
                    BillingRate = reservation.BillingRate,
                    Deposit = reservation.Deposit,
                    DepositTypeId = (int)reservation.DepositType,
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
                    PaymentReceived = reservation.PaymentReceived,
                    WelcomeLetterChecked = reservation.WelcomeLetterChecked,
                    WelcomeLetterSent = reservation.WelcomeLetterSent,
                    ReadyForArrival = reservation.ReadyForArrival,
                    Code = reservation.Code,
                    DepartureLetterChecked = reservation.DepartureLetterChecked,
                    DepartureLetterSent = reservation.DepartureLetterSent,
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
                    CurrentInvoiceNo = reservation.CurrentInvoiceNo,
                    CreditDue = reservation.CreditDue,
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

                var updatedReservationResult = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
                {
                    ReservationId = reservation.ReservationId,
                    OrganizationId = reservation.OrganizationId
                }, transaction: transaction);

                if (updatedReservationResult == null || !updatedReservationResult.Any())
                    throw new Exception("Reservation not updated");

                await transaction.CommitAsync();
                return ConvertEntityToModel(updatedReservationResult.FirstOrDefault()!);
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
