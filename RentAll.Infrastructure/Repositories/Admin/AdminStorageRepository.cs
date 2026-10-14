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
            """);

    public Task<int> UpdateOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath)
        => ExecuteAsync("""
            UPDATE [Organization].[Office]
            SET LogoPath = @LogoPath
            WHERE OfficeId = @OfficeId AND OrganizationId = @OrganizationId
            """, new { OfficeId = officeId, OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<OfficeLogoRow>> GetAccountingOfficeLogosAsync()
        => QueryAsync<OfficeLogoRow>("""
            SELECT ao.OfficeId, ao.OrganizationId, o.Name, ao.LogoPath
            FROM [Organization].[AccountingOffice] ao
            LEFT JOIN [Organization].[Office] o ON o.OfficeId = ao.OfficeId
            """);

    public Task<int> UpdateAccountingOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath)
        => ExecuteAsync("""
            UPDATE [Organization].[AccountingOffice]
            SET LogoPath = @LogoPath
            WHERE OfficeId = @OfficeId AND OrganizationId = @OrganizationId
            """, new { OfficeId = officeId, OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<OrganizationLogoRow>> GetOrganizationLogosAsync()
        => QueryAsync<OrganizationLogoRow>("""
            SELECT OrganizationId, LogoPath
            FROM [Organization].[Organization]
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
            """);

    public Task<int> UpdateApplianceDecalPathAsync(int applianceId, string? decalPath)
        => ExecuteAsync("""
            UPDATE [Maintenance].[Appliance]
            SET DecalPath = @DecalPath
            WHERE ApplianceId = @ApplianceId
            """, new { ApplianceId = applianceId, DecalPath = decalPath });

    public Task<IReadOnlyList<PropertyPhotoRow>> GetListingPhotosAsync()
        => QueryAsync<PropertyPhotoRow>("""
            SELECT pp.PhotoId, p.OrganizationId, o.Name AS OfficeName, p.PropertyCode, pp.PhotoPath
            FROM [Property].[PropertyPhoto] pp
            INNER JOIN [Property].[Property] p ON p.PropertyId = pp.PropertyId
            LEFT JOIN [Organization].[Office] o ON o.OfficeId = p.OfficeId
            """);

    public Task<int> UpdateListingPhotoPathAsync(int photoId, Guid organizationId, string? photoPath)
        => ExecuteAsync("""
            UPDATE pp
            SET pp.PhotoPath = ISNULL(@PhotoPath, '')
            FROM [Property].[PropertyPhoto] pp
            INNER JOIN [Property].[Property] p ON p.PropertyId = pp.PropertyId
            WHERE pp.PhotoId = @PhotoId AND p.OrganizationId = @OrganizationId
            """, new { PhotoId = photoId, OrganizationId = organizationId, PhotoPath = photoPath });

    public Task<IReadOnlyList<InspectionPhotoRow>> GetInspectionPhotosAsync()
        => QueryAsync<InspectionPhotoRow>("""
            SELECT ph.PhotoId, ph.OrganizationId, COALESCE(o.Name, mo.Name) AS OfficeName, p.PropertyCode, ph.PhotoPath
            FROM [Maintenance].[Photo] ph
            LEFT JOIN [Organization].[Office] o ON o.OfficeId = ph.OfficeId
            LEFT JOIN [Maintenance].[Maintenance] m ON m.MaintenanceId = ph.MaintenanceId
            LEFT JOIN [Organization].[Office] mo ON mo.OfficeId = m.OfficeId
            LEFT JOIN [Property].[Property] p ON p.PropertyId = m.PropertyId
            """);

    public Task<int> UpdateInspectionPhotoPathAsync(Guid photoId, Guid organizationId, string? photoPath)
        => ExecuteAsync("""
            UPDATE [Maintenance].[Photo]
            SET PhotoPath = ISNULL(@PhotoPath, '')
            WHERE PhotoId = @PhotoId AND OrganizationId = @OrganizationId
            """, new { PhotoId = photoId, OrganizationId = organizationId, PhotoPath = photoPath });

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
