using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Infrastructure.Repositories.Admin;

public class AdminStorageRepository : IAdminStorageRepository
{
    private readonly string _dbConnectionString;

    public AdminStorageRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    public Task<IReadOnlyList<OfficeLogoRow>> GetOfficeLogosAsync()
        => QueryAsync<OfficeLogoRow>("""
            SELECT OfficeId, OrganizationId, Name, LogoPath
            FROM [Organization].[Office]
            WHERE LogoPath IS NOT NULL AND LTRIM(RTRIM(LogoPath)) <> ''
            """);

    public Task<int> UpdateOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath)
        => ExecuteAsync("""
            UPDATE [Organization].[Office]
            SET LogoPath = @LogoPath
            WHERE OfficeId = @OfficeId AND OrganizationId = @OrganizationId
            """, new { OfficeId = officeId, OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<OrganizationLogoRow>> GetOrganizationLogosAsync()
        => QueryAsync<OrganizationLogoRow>("""
            SELECT OrganizationId, LogoPath
            FROM [Organization].[Organization]
            WHERE LogoPath IS NOT NULL AND LTRIM(RTRIM(LogoPath)) <> ''
            """);

    public Task<int> UpdateOrganizationLogoPathAsync(Guid organizationId, string? logoPath)
        => ExecuteAsync("""
            UPDATE [Organization].[Organization]
            SET LogoPath = @LogoPath
            WHERE OrganizationId = @OrganizationId
            """, new { OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<ProfileImageRow>> GetProfileImagesAsync()
        => QueryAsync<ProfileImageRow>("""
            SELECT u.UserId, u.OrganizationId, o.Name AS OfficeName, u.ProfilePath
            FROM [User].[User] u
            LEFT JOIN [Organization].[Office] o ON o.OfficeId = u.DefaultOfficeId AND o.OrganizationId = u.OrganizationId
            WHERE u.ProfilePath IS NOT NULL AND LTRIM(RTRIM(u.ProfilePath)) <> ''
            """);

    public Task<int> UpdateProfilePathAsync(Guid userId, Guid organizationId, string? profilePath)
        => ExecuteAsync("""
            UPDATE [User].[User]
            SET ProfilePath = @ProfilePath
            WHERE UserId = @UserId AND OrganizationId = @OrganizationId
            """, new { UserId = userId, OrganizationId = organizationId, ProfilePath = profilePath });

    public Task<IReadOnlyList<StateFormImageRow>> GetStateFormImagesAsync()
        => QueryAsync<StateFormImageRow>("""
            SELECT StateFormId, OrganizationId, Path
            FROM [Organization].[StateForm]
            WHERE Path IS NOT NULL AND LTRIM(RTRIM(Path)) <> ''
            """);

    public Task<int> UpdateStateFormPathAsync(int stateFormId, string organizationId, string? path)
        => ExecuteAsync("""
            UPDATE [Organization].[StateForm]
            SET Path = ISNULL(@Path, '')
            WHERE StateFormId = @StateFormId AND OrganizationId = @OrganizationId
            """, new { StateFormId = stateFormId, OrganizationId = organizationId, Path = path });

    public Task<IReadOnlyList<ReceiptImageRow>> GetReceiptImagesAsync()
        => QueryAsync<ReceiptImageRow>("""
            SELECT r.ReceiptId, r.OrganizationId, o.Name AS OfficeName, r.ReceiptPath
            FROM [Maintenance].[Receipt] r
            INNER JOIN [Organization].[Office] o ON o.OfficeId = r.OfficeId AND o.OrganizationId = r.OrganizationId
            WHERE r.ReceiptPath IS NOT NULL AND LTRIM(RTRIM(r.ReceiptPath)) <> ''
            """);

    public Task<int> UpdateReceiptPathAsync(Guid receiptId, Guid organizationId, string? receiptPath)
        => ExecuteAsync("""
            UPDATE [Maintenance].[Receipt]
            SET ReceiptPath = @ReceiptPath
            WHERE ReceiptId = @ReceiptId AND OrganizationId = @OrganizationId
            """, new { ReceiptId = receiptId, OrganizationId = organizationId, ReceiptPath = receiptPath });

    public Task<IReadOnlyList<ReceiptImageRow>> GetReceiptDraftImagesAsync()
        => QueryAsync<ReceiptImageRow>("""
            SELECT d.ReceiptDraftId AS ReceiptId, d.OrganizationId, o.Name AS OfficeName, d.ReceiptPath
            FROM [Maintenance].[ReceiptDraft] d
            LEFT JOIN [Organization].[Office] o ON o.OfficeId = d.OfficeId AND o.OrganizationId = d.OrganizationId
            WHERE d.ReceiptPath IS NOT NULL AND LTRIM(RTRIM(d.ReceiptPath)) <> ''
            """);

    public Task<int> UpdateReceiptDraftPathAsync(Guid receiptDraftId, Guid organizationId, string? receiptPath)
        => ExecuteAsync("""
            UPDATE [Maintenance].[ReceiptDraft]
            SET ReceiptPath = @ReceiptPath
            WHERE ReceiptDraftId = @ReceiptDraftId AND OrganizationId = @OrganizationId
            """, new { ReceiptDraftId = receiptDraftId, OrganizationId = organizationId, ReceiptPath = receiptPath });

    public Task<IReadOnlyList<ApplianceImageRow>> GetApplianceDecalsAsync()
        => QueryAsync<ApplianceImageRow>("""
            SELECT a.ApplianceId, p.OrganizationId, o.Name AS OfficeName, p.PropertyCode, a.DecalPath
            FROM [Maintenance].[Appliance] a
            INNER JOIN [Property].[Property] p ON p.PropertyId = a.PropertyId
            LEFT JOIN [Organization].[Office] o ON o.OfficeId = p.OfficeId AND o.OrganizationId = p.OrganizationId
            WHERE a.DecalPath IS NOT NULL AND LTRIM(RTRIM(a.DecalPath)) <> ''
            """);

    public Task<int> UpdateApplianceDecalPathAsync(int applianceId, string? decalPath)
        => ExecuteAsync("""
            UPDATE [Maintenance].[Appliance]
            SET DecalPath = @DecalPath
            WHERE ApplianceId = @ApplianceId
            """, new { ApplianceId = applianceId, DecalPath = decalPath });

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.QueryAsync<T>(sql);
        return rows.ToList();
    }

    private async Task<int> ExecuteAsync(string sql, object parameters)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await db.ExecuteAsync(sql, parameters);
    }
}
