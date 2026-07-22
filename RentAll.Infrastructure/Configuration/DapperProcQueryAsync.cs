using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace RentAll.Infrastructure.Configuration;

public static class SqlConnectionExtensions
{
    static SqlConnectionExtensions() => DapperDateOnlyTypeHandlers.EnsureRegistered();

    public static async Task<IEnumerable<T>?> DapperProcQueryAsync<T>(
        this SqlConnection connection,
        string procedureName,
        object? parameters = null,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        try
        {
            var result = await connection.QueryAsync<T>(
                sql: procedureName,
                param: parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout,
                transaction: transaction
            );

            return result;
        }
        catch (SqlException ex)
        {
            await TryLogDatabaseErrorAsync(connection, procedureName, parameters, ex);
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            // Re-throw the exception with additional context
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
    }

    public static async Task<T?> DapperProcQueryScalarAsync<T>(
        this SqlConnection connection,
        string procedureName,
        object? parameters = null,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        try
        {
            var result = await connection.QueryFirstOrDefaultAsync<T>(
                sql: procedureName,
                param: parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout,
                transaction: transaction
            );

            return result;
        }
        catch (SqlException ex)
        {
            await TryLogDatabaseErrorAsync(connection, procedureName, parameters, ex);
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            // Re-throw the exception with additional context
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
    }

    public static async Task<(IEnumerable<TFirst> First, IEnumerable<TSecond> Second)> DapperProcQueryMultipleAsync<TFirst, TSecond>(
        this SqlConnection connection,
        string procedureName,
        object? parameters = null,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        try
        {
            using var multi = await connection.QueryMultipleAsync(
                sql: procedureName,
                param: parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout,
                transaction: transaction
            );

            if (multi.IsConsumed)
                return (Enumerable.Empty<TFirst>(), Enumerable.Empty<TSecond>());

            var first = await multi.ReadAsync<TFirst>();
            var second = multi.IsConsumed
                ? Enumerable.Empty<TSecond>()
                : await multi.ReadAsync<TSecond>();

            return (first, second);
        }
        catch (SqlException ex)
        {
            await TryLogDatabaseErrorAsync(connection, procedureName, parameters, ex);
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
    }

    public static async Task DapperProcExecuteAsync(
        this SqlConnection connection,
        string procedureName,
        object? parameters = null,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        try
        {
            var rowsAffected = await connection.ExecuteAsync(
                sql: procedureName,
                param: parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout,
                transaction: transaction
            );
        }
        catch (SqlException ex)
        {
            await TryLogDatabaseErrorAsync(connection, procedureName, parameters, ex);
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            // Re-throw the exception with additional context
            throw new InvalidOperationException(
                $"Error executing stored procedure '{procedureName}': {ex.Message}",
                ex);
        }
    }

    private static async Task TryLogDatabaseErrorAsync(SqlConnection connection, string procedureName, object? parameters, SqlException exception)
    {
        if (procedureName.Equals("Logging.DatabaseErrorLog_Add", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            await using var loggingConnection = new SqlConnection(connection.ConnectionString);
            await loggingConnection.ExecuteAsync(
                sql: "Logging.DatabaseErrorLog_Add",
                param: new
                {
                    OrganizationId = ResolveOrganizationId(parameters),
                    OfficeId = ResolveOfficeId(parameters),
                    TableName = ResolveTableName(procedureName),
                    Message = TruncateMessage(exception.Message),
                    Exception = exception.ToString()
                },
                commandType: CommandType.StoredProcedure
            );
        }
        catch
        {
            // Ignore logging failures to avoid masking the original SQL error.
        }
    }

    private static Guid? ResolveOrganizationId(object? parameters)
    {
        var rawValue = GetPropertyValue(parameters, "OrganizationId");
        if (rawValue is Guid guidValue)
            return guidValue;

        if (rawValue is string stringValue && Guid.TryParse(stringValue, out var parsedGuid))
            return parsedGuid;

        return null;
    }

    private static int? ResolveOfficeId(object? parameters)
    {
        var rawValue = GetPropertyValue(parameters, "OfficeId");
        if (rawValue is int intValue)
            return intValue;

        if (rawValue is string stringValue && int.TryParse(stringValue, out var parsedInt))
            return parsedInt;

        return null;
    }

    private static object? GetPropertyValue(object? target, string propertyName)
    {
        if (target == null)
            return null;

        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        return property?.GetValue(target);
    }

    private static string? ResolveTableName(string procedureName)
    {
        var dotIndex = procedureName.IndexOf('.');
        if (dotIndex < 0 || dotIndex == procedureName.Length - 1)
            return null;

        var objectName = procedureName[(dotIndex + 1)..];
        var underscoreIndex = objectName.IndexOf('_');
        return underscoreIndex > 0 ? objectName[..underscoreIndex] : objectName;
    }

    private static string TruncateMessage(string message)
    {
        const int maxLength = 2500;
        if (message.Length <= maxLength)
            return message;

        return message[..maxLength];
    }
}
