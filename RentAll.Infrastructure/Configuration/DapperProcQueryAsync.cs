using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace RentAll.Infrastructure.Configuration;

public static class SqlConnectionExtensions
{
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
		catch (Exception ex)
		{
			// Re-throw the exception with additional context
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
		catch (Exception ex)
		{
			// Re-throw the exception with additional context
			throw new InvalidOperationException(
				$"Error executing stored procedure '{procedureName}': {ex.Message}",
				ex);
		}
	}
}


