using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Contacts
{
    public partial class ContactRepository : IContactRepository
    {
        public async Task DeleteByIdAsync(Guid contactId, Guid modifiedBy)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Organization.Contact_DeleteById", new
            {
                ContactId = contactId,
                ModifiedBy = modifiedBy
            });
        }
    }
}
