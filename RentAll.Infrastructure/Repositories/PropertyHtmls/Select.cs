using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyHtmls
{
	public partial class PropertyHtmlRepository : IPropertyHtmlRepository
	{
		public async Task<PropertyHtml?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyHtmlEntity>("Property.PropertyHtml_GetByPropertyId", new
			{
				PropertyId = propertyId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}


