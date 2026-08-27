using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILoggingRepository
{
    Task<List<AccountingError>> GetAllAccountingErrorsByOrganizationIdAsync(Guid organizationId);
    Task DeleteAllAccountingErrorsByOrganizationIdAsync(Guid organizationId);
    Task<AccountingError?> GetAccountingErrorByIdAsync(Guid accountingErrorId, Guid organizationId);

    Task<List<AccountingLog>> GetAllAccountingLogsByOrganizationIdAsync(Guid organizationId);
    Task DeleteAllAccountingLogsByOrganizationIdAsync(Guid organizationId);
    Task<AccountingLog?> GetAccountingLogByIdAsync(int id, Guid organizationId);

    Task AddApplicationLogAsync(ApplicationLog log);
    Task<List<ApplicationLog>> GetAllApplicationLogsByOrganizationIdAsync(Guid organizationId);
    Task DeleteAllApplicationLogsByOrganizationIdAsync(Guid organizationId);
    Task<ApplicationLog?> GetApplicationLogByIdAsync(int id, Guid organizationId);

    Task AddDatabaseErrorAsync(DatabaseErrorLog errorLog);
    Task<List<DatabaseErrorLog>> GetAllDatabaseErrorLogsByOrganizationIdAsync(Guid organizationId);
    Task DeleteAllDatabaseErrorLogsByOrganizationIdAsync(Guid organizationId);
    Task<DatabaseErrorLog?> GetDatabaseErrorLogByIdAsync(int id, Guid organizationId);

    Task AddErrorLogAsync(LoggingErrorLog errorLog);
    Task<List<LoggingErrorLog>> GetAllGeneralErrorLogsByOrganizationIdAsync(Guid organizationId);
    Task DeleteAllGeneralErrorLogsByOrganizationIdAsync(Guid organizationId);
    Task<LoggingErrorLog?> GetGeneralErrorLogByIdAsync(int id, Guid organizationId);

    Task AddPropertyUploadLogAsync(PropertyUploadLog log);
    Task<List<PropertyUploadLog>> GetAllPropertyUploadLogsByOrganizationIdAsync(Guid organizationId);
    Task DeleteAllPropertyUploadLogsByOrganizationIdAsync(Guid organizationId);
    Task<PropertyUploadLog?> GetPropertyUploadLogByIdAsync(int id, Guid organizationId);

    Task ApplyLogRetentionAsync(int retainDays);

    Task<SchedulerJobRun?> GetSchedulerJobRunByJobNameAsync(string jobName);
    Task<(bool Claimed, SchedulerJobRun? Run)> TryClaimSchedulerJobDayAsync(string jobName, DateOnly runDate, DateTimeOffset claimedAt, string? message);
    Task<SchedulerJobRun?> CompleteSchedulerJobRunAsync(string jobName, DateTimeOffset completedAt, string? message);
}
