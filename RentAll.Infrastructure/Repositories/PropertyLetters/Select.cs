using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyLetters
{
	public partial class PropertyLetterRepository : IPropertyLetterRepository
	{
		public async Task<IEnumerable<PropertyLetter>> GetAllAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyLetterEntity>("dbo.PropertyLetter_GetAll", null);

			if (res == null || !res.Any())
				return Enumerable.Empty<PropertyLetter>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<PropertyLetter?> GetByPropertyIdAsync(Guid propertyId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyLetterEntity>("dbo.PropertyLetter_GetByPropertyId", new
			{
				PropertyId = propertyId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}

