using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Logging;

namespace RentAll.Infrastructure.Repositories.Logging;

public class LoggingRepository : ILoggingRepository
{
    private readonly string _dbConnectionString;

    public LoggingRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    #region Accounting Error Log
    public async Task<List<AccountingError>> GetAllAccountingErrorsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<AccountingErrorLogEntity>("Logging.AccountingErrorLog_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
        return rows?.Select(ConvertEntityToAccountingErrorModel).ToList() ?? [];
    }

    public async Task DeleteAllAccountingErrorsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.AccountingErrorLog_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }

    public async Task<AccountingError?> GetAccountingErrorByIdAsync(Guid accountingErrorId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<AccountingErrorLogEntity>("Logging.AccountingErrorLog_GetById", new
        {
            AccountingErrorId = accountingErrorId,
            OrganizationId = organizationId
        });
        var row = rows?.FirstOrDefault();
        return row == null ? null : ConvertEntityToAccountingErrorModel(row);
    }

    private static AccountingError ConvertEntityToAccountingErrorModel(AccountingErrorLogEntity entity)
    {
        return new AccountingError
        {
            AccountingErrorId = entity.AccountingErrorId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            Trigger = entity.Trigger,
            SourceTypeId = entity.SourceTypeId,
            SourceId = entity.SourceId,
            DocumentCode = entity.DocumentCode,
            AccountingPeriod = entity.AccountingPeriod,
            Amount = entity.Amount,
            Message = entity.Message,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy
        };
    }
    #endregion

    #region Accounting Log
    public async Task<List<AccountingLog>> GetAllAccountingLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<AccountingLogEntity>("Logging.AccountingLog_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
        return rows?.Select(ConvertEntityToAccountingLogModel).ToList() ?? [];
    }

    public async Task DeleteAllAccountingLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.AccountingLog_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }

    public async Task<AccountingLog?> GetAccountingLogByIdAsync(int id, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<AccountingLogEntity>("Logging.AccountingLog_GetById", new
        {
            Id = id,
            OrganizationId = organizationId
        });
        var row = rows?.FirstOrDefault();
        return row == null ? null : ConvertEntityToAccountingLogModel(row);
    }

    private static AccountingLog ConvertEntityToAccountingLogModel(AccountingLogEntity entity)
    {
        return new AccountingLog
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            InvoiceId = entity.InvoiceId,
            OriginalAmount = entity.OriginalAmount,
            RentalLine = entity.RentalLine,
            Split = entity.Split,
            FirstPeriod = entity.FirstPeriod,
            SecondPeriod = entity.SecondPeriod,
            FirstAmount = entity.FirstAmount,
            SecondAmount = entity.SecondAmount,
            Message = entity.Message,
            CreatedOn = entity.CreatedOn
        };
    }
    #endregion

    #region Application Log
    public async Task AddApplicationLogAsync(ApplicationLog log)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.ApplicationLog_Add", new
        {
            Level = log.Level,
            Category = log.Category,
            EventId = log.EventId,
            OrganizationId = log.OrganizationId,
            OfficeId = log.OfficeId,
            TraceId = log.TraceId,
            Message = log.Message,
            Exception = log.Exception,
            Properties = log.Properties
        });
    }

    public async Task<List<ApplicationLog>> GetAllApplicationLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<ApplicationLogEntity>("Logging.ApplicationLog_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
        return rows?.Select(ConvertEntityToApplicationLogModel).ToList() ?? [];
    }

    public async Task DeleteAllApplicationLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.ApplicationLog_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }

    public async Task<ApplicationLog?> GetApplicationLogByIdAsync(int id, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<ApplicationLogEntity>("Logging.ApplicationLog_GetById", new
        {
            Id = id,
            OrganizationId = organizationId
        });
        var row = rows?.FirstOrDefault();
        return row == null ? null : ConvertEntityToApplicationLogModel(row);
    }

    private static ApplicationLog ConvertEntityToApplicationLogModel(ApplicationLogEntity entity)
    {
        return new ApplicationLog
        {
            Id = entity.Id,
            Level = entity.Level,
            Category = entity.Category,
            EventId = entity.EventId,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            TraceId = entity.TraceId,
            Message = entity.Message,
            Exception = entity.Exception,
            Properties = entity.Properties,
            CreatedOn = entity.CreatedOn
        };
    }
    #endregion

    #region Database Error Log
    public async Task AddDatabaseErrorAsync(DatabaseErrorLog errorLog)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.DatabaseErrorLog_Add", new
        {
            OrganizationId = errorLog.OrganizationId,
            OfficeId = errorLog.OfficeId,
            TableName = errorLog.TableName,
            Message = errorLog.Message,
            Exception = errorLog.Exception
        });
    }

    public async Task<List<DatabaseErrorLog>> GetAllDatabaseErrorLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<DatabaseErrorLogEntity>("Logging.DatabaseErrorLog_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
        return rows?.Select(ConvertEntityToDatabaseErrorLogModel).ToList() ?? [];
    }

    public async Task DeleteAllDatabaseErrorLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.DatabaseErrorLog_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }

    public async Task<DatabaseErrorLog?> GetDatabaseErrorLogByIdAsync(int id, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<DatabaseErrorLogEntity>("Logging.DatabaseErrorLog_GetById", new
        {
            Id = id,
            OrganizationId = organizationId
        });
        var row = rows?.FirstOrDefault();
        return row == null ? null : ConvertEntityToDatabaseErrorLogModel(row);
    }

    private static DatabaseErrorLog ConvertEntityToDatabaseErrorLogModel(DatabaseErrorLogEntity entity)
    {
        return new DatabaseErrorLog
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            TableName = entity.TableName,
            Message = entity.Message,
            Exception = entity.Exception,
            CreatedOn = entity.CreatedOn
        };
    }
    #endregion

    #region General Error Log
    public async Task AddErrorLogAsync(LoggingErrorLog errorLog)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.GeneralErrorLog_Add", new
        {
            OrganizationId = errorLog.OrganizationId,
            OfficeId = errorLog.OfficeId,
            ReservationId = errorLog.ReservationId,
            PropertyId = errorLog.PropertyId,
            InvoiceId = errorLog.InvoiceId,
            ReceiptId = errorLog.ReceiptId,
            JournalEntryId = errorLog.JournalEntryId,
            Message = errorLog.Message,
            Exception = errorLog.Exception
        });
    }

    public async Task<List<LoggingErrorLog>> GetAllGeneralErrorLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<GeneralErrorLogEntity>("Logging.GeneralErrorLog_GetAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
        return rows?.Select(ConvertEntityToGeneralErrorLogModel).ToList() ?? [];
    }

    public async Task DeleteAllGeneralErrorLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.GeneralErrorLog_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }

    public async Task<LoggingErrorLog?> GetGeneralErrorLogByIdAsync(int id, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<GeneralErrorLogEntity>("Logging.GeneralErrorLog_GetById", new
        {
            Id = id,
            OrganizationId = organizationId
        });
        var row = rows?.FirstOrDefault();
        return row == null ? null : ConvertEntityToGeneralErrorLogModel(row);
    }

    private static LoggingErrorLog ConvertEntityToGeneralErrorLogModel(GeneralErrorLogEntity entity)
    {
        return new LoggingErrorLog
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            ReservationId = entity.ReservationId,
            PropertyId = entity.PropertyId,
            InvoiceId = entity.InvoiceId,
            ReceiptId = entity.ReceiptId,
            JournalEntryId = entity.JournalEntryId,
            Message = entity.Message,
            Exception = entity.Exception,
            CreatedOn = entity.CreatedOn
        };
    }
    #endregion

    #region Log Retention
    public async Task ApplyLogRetentionAsync(int retainDays)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.LogRetention_Apply", new
        {
            RetainDays = retainDays
        });
    }
    #endregion
}
