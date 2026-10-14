namespace RentAll.Domain.Interfaces.Repositories;

public interface IAdminStorageRepository
{
    Task<IReadOnlyList<OfficeLogoRow>> GetOfficeLogosAsync();
    Task<int> UpdateOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath);
    Task<IReadOnlyList<OfficeLogoRow>> GetAccountingOfficeLogosAsync();
    Task<int> UpdateAccountingOfficeLogoPathAsync(int officeId, Guid organizationId, string? logoPath);
    Task<IReadOnlyList<OrganizationLogoRow>> GetOrganizationLogosAsync();
    Task<int> UpdateOrganizationLogoPathAsync(Guid organizationId, string? logoPath);
    Task<IReadOnlyList<ProfileImageRow>> GetProfileImagesAsync();
    Task<int> UpdateProfilePathAsync(Guid userId, Guid organizationId, string? profilePath);
    Task<IReadOnlyList<StateFormImageRow>> GetStateFormImagesAsync();
    Task<int> UpdateStateFormPathAsync(int stateFormId, string organizationId, string? path);
    Task<IReadOnlyList<ReceiptImageRow>> GetReceiptImagesAsync();
    Task<int> UpdateReceiptPathAsync(Guid receiptId, Guid organizationId, string? receiptPath);
    Task<IReadOnlyList<ReceiptImageRow>> GetReceiptDraftImagesAsync();
    Task<int> UpdateReceiptDraftPathAsync(Guid receiptDraftId, Guid organizationId, string? receiptPath);
    Task<IReadOnlyList<ApplianceImageRow>> GetApplianceDecalsAsync();
    Task<int> UpdateApplianceDecalPathAsync(int applianceId, string? decalPath);
    Task<IReadOnlyList<PropertyPhotoRow>> GetListingPhotosAsync();
    Task<int> UpdateListingPhotoPathAsync(int photoId, Guid organizationId, string? photoPath);
    Task<IReadOnlyList<InspectionPhotoRow>> GetInspectionPhotosAsync();
    Task<int> UpdateInspectionPhotoPathAsync(Guid photoId, Guid organizationId, string? photoPath);
    Task<IReadOnlyList<string>> GetSpacedOfficeNamesAsync();
    Task<IReadOnlyList<StoredPathRow>> GetStoredPathsForFoldersAsync(IReadOnlyList<string> folders);
    Task<int> UpdateStoredPathAsync(string source, string recordId, string organizationId, string documentPath);
}

public sealed class OfficeLogoRow
{
    public int OfficeId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
}

public sealed class OrganizationLogoRow
{
    public Guid OrganizationId { get; set; }
    public string LogoPath { get; set; } = string.Empty;
}

public sealed class ProfileImageRow
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OfficeName { get; set; }
    public string ProfilePath { get; set; } = string.Empty;
}

public sealed class StateFormImageRow
{
    public int StateFormId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public sealed class ReceiptImageRow
{
    public Guid ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OfficeName { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string ReceiptPath { get; set; } = string.Empty;
}

public sealed class PropertyPhotoRow
{
    public int PhotoId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OfficeName { get; set; }
    public string? PropertyCode { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
}

public sealed class InspectionPhotoRow
{
    public Guid PhotoId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OfficeName { get; set; }
    public string? PropertyCode { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
}

public sealed class StoredPathRow
{
    public string Source { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = string.Empty;
    public int MaxLength { get; set; }
}

public sealed class ApplianceImageRow
{
    public int ApplianceId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OfficeName { get; set; }
    public string? PropertyCode { get; set; }
    public string DecalPath { get; set; } = string.Empty;
}
