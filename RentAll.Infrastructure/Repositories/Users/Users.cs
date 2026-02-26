using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Users
{
    public partial class UserRepository
    {
        #region Create
        public async Task<User> CreateAsync(User user)
        {
            var userGroupsJson = user.UserGroups != null && user.UserGroups.Any()
                ? JsonSerializer.Serialize(user.UserGroups)
                : "[]";

            var officeAccessJson = user.OfficeAccess != null && user.OfficeAccess.Any()
                ? JsonSerializer.Serialize(user.OfficeAccess)
                : "[]";

            try
            {
                await using var db = new SqlConnection(_dbConnectionString);
                var res = await db.DapperProcQueryAsync<UserEntity>("User.User_Add", new
                {
                    OrganizationId = user.OrganizationId,
                    AgentId = user.AgentId,
                    CommissionRate = user.CommissionRate,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    PasswordHash = user.PasswordHash,
                    UserGroups = userGroupsJson,
                    OfficeAccess = officeAccessJson,
                    ProfilePath = user.ProfilePath,
                    StartupPageId = (int)user.StartupPage,
                    CreatedBy = user.CreatedBy
                });

                if (res == null || !res.Any())
                    throw new Exception("User not found");

                return ConvertEntityToModel(res.FirstOrDefault()!);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new User();

        }
        #endregion

        #region Select
        public async Task<IEnumerable<User>> GetAllAsync(Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_GetAll", new
            {
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<User>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_GetById", new
            {
                UserId = userId
            });

            if (res == null || !res.Any())
                throw new Exception("User not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_GetByEmail", new
            {
                Email = email
            });

            if (res == null || !res.Any())
                throw new Exception("User not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_GetByEmail", new
            {
                Email = email
            });

            if (res == null || !res.Any())
                return false;
            return true;
        }
        #endregion

        #region Update
        public async Task<User> UpdateByIdAsync(User user)
        {
            await using var db = new SqlConnection(_dbConnectionString);

            var userGroupsJson = user.UserGroups != null && user.UserGroups.Any()
                ? JsonSerializer.Serialize(user.UserGroups)
                : "[]";

            var officeAccessJson = user.OfficeAccess != null && user.OfficeAccess.Any()
                ? JsonSerializer.Serialize(user.OfficeAccess)
                : "[]";

            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_UpdateById", new
            {
                OrganizationId = user.OrganizationId,
                UserId = user.UserId,
                AgentId = user.AgentId,
                CommissionRate = user.CommissionRate,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PasswordHash = user.PasswordHash,
                UserGroups = userGroupsJson,
                OfficeAccess = officeAccessJson,
                ProfilePath = user.ProfilePath,
                StartupPageId = (int)user.StartupPage,
                IsActive = user.IsActive,
                ModifiedBy = user.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("User not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Delete
        public async Task DeleteByIdAsync(Guid userId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("User.User_DeleteById", new
            {
                UserId = userId
            });
        }
        #endregion
    }
}
