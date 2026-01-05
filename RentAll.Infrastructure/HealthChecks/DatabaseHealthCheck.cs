using Microsoft.Extensions.Diagnostics.HealthChecks;
using RentAll.Infrastructure.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace RentAll.Infrastructure.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            if (connection is SqlConnection sqlConnection)
            {
                await sqlConnection.OpenAsync(cancellationToken);

                // Simple query to verify database connectivity
                using var command = sqlConnection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
            }
            else
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.ExecuteScalar();
            }

            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}

