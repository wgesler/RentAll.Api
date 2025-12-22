using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Infrastructure.Repositories.CodeSequences;

public partial class CodeSequenceRepository : ICodeSequenceRepository
{
	private readonly string _dbConnectionString;

	public CodeSequenceRepository(IOptions<AppSettings> appSettings)
	{
		_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
	}
}



