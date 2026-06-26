using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILoggingRepository
{
    Task AddErrorLogAsync(LoggingErrorLog errorLog);
    Task AddDatabaseErrorAsync(DatabaseErrorLog errorLog);
    Task AddApplicationLogAsync(ApplicationLog log);
    Task ApplyLogRetentionAsync(int retainDays);
}
