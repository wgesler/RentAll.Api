using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Properties;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties;

public partial class PropertyRepository
{
    #region Selects
    public async Task<PropertyAgreement?> GetPropertyAgreementByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return null;

        var model = ConvertEntityToModel(res.First());
        model.AgreementLines = await GetAgreementLinesByAgreementIdAsync(db, model.PropertyId);
        return model;
    }

    #endregion

    #region Creates
    public async Task<PropertyAgreement> CreatePropertyAgreementAsync(PropertyAgreement agreement)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var res = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_Add", new
            {
                PropertyId = agreement.PropertyId,
                OfficeId = agreement.OfficeId,
                ManagementFeeTypeId = (int)agreement.ManagementFeeType,
                FlatRateAmount = agreement.FlatRateAmount,
                W9Path = agreement.W9Path,
                InsurancePath = agreement.InsurancePath,
                InsuranceExpiration = agreement.InsuranceExpiration,
                AgreementPath = agreement.AgreementPath,
                Markup = agreement.Markup,
                RevenueSplitOwner = agreement.RevenueSplitOwner,
                RevenueSplitOffice = agreement.RevenueSplitOffice,
                WorkingCapitalBalance = agreement.WorkingCapitalBalance,
                LinenAndTowelFee = agreement.LinenAndTowelFee,
                HourlyLaborCost = agreement.HourlyLaborCost,
                BankName = agreement.BankName,
                RoutingNumber = agreement.RoutingNumber,
                AccountNumber = agreement.AccountNumber,
                RentalIncomeCcId = agreement.RentalIncomeCcId,
                RentalExpenseCcId = agreement.RentalExpenseCcId,
                Notes = agreement.Notes
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new InvalidOperationException("Property agreement not created");

            var createdAgreement = ConvertEntityToModel(res.First());

            if (agreement.AgreementLines != null && agreement.AgreementLines.Any())
            {
                foreach (var line in agreement.AgreementLines)
                {
                    await db.DapperProcQueryAsync<AgreementLineEntity>("Property.AgreementLine_Add", new
                    {
                        AgreementId = createdAgreement.PropertyId,
                        Title = line.Title,
                        StartDate = line.StartDate,
                        EndDate = line.EndDate,
                        Deposit = line.Deposit,
                        OneTime = line.OneTime,
                        Monthly = line.Monthly,
                        Daily = line.Daily
                    }, transaction: transaction);
                }
            }

            var populatedRes = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_GetByPropertyId", new
            {
                PropertyId = createdAgreement.PropertyId
            }, transaction: transaction);

            if (populatedRes == null || !populatedRes.Any())
                throw new InvalidOperationException("Property agreement not found");

            var model = ConvertEntityToModel(populatedRes.First());
            model.AgreementLines = await GetAgreementLinesByAgreementIdAsync(db, model.PropertyId, transaction);
            await transaction.CommitAsync();
            return model;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Updates
    public async Task<PropertyAgreement> UpdatePropertyAgreementByPropertyIdAsync(PropertyAgreement agreement)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var currentAgreementResult = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_GetByPropertyId", new
            {
                PropertyId = agreement.PropertyId
            }, transaction: transaction);

            if (currentAgreementResult == null || !currentAgreementResult.Any())
                throw new InvalidOperationException("Property agreement not found");

            var currentLineItems = await GetAgreementLinesByAgreementIdAsync(db, agreement.PropertyId, transaction);
            var currentLineIds = currentLineItems.Select(line => line.AgreementLineId).ToHashSet();
            var incomingLineIds = agreement.AgreementLines.Where(line => line.AgreementLineId > 0).Select(line => line.AgreementLineId).ToHashSet();

            var res = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_UpdateByPropertyId", new
            {
                PropertyId = agreement.PropertyId,
                OfficeId = agreement.OfficeId,
                ManagementFeeTypeId = (int)agreement.ManagementFeeType,
                FlatRateAmount = agreement.FlatRateAmount,
                W9Path = agreement.W9Path,
                InsurancePath = agreement.InsurancePath,
                InsuranceExpiration = agreement.InsuranceExpiration,
                AgreementPath = agreement.AgreementPath,
                Markup = agreement.Markup,
                RevenueSplitOwner = agreement.RevenueSplitOwner,
                RevenueSplitOffice = agreement.RevenueSplitOffice,
                WorkingCapitalBalance = agreement.WorkingCapitalBalance,
                LinenAndTowelFee = agreement.LinenAndTowelFee,
                HourlyLaborCost = agreement.HourlyLaborCost,
                BankName = agreement.BankName,
                RoutingNumber = agreement.RoutingNumber,
                AccountNumber = agreement.AccountNumber,
                RentalIncomeCcId = agreement.RentalIncomeCcId,
                RentalExpenseCcId = agreement.RentalExpenseCcId,
                Notes = agreement.Notes
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new InvalidOperationException("Property agreement not found");

            foreach (var line in agreement.AgreementLines)
            {
                if (line.AgreementLineId <= 0)
                {
                    await db.DapperProcQueryAsync<AgreementLineEntity>("Property.AgreementLine_Add", new
                    {
                        AgreementId = agreement.PropertyId,
                        Title = line.Title,
                        StartDate = line.StartDate,
                        EndDate = line.EndDate,
                        Deposit = line.Deposit,
                        OneTime = line.OneTime,
                        Monthly = line.Monthly,
                        Daily = line.Daily
                    }, transaction: transaction);
                }
                else if (currentLineIds.Contains(line.AgreementLineId))
                {
                    await db.DapperProcQueryAsync<AgreementLineEntity>("Property.AgreementLine_UpdateById", new
                    {
                        AgreementLineId = line.AgreementLineId,
                        AgreementId = agreement.PropertyId,
                        Title = line.Title,
                        StartDate = line.StartDate,
                        EndDate = line.EndDate,
                        Deposit = line.Deposit,
                        OneTime = line.OneTime,
                        Monthly = line.Monthly,
                        Daily = line.Daily
                    }, transaction: transaction);
                }
            }

            var lineIdsToDelete = currentLineIds.Except(incomingLineIds).ToList();
            foreach (var lineId in lineIdsToDelete)
            {
                await db.DapperProcExecuteAsync("Property.AgreementLine_DeleteById", new
                {
                    AgreementLineId = lineId
                }, transaction: transaction);
            }

            var populatedRes = await db.DapperProcQueryAsync<PropertyAgreementEntity>("Property.PropertyAgreements_GetByPropertyId", new
            {
                PropertyId = agreement.PropertyId
            }, transaction: transaction);

            if (populatedRes == null || !populatedRes.Any())
                throw new InvalidOperationException("Property agreement not found");

            var model = ConvertEntityToModel(populatedRes.First());
            model.AgreementLines = await GetAgreementLinesByAgreementIdAsync(db, model.PropertyId, transaction);
            await transaction.CommitAsync();
            return model;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Deletes
    public async Task DeletePropertyAgreementByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.PropertyAgreements_DeleteByPropertyId", new
        {
            PropertyId = propertyId
        });
    }

    #endregion

    private static async Task<List<AgreementLine>> GetAgreementLinesByAgreementIdAsync(SqlConnection db, Guid agreementId, System.Data.Common.DbTransaction? transaction = null)
    {
        var lineRows = await db.DapperProcQueryAsync<AgreementLineEntity>("Property.AgreementLine_GetByAgreementId", new
        {
            AgreementId = agreementId
        }, transaction: transaction);

        if (lineRows == null || !lineRows.Any())
            return new List<AgreementLine>();

        return lineRows.Select(line => new AgreementLine
        {
            AgreementLineId = line.AgreementLineId,
            AgreementId = line.AgreementId,
            Title = line.Title,
            StartDate = line.StartDate,
            EndDate = line.EndDate,
            Deposit = line.Deposit,
            OneTime = line.OneTime,
            Monthly = line.Monthly,
            Daily = line.Daily
        }).ToList();
    }
}
