using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<OwnerReportRecapLoadPlan> GetOwnerReportRecapLoadPlanAsync(
        Guid organizationId,
        string officeIds,
        DateOnly reportPeriodStartDate,
        Guid? propertyId = null,
        int calculationVersion = OwnerReportAnchorCalculation.CurrentVersion)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<OwnerReportRecapLoadPlanEntity>(
            "Accounting.OwnerReportMonthlyAnchor_GetRecapLoadPlan",
            new
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                PropertyId = propertyId,
                ReportPeriodStartDate = reportPeriodStartDate.ToDateTime(TimeOnly.MinValue),
                CalculationVersion = calculationVersion
            });

        return ConvertOwnerReportRecapLoadPlanEntityToModel(entities?.FirstOrDefault());
    }

    public async Task<IReadOnlyList<OwnerReportMonthlyAnchor>> GetOwnerReportPriorMonthAnchorsAsync(
        Guid organizationId,
        string officeIds,
        DateOnly reportPeriodStartDate,
        Guid? propertyId = null,
        int calculationVersion = OwnerReportAnchorCalculation.CurrentVersion,
        bool includeDirty = false)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<OwnerReportMonthlyAnchorEntity>(
            "Accounting.OwnerReportMonthlyAnchor_GetPriorMonthByCriteria",
            new
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                PropertyId = propertyId,
                ReportPeriodStartDate = reportPeriodStartDate.ToDateTime(TimeOnly.MinValue),
                CalculationVersion = calculationVersion,
                IncludeDirty = includeDirty
            });

        return (entities ?? Enumerable.Empty<OwnerReportMonthlyAnchorEntity>())
            .Select(ConvertOwnerReportMonthlyAnchorEntityToModel)
            .ToList();
    }

    public async Task UpsertOwnerReportMonthlyAnchorAsync(OwnerReportMonthlyAnchor anchor, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.OwnerReportMonthlyAnchor_Upsert", new
        {
            OrganizationId = anchor.OrganizationId,
            OfficeId = anchor.OfficeId,
            PropertyId = anchor.PropertyId,
            PeriodMonth = anchor.PeriodMonth.ToDateTime(TimeOnly.MinValue),
            PeriodStartDate = anchor.PeriodStartDate.ToDateTime(TimeOnly.MinValue),
            PeriodEndDate = anchor.PeriodEndDate.ToDateTime(TimeOnly.MinValue),
            anchor.StartingBalance,
            anchor.ReceivedIncome,
            anchor.OwnerExpenses,
            anchor.OwnerPayment,
            anchor.OwnerPaymentPaid,
            anchor.EndingBalance,
            anchor.WorkingCapital,
            anchor.AnchorStatusId,
            anchor.CalculationVersion,
            anchor.IsDirty,
            CurrentUser = currentUser
        });
    }

    public async Task MarkOwnerReportMonthlyAnchorsDirtyFromPeriodAsync(
        Guid organizationId,
        int officeId,
        DateOnly fromPeriodMonth,
        Guid currentUser,
        Guid? propertyId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.OwnerReportMonthlyAnchor_MarkDirtyFromPeriod", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PropertyId = propertyId,
            FromPeriodMonth = fromPeriodMonth.ToDateTime(TimeOnly.MinValue),
            CurrentUser = currentUser
        });
    }

    private static OwnerReportRecapLoadPlan ConvertOwnerReportRecapLoadPlanEntityToModel(OwnerReportRecapLoadPlanEntity? entity)
    {
        if (entity == null)
        {
            return new OwnerReportRecapLoadPlan
            {
                UseNarrowRecapLoad = false,
                RecapLoadStartDate = null,
                PropertyCount = 0,
                AnchoredPropertyCount = 0,
                PriorPeriodMonth = default
            };
        }

        return new OwnerReportRecapLoadPlan
        {
            UseNarrowRecapLoad = entity.UseNarrowRecapLoad,
            RecapLoadStartDate = entity.RecapLoadStartDate.HasValue
                ? DateOnly.FromDateTime(entity.RecapLoadStartDate.Value)
                : null,
            PropertyCount = entity.PropertyCount,
            AnchoredPropertyCount = entity.AnchoredPropertyCount,
            PriorPeriodMonth = DateOnly.FromDateTime(entity.PriorPeriodMonth)
        };
    }

    private static OwnerReportMonthlyAnchor ConvertOwnerReportMonthlyAnchorEntityToModel(OwnerReportMonthlyAnchorEntity entity)
        => new()
        {
            OwnerReportMonthlyAnchorId = entity.OwnerReportMonthlyAnchorId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            PeriodMonth = DateOnly.FromDateTime(entity.PeriodMonth),
            PeriodStartDate = DateOnly.FromDateTime(entity.PeriodStartDate),
            PeriodEndDate = DateOnly.FromDateTime(entity.PeriodEndDate),
            StartingBalance = entity.StartingBalance,
            ReceivedIncome = entity.ReceivedIncome,
            OwnerExpenses = entity.OwnerExpenses,
            OwnerPayment = entity.OwnerPayment,
            OwnerPaymentPaid = entity.OwnerPaymentPaid,
            EndingBalance = entity.EndingBalance,
            WorkingCapital = entity.WorkingCapital,
            AnchorStatusId = entity.AnchorStatusId,
            CalculationVersion = entity.CalculationVersion,
            IsDirty = entity.IsDirty,
            CalculatedOn = entity.CalculatedOn
        };
}
