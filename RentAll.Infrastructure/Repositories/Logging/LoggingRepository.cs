using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Logging;

public class LoggingRepository : ILoggingRepository
{
    private readonly string _dbConnectionString;

    public LoggingRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

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

    public async Task ApplyLogRetentionAsync(int retainDays)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.LogRetention_Apply", new
        {
            RetainDays = retainDays
        });
    }
}
