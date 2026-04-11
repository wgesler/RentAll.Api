using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Users
{
    public partial class UserRepository
    {
        #region Selects
        public async Task<IEnumerable<User>> GetUsersByOrganizationIdAsync(Guid organizationId)
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

        public async Task<IEnumerable<User>> GetUsersByRoleTypeAsync(Guid organizationId, string roleType)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_GetByRoleType", new
            {
                OrganizationId = organizationId,
                Role = roleType
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<User>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
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

        public async Task<User?> GetUserByEmailAsync(string email)
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

        #region Creates
        public async Task<User> CreateAsync(User user)
        {
            var userGroupsJson = user.UserGroups != null && user.UserGroups.Any()
                ? JsonSerializer.Serialize(user.UserGroups)
                : "[]";

            var officeAccessJson = user.OfficeAccess != null && user.OfficeAccess.Any()
                ? JsonSerializer.Serialize(user.OfficeAccess)
                : "[]";

            var propertiesJson = user.Properties != null && user.Properties.Any()
                ? JsonSerializer.Serialize(user.Properties)
                : "[]";

            try
            {
                await using var db = new SqlConnection(_dbConnectionString);
                var res = await db.DapperProcQueryAsync<UserEntity>("User.User_Add", new
                {
                    OrganizationId = user.OrganizationId,
                    AgentId = user.AgentId,
                    ContactId = user.ContactId,
                    CommissionRate = user.CommissionRate,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    PasswordHash = user.PasswordHash,
                    UserGroups = userGroupsJson,
                    OfficeAccess = officeAccessJson,
                    Properties = propertiesJson,
                    ProfilePath = user.ProfilePath,
                    StartupPageId = (int)user.StartupPage,
                    DefaultOfficeId = user.DefaultOfficeId,
                    CreatedBy = user.CreatedBy
                });

                if (res == null || !res.Any())
                    throw new Exception("User not found");

                return ConvertEntityToModel(res.FirstOrDefault()!);
            }
            catch
            {
                // Return empty user on failure
            }
            return new User();

        }
        #endregion

        #region Updates
        public async Task<User> UpdateByIdAsync(User user)
        {
            await using var db = new SqlConnection(_dbConnectionString);

            var userGroupsJson = user.UserGroups != null && user.UserGroups.Any()
                ? JsonSerializer.Serialize(user.UserGroups)
                : "[]";

            var officeAccessJson = user.OfficeAccess != null && user.OfficeAccess.Any()
                ? JsonSerializer.Serialize(user.OfficeAccess)
                : "[]";

            var propertiesJson = user.Properties != null && user.Properties.Any()
                ? JsonSerializer.Serialize(user.Properties)
                : "[]";

            var res = await db.DapperProcQueryAsync<UserEntity>("User.User_UpdateById", new
            {
                OrganizationId = user.OrganizationId,
                UserId = user.UserId,
                AgentId = user.AgentId,
                ContactId = user.ContactId,
                CommissionRate = user.CommissionRate,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PasswordHash = user.PasswordHash,
                UserGroups = userGroupsJson,
                OfficeAccess = officeAccessJson,
                Properties = propertiesJson,
                ProfilePath = user.ProfilePath,
                StartupPageId = (int)user.StartupPage,
                DefaultOfficeId = user.DefaultOfficeId,
                IsActive = user.IsActive,
                ModifiedBy = user.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("User not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeleteUserByIdAsync(Guid userId)
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
