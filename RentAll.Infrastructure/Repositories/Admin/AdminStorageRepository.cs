using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Admin;

public class AdminStorageRepository : IAdminStorageRepository
{
    private static readonly HashSet<string> StoredPathSources = new(StringComparer.Ordinal)
    {
        "document", "inspection", "property-agreement", "property-w9", "property-insurance",
        "contact-agreement", "contact-w9", "contact-insurance", "contact-front", "contact-back",
        "email", "quote", "office-logo", "accounting-logo", "accounting-check", "org-logo",
        "branding-logo", "branding-collapsed", "profile", "state-form", "receipt", "receipt-draft",
        "decal", "listing", "inspection-photo", "check-html", "ticket"
    };

    private readonly string _dbConnectionString;

    public AdminStorageRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    public Task<IReadOnlyList<OfficeLogoRow>> GetOfficeLogosAsync()
        => QueryProcAsync<OfficeLogoRow>("Organization.Office_GetStorageLogos");

    public Task<int> UpdateOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath)
        => ExecuteProcAsync("Organization.Office_UpdateLogoPath", new { OfficeId = officeId, OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<OfficeLogoRow>> GetAccountingOfficeLogosAsync()
        => QueryProcAsync<OfficeLogoRow>("Organization.AccountingOffice_GetStorageLogos");

    public Task<int> UpdateAccountingOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath)
        => ExecuteProcAsync("Organization.AccountingOffice_UpdateLogoPath", new { OfficeId = officeId, OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<OrganizationLogoRow>> GetOrganizationLogosAsync()
        => QueryProcAsync<OrganizationLogoRow>("Organization.Organization_GetLogoPaths");

    public Task<int> UpdateOrganizationLogoPathAsync(Guid organizationId, string? logoPath)
        => ExecuteProcAsync("Organization.Organization_UpdateLogoPath", new { OrganizationId = organizationId, LogoPath = logoPath });

    public Task<IReadOnlyList<ProfileImageRow>> GetProfileImagesAsync()
        => QueryProcAsync<ProfileImageRow>("User.User_GetProfilePaths");

    public Task<int> UpdateProfilePathAsync(Guid userId, Guid organizationId, string? profilePath)
        => ExecuteProcAsync("User.User_UpdateProfilePath", new { UserId = userId, OrganizationId = organizationId, ProfilePath = profilePath });

    public Task<IReadOnlyList<StateFormImageRow>> GetStateFormImagesAsync()
        => QueryProcAsync<StateFormImageRow>("Organization.StateForm_GetPaths");

    public Task<int> UpdateStateFormPathAsync(int stateFormId, string organizationId, string? path)
        => ExecuteProcAsync("Organization.StateForm_UpdatePath", new { StateFormId = stateFormId, OrganizationId = organizationId, Path = path });

    public Task<IReadOnlyList<ReceiptImageRow>> GetReceiptImagesAsync()
        => QueryProcAsync<ReceiptImageRow>("Maintenance.Receipt_GetStoragePaths");

    public Task<int> UpdateReceiptPathAsync(Guid receiptId, Guid organizationId, string? receiptPath)
        => ExecuteProcAsync("Maintenance.Receipt_UpdatePath", new { ReceiptId = receiptId, OrganizationId = organizationId, ReceiptPath = receiptPath });

    public Task<IReadOnlyList<ReceiptImageRow>> GetReceiptDraftImagesAsync()
        => QueryProcAsync<ReceiptImageRow>("Maintenance.ReceiptDraft_GetStoragePaths");

    public Task<int> UpdateReceiptDraftPathAsync(Guid receiptDraftId, Guid organizationId, string? receiptPath)
        => ExecuteProcAsync("Maintenance.ReceiptDraft_UpdatePath", new { ReceiptDraftId = receiptDraftId, OrganizationId = organizationId, ReceiptPath = receiptPath });

    public Task<IReadOnlyList<ApplianceImageRow>> GetApplianceDecalsAsync()
        => QueryProcAsync<ApplianceImageRow>("Maintenance.Appliance_GetDecalPaths");

    public Task<int> UpdateApplianceDecalPathAsync(int applianceId, string? decalPath)
        => ExecuteProcAsync("Maintenance.Appliance_UpdateDecalPath", new { ApplianceId = applianceId, DecalPath = decalPath });

    public Task<IReadOnlyList<PropertyPhotoRow>> GetListingPhotosAsync()
        => QueryProcAsync<PropertyPhotoRow>("Property.PropertyPhoto_GetPaths");

    public Task<int> UpdateListingPhotoPathAsync(int photoId, Guid organizationId, string? photoPath)
        => ExecuteProcAsync("Property.PropertyPhoto_UpdatePath", new { PhotoId = photoId, OrganizationId = organizationId, PhotoPath = photoPath });

    public Task<IReadOnlyList<InspectionPhotoRow>> GetInspectionPhotosAsync()
        => QueryProcAsync<InspectionPhotoRow>("Maintenance.Photo_GetPaths");

    public Task<int> UpdateInspectionPhotoPathAsync(Guid photoId, Guid organizationId, string? photoPath)
        => ExecuteProcAsync("Maintenance.Photo_UpdatePath", new { PhotoId = photoId, OrganizationId = organizationId, PhotoPath = photoPath });

    public Task<IReadOnlyList<string>> GetSpacedOfficeNamesAsync()
        => QueryProcAsync<string>("Organization.Office_GetSpacedNames");

    public Task<IReadOnlyList<StoredPathRow>> GetStoredPathsForFoldersAsync(IReadOnlyList<string> folders)
    {
        if (folders.Count == 0)
            return Task.FromResult<IReadOnlyList<StoredPathRow>>(Array.Empty<StoredPathRow>());

        return QueryProcAsync<StoredPathRow>("Organization.StoragePath_GetByFolders", new { FoldersJson = JsonSerializer.Serialize(folders) });
    }

    public Task<int> UpdateStoredPathAsync(string source, string recordId, string organizationId, string documentPath)
    {
        if (!StoredPathSources.Contains(source))
            throw new InvalidOperationException("Stored path source is not valid.");

        return ExecuteProcAsync("Organization.StoragePath_Update", new
        {
            Source = source,
            RecordId = recordId,
            OrganizationId = organizationId,
            DocumentPath = documentPath
        });
    }

    private async Task<IReadOnlyList<T>> QueryProcAsync<T>(string procedureName, object? parameters = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var rows = await db.DapperProcQueryAsync<T>(procedureName, parameters);
        return rows?.ToList() ?? new List<T>();
    }

    private async Task<int> ExecuteProcAsync(string procedureName, object parameters)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await db.DapperProcQueryScalarAsync<int>(procedureName, parameters);
    }
}
