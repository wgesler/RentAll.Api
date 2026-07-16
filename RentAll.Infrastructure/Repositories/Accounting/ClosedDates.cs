using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<List<ClosedDate>> GetClosedDatesByCriteriaAsync(Guid organizationId, string officeIds, DateOnly? startDate, DateOnly? endDate, int? postingStatusId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ClosedDateEntity>("Accounting.ClosedDates_GetByCriteria", new
        {
            OrganizationId = organizationId,
            Offices = officeIds,
            StartDate = startDate,
            EndDate = endDate,
            PostingStatusId = postingStatusId
        });

        if (res == null || !res.Any())
            return new List<ClosedDate>();

        return res.Select(ConvertClosedDateEntityToModel).ToList();
    }

    public async Task<ClosedDate?> GetClosedDateByIdAsync(int closedDateId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ClosedDateEntity>("Accounting.ClosedDates_GetById", new
        {
            ClosedDateId = closedDateId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertClosedDateEntityToModel(res.First());
    }

    public async Task<PostingStatus> CheckAccountingPeriodAsync(Guid organizationId, int officeId, DateOnly accountingPeriod)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Accounting.CheckPeriod", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AccountingPeriod = accountingPeriod
        });

        return Enum.IsDefined(typeof(PostingStatus), result) ? (PostingStatus)result : PostingStatus.Open;
    }
    #endregion

    #region Creates
    public async Task<ClosedDate> CreateClosedDateAsync(ClosedDate closedDate)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ClosedDateEntity>("Accounting.ClosedDates_Add", new
        {
            OrganizationId = closedDate.OrganizationId,
            OfficeId = closedDate.OfficeId,
            StartDate = closedDate.StartDate,
            EndDate = closedDate.EndDate,
            PostingStatusId = (int)closedDate.PostingStatusId
        });

        if (res == null || !res.Any())
            throw new Exception("Closed date not created");

        return ConvertClosedDateEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<ClosedDate> UpdateClosedDateByIdAsync(ClosedDate closedDate)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ClosedDateEntity>("Accounting.ClosedDates_UpdateById", new
        {
            ClosedDateId = closedDate.ClosedDateId,
            OrganizationId = closedDate.OrganizationId,
            OfficeId = closedDate.OfficeId,
            StartDate = closedDate.StartDate,
            EndDate = closedDate.EndDate,
            PostingStatusId = (int)closedDate.PostingStatusId
        });

        if (res == null || !res.Any())
            throw new Exception("Closed date not found");

        return ConvertClosedDateEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteClosedDateByIdAsync(int closedDateId, Guid organizationId, int officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.ClosedDates_DeleteById", new
        {
            ClosedDateId = closedDateId,
            OrganizationId = organizationId,
            OfficeId = officeId
        });
    }
    #endregion
}
