using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Configuration;

public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _connectionString;

    public DatabaseConnectionFactory(IConfiguration configuration)
    {
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            _connectionString = defaultConnection;
            return;
        }

        // Backward-compatible fallback for existing appsettings structure.
        var dbConnections = configuration.GetSection("AppSettings:DbConnections").GetChildren();
        foreach (var db in dbConnections)
        {
            var dbName = db["DbName"];
            var connectionString = db["ConnectionString"];
            if (string.Equals(dbName, "RentAll", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(connectionString))
            {
                _connectionString = connectionString;
                return;
            }
        }

        foreach (var db in dbConnections)
        {
            var connectionString = db["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _connectionString = connectionString;
                return;
            }
        }

        throw new InvalidOperationException("No database connection string configured. Set ConnectionStrings:DefaultConnection or AppSettings:DbConnections.");
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}


