using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection string is not configured");
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}


