using RentAll.Api.Dtos.Admin;
using RentAll.Domain.Constants;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;
using System.Text.RegularExpressions;

namespace RentAll.Api.Services;

public class StoragePathRepairService
{
    private const int ErrorLimit = 25;
    private static readonly Regex GuidePathRegex = new("data-rentall-guide-path=\"(?<path>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly IAdminStorageRepository _adminStorageRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IFileService _fileService;
    private readonly ILogger<StoragePathRepairService> _logger;

    public StoragePathRepairService(
        IAdminStorageRepository adminStorageRepository,
        IOrganizationRepository organizationRepository,
        IFileService fileService,
        ILogger<StoragePathRepairService> logger)
    {
        _adminStorageRepository = adminStorageRepository;
        _organizationRepository = organizationRepository;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<StorageRepairResultDto> RepairListingPhotosAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetListingPhotosAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PhotoPath))
                continue;
            await MoveOneAsync(result, "listing photo " + row.PhotoId, row.OrganizationId, row.OfficeName, "listings", row.PropertyCode, row.PhotoPath, 2048, ImageType.Photos, report,
                path => _adminStorageRepository.UpdateListingPhotoPathAsync(row.PhotoId, row.OrganizationId, path));
        }
        rows = await _adminStorageRepository.GetListingPhotosAsync();
        await ReattachEmptyImagesAsync(result, report, rows.Where(row => string.IsNullOrWhiteSpace(row.PhotoPath)).Select(row => new EmptyImageSlot("listing photo " + row.PhotoId, row.OrganizationId, row.OfficeName, "listings", row.PropertyCode, 2048, path => _adminStorageRepository.UpdateListingPhotoPathAsync(row.PhotoId, row.OrganizationId, path))).ToList(), rows.Select(row => row.PhotoPath));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairInspectionPhotosAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetInspectionPhotosAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PhotoPath))
                continue;
            await MoveOneAsync(result, "inspection photo " + row.PhotoId, row.OrganizationId, row.OfficeName, "inspection", row.PropertyCode, row.PhotoPath, 2048, ImageType.Photos, report,
                path => _adminStorageRepository.UpdateInspectionPhotoPathAsync(row.PhotoId, row.OrganizationId, path));
        }
        rows = await _adminStorageRepository.GetInspectionPhotosAsync();
        await ReattachEmptyImagesAsync(result, report, rows.Where(row => string.IsNullOrWhiteSpace(row.PhotoPath)).Select(row => new EmptyImageSlot("inspection photo " + row.PhotoId, row.OrganizationId, row.OfficeName, "inspection", row.PropertyCode, 2048, path => _adminStorageRepository.UpdateInspectionPhotoPathAsync(row.PhotoId, row.OrganizationId, path))).ToList(), rows.Select(row => row.PhotoPath));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairOfficeLogosAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetOfficeLogosAsync();
        var accountingRows = await _adminStorageRepository.GetAccountingOfficeLogosAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count + accountingRows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.LogoPath))
                continue;
            await MoveOneAsync(result, "office " + row.OfficeId, row.OrganizationId, row.Name, "logos", null, row.LogoPath, 2048, ImageType.Logos, report,
                path => _adminStorageRepository.UpdateOfficeLogoPathAsync(row.OfficeId, row.OrganizationId, path));
        }

        foreach (var row in accountingRows)
        {
            if (string.IsNullOrWhiteSpace(row.LogoPath))
                continue;
            await MoveOneAsync(result, "accounting office " + row.OfficeId, row.OrganizationId, row.Name, "logos", null, row.LogoPath, 2048, ImageType.Logos, report,
                path => _adminStorageRepository.UpdateAccountingOfficeLogoPathAsync(row.OfficeId, row.OrganizationId, path));
        }
        rows = await _adminStorageRepository.GetOfficeLogosAsync();
        accountingRows = await _adminStorageRepository.GetAccountingOfficeLogosAsync();
        var officeSlots = rows.Where(row => string.IsNullOrWhiteSpace(row.LogoPath)).Select(row => new EmptyImageSlot("office " + row.OfficeId, row.OrganizationId, row.Name, "logos", null, 2048, path => _adminStorageRepository.UpdateOfficeLogoPathAsync(row.OfficeId, row.OrganizationId, path)));
        var accountingSlots = accountingRows.Where(row => string.IsNullOrWhiteSpace(row.LogoPath)).Select(row => new EmptyImageSlot("accounting office " + row.OfficeId, row.OrganizationId, row.Name, "logos", null, 2048, path => _adminStorageRepository.UpdateAccountingOfficeLogoPathAsync(row.OfficeId, row.OrganizationId, path)));
        await ReattachEmptyImagesAsync(result, report, officeSlots.Concat(accountingSlots).ToList(), rows.Select(row => row.LogoPath).Concat(accountingRows.Select(row => row.LogoPath)));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairOrganizationLogosAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetOrganizationLogosAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.LogoPath))
                continue;
            await MoveOneAsync(result, "organization " + row.OrganizationId, row.OrganizationId, null, "logos", null, row.LogoPath, 2048, ImageType.Logos, report,
                path => _adminStorageRepository.UpdateOrganizationLogoPathAsync(row.OrganizationId, path));
        }
        rows = await _adminStorageRepository.GetOrganizationLogosAsync();
        await ReattachEmptyImagesAsync(result, report, rows.Where(row => string.IsNullOrWhiteSpace(row.LogoPath)).Select(row => new EmptyImageSlot("organization " + row.OrganizationId, row.OrganizationId, null, "logos", null, 2048, path => _adminStorageRepository.UpdateOrganizationLogoPathAsync(row.OrganizationId, path))).ToList(), rows.Select(row => row.LogoPath));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairProfileImagesAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetProfileImagesAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ProfilePath))
                continue;
            await MoveOneAsync(result, "user " + row.UserId, row.OrganizationId, row.OfficeName, "profiles", null, row.ProfilePath, 2048, ImageType.Profiles, report,
                path => _adminStorageRepository.UpdateProfilePathAsync(row.UserId, row.OrganizationId, path));
        }
        rows = await _adminStorageRepository.GetProfileImagesAsync();
        await ReattachEmptyImagesAsync(result, report, rows.Where(row => string.IsNullOrWhiteSpace(row.ProfilePath)).Select(row => new EmptyImageSlot("user " + row.UserId, row.OrganizationId, row.OfficeName, "profiles", null, 2048, path => _adminStorageRepository.UpdateProfilePathAsync(row.UserId, row.OrganizationId, path))).ToList(), rows.Select(row => row.ProfilePath));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairStateFormsAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetStateFormImagesAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.OrganizationId, out var organizationId))
            {
                AddFailure(result, "state form " + row.StateFormId + ": organization id is not valid");
                await ReportAsync(report, "file", FileNameOf(row.Path), "failed", "organization id is not valid");
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Path))
                continue;
            await MoveOneAsync(result, "state form " + row.StateFormId, organizationId, null, "stateform", null, row.Path, 500, ImageType.StateForm, report,
                path => _adminStorageRepository.UpdateStateFormPathAsync(row.StateFormId, row.OrganizationId, path));
        }
        rows = await _adminStorageRepository.GetStateFormImagesAsync();
        await ReattachEmptyImagesAsync(result, report, rows.Where(row => string.IsNullOrWhiteSpace(row.Path) && Guid.TryParse(row.OrganizationId, out _)).Select(row => new EmptyImageSlot("state form " + row.StateFormId, Guid.Parse(row.OrganizationId), null, "stateform", null, 500, path => _adminStorageRepository.UpdateStateFormPathAsync(row.StateFormId, row.OrganizationId, path))).ToList(), rows.Select(row => row.Path));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairReceiptImagesAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var receipts = await _adminStorageRepository.GetReceiptImagesAsync();
        var drafts = await _adminStorageRepository.GetReceiptDraftImagesAsync();
        await ReportAsync(report, "start", "", "", "", receipts.Count + drafts.Count);
        foreach (var row in receipts)
        {
            if (string.IsNullOrWhiteSpace(row.ReceiptPath))
                continue;
            await MoveOneAsync(result, "receipt " + row.ReceiptId, row.OrganizationId, row.OfficeName, "receipts", null, row.ReceiptPath, 2048, ImageType.Receipts, report,
                path => _adminStorageRepository.UpdateReceiptPathAsync(row.ReceiptId, row.OrganizationId, path));
        }

        foreach (var row in drafts)
        {
            if (string.IsNullOrWhiteSpace(row.ReceiptPath))
                continue;
            await MoveOneAsync(result, "receipt draft " + row.ReceiptId, row.OrganizationId, row.OfficeName, "receipts", null, row.ReceiptPath, 2048, ImageType.Receipts, report,
                path => _adminStorageRepository.UpdateReceiptDraftPathAsync(row.ReceiptId, row.OrganizationId, path));
        }
        receipts = await _adminStorageRepository.GetReceiptImagesAsync();
        drafts = await _adminStorageRepository.GetReceiptDraftImagesAsync();
        var receiptSlots = receipts.Where(row => string.IsNullOrWhiteSpace(row.ReceiptPath)).Select(row => new EmptyImageSlot("receipt " + row.ReceiptId, row.OrganizationId, row.OfficeName, "receipts", null, 2048, path => _adminStorageRepository.UpdateReceiptPathAsync(row.ReceiptId, row.OrganizationId, path)));
        var draftSlots = drafts.Where(row => string.IsNullOrWhiteSpace(row.ReceiptPath)).Select(row => new EmptyImageSlot("receipt draft " + row.ReceiptId, row.OrganizationId, row.OfficeName, "receipts", null, 2048, path => _adminStorageRepository.UpdateReceiptDraftPathAsync(row.ReceiptId, row.OrganizationId, path)));
        await ReattachEmptyImagesAsync(result, report, receiptSlots.Concat(draftSlots).ToList(), receipts.Select(row => row.ReceiptPath).Concat(drafts.Select(row => row.ReceiptPath)));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairApplianceDecalsAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetApplianceDecalsAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.DecalPath))
                continue;
            await MoveOneAsync(result, "appliance " + row.ApplianceId, row.OrganizationId, row.OfficeName, "appliancedecal", row.PropertyCode, row.DecalPath, 2500, ImageType.ApplianceDecal, report,
                path => _adminStorageRepository.UpdateApplianceDecalPathAsync(row.ApplianceId, path));
        }
        rows = await _adminStorageRepository.GetApplianceDecalsAsync();
        await ReattachEmptyImagesAsync(result, report, rows.Where(row => string.IsNullOrWhiteSpace(row.DecalPath)).Select(row => new EmptyImageSlot("appliance " + row.ApplianceId, row.OrganizationId, row.OfficeName, "appliancedecal", row.PropertyCode, 2500, path => _adminStorageRepository.UpdateApplianceDecalPathAsync(row.ApplianceId, path))).ToList(), rows.Select(row => row.DecalPath));
        return result;
    }

    public async Task<StorageRepairResultDto> RepairDenverDbaDocumentsAsync(Func<StorageRepairEventDto, Task>? report = null)
    {
        const string oldOffice = "denver dba avenuewest colorado";
        const string newOffice = "denver";
        var result = new StorageRepairResultDto();
        var rows = await _adminStorageRepository.GetDenverDbaDocumentsAsync();
        await ReportAsync(report, "start", "", "", "", rows.Count);
        foreach (var row in rows)
        {
            var maxLength = row.Source == "quote" ? 1000 : 2048;
            var fileName = FileNameOf(row.DocumentPath);
            StoredImageRelocation? relocation = null;
            try
            {
                relocation = await _fileService.MoveStoredFileOfficeAsync(row.DocumentPath, oldOffice, newOffice, maxLength);
                if (relocation.Missing)
                {
                    AddFailure(result, row.Source + " " + row.RecordId + ": file was not found");
                    await ReportAsync(report, "file", fileName, "failed", "file was not found");
                    continue;
                }

                if (relocation.AlreadyCorrect)
                {
                    result.AlreadyCorrect++;
                    await ReportAsync(report, "file", fileName, "already correct", "");
                    continue;
                }

                var saved = await _adminStorageRepository.UpdateDenverDocumentPathAsync(row.Source, row.RecordId, relocation.NewPath ?? "");
                if (saved < 1)
                    throw new InvalidOperationException("Database row was not updated.");

                result.Moved++;
                await ReportAsync(report, "file", fileName, relocation.FoundExisting ? "saved" : "moved", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageRepair] {Source} {RecordId} failed", row.Source, row.RecordId);
                if (relocation != null && !relocation.FoundExisting && !relocation.Missing && !string.IsNullOrWhiteSpace(relocation.NewPath) && relocation.Content.Length > 0)
                {
                    try
                    {
                        await _fileService.RestoreStoredImageAsync(relocation.OriginalPath, relocation.Content, relocation.ContentType);
                        await _fileService.DeleteDocumentAsync(Guid.Empty, newOffice, relocation.NewPath);
                    }
                    catch (Exception restoreEx)
                    {
                        _logger.LogError(restoreEx, "[StorageRepair] {Source} {RecordId} could not restore the original file", row.Source, row.RecordId);
                    }
                }

                AddFailure(result, row.Source + " " + row.RecordId + ": " + ex.Message);
                await ReportAsync(report, "file", fileName, "failed", ex.Message);
            }
        }

        return result;
    }

    public async Task<StorageRepairResultDto> RepairUserGuideImagesAsync(Guid modifiedBy, Func<StorageRepairEventDto, Task>? report = null)
    {
        var result = new StorageRepairResultDto();
        var guide = await _organizationRepository.GetUserGuideAsync();
        if (guide?.Sections == null || guide.Sections.Count == 0)
        {
            await ReportAsync(report, "start", "", "", "", 0);
            return result;
        }

        var sections = new Dictionary<string, string>(guide.Sections, StringComparer.OrdinalIgnoreCase);
        var paths = CollectGuidePaths(sections.Values);
        await ReportAsync(report, "start", "", "", "", paths.Count);
        foreach (var path in paths)
        {
            await MoveOneAsync(result, "user guide image", SystemConstants.SystemOrganizationId, null, "userguide", null, path, 2048, ImageType.UserGuide, report, async newPath =>
            {
                var snapshot = new Dictionary<string, string>(sections, StringComparer.OrdinalIgnoreCase);
                try
                {
                    foreach (var key in sections.Keys.ToList())
                        sections[key] = newPath == null ? RemoveGuideImage(sections[key], path) : sections[key].Replace(path, newPath, StringComparison.Ordinal);
                    guide.Sections = sections;
                    await _organizationRepository.UpsertUserGuideAsync(guide, modifiedBy);
                    return 1;
                }
                catch
                {
                    foreach (var pair in snapshot)
                        sections[pair.Key] = pair.Value;
                    guide.Sections = sections;
                    throw;
                }
            });
        }

        return result;
    }

    private async Task ReattachEmptyImagesAsync(StorageRepairResultDto result, Func<StorageRepairEventDto, Task>? report, IReadOnlyList<EmptyImageSlot> slots, IEnumerable<string?> currentPaths)
    {
        var used = new HashSet<string>(currentPaths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(PathKey), StringComparer.OrdinalIgnoreCase);
        foreach (var group in slots.GroupBy(slot => PathKey(slot.OrganizationId + "|" + slot.OfficeName + "|" + slot.Category + "|" + slot.PropertyCode)))
        {
            var pending = group.ToList();
            var first = pending[0];
            IReadOnlyList<string> urls;
            try
            {
                urls = await _fileService.ListImageUrlsAsync(first.OrganizationId, first.OfficeName, first.Category, first.PropertyCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StorageRepair] {Label} could not list the new folder", first.Label);
                AddFailure(result, first.Label + ": the new folder could not be read");
                await ReportAsync(report, "file", first.Label, "failed", "the new folder could not be read");
                continue;
            }

            var free = urls.Where(url => !used.Contains(PathKey(url))).ToList();
            if (free.Count == 1 && pending.Count == 1)
            {
                if (free[0].Length > first.MaxPathLength)
                {
                    AddFailure(result, first.Label + ": new path is longer than the database column");
                    await ReportAsync(report, "file", FileNameOf(free[0]), "failed", "new path is longer than the database column");
                    continue;
                }

                var saved = await first.Save(free[0]);
                if (saved < 1)
                {
                    AddFailure(result, first.Label + ": database row was not updated");
                    await ReportAsync(report, "file", FileNameOf(free[0]), "failed", "database row was not updated");
                    continue;
                }

                used.Add(PathKey(free[0]));
                result.Moved++;
                await ReportAsync(report, "file", FileNameOf(free[0]), "saved", "");
                continue;
            }

            if (free.Count > 0)
                await ReportAsync(report, "file", first.Label, "not re-added", free.Count + " files for " + pending.Count + " records");
        }
    }

    private static string PathKey(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        var value = path.Trim().TrimEnd('/');
        var marker = ".blob.core.windows.net/";
        var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            value = value[(index + marker.Length)..];
            var slash = value.IndexOf('/');
            if (slash >= 0)
                value = value[(slash + 1)..];
        }

        return Uri.UnescapeDataString(value).ToLowerInvariant();
    }

    private sealed class EmptyImageSlot
    {
        public EmptyImageSlot(string label, Guid organizationId, string? officeName, string category, string? propertyCode, int maxPathLength, Func<string, Task<int>> save)
        {
            Label = label;
            OrganizationId = organizationId;
            OfficeName = officeName;
            Category = category;
            PropertyCode = propertyCode;
            MaxPathLength = maxPathLength;
            Save = save;
        }

        public string Label { get; }
        public Guid OrganizationId { get; }
        public string? OfficeName { get; }
        public string Category { get; }
        public string? PropertyCode { get; }
        public int MaxPathLength { get; }
        public Func<string, Task<int>> Save { get; }
    }

    private async Task<bool> MoveOneAsync(
        StorageRepairResultDto result,
        string label,
        Guid organizationId,
        string? officeName,
        string category,
        string? propertyCode,
        string currentPath,
        int maxPathLength,
        ImageType imageType,
        Func<StorageRepairEventDto, Task>? report,
        Func<string?, Task<int>> updatePath)
    {
        var fileName = FileNameOf(currentPath);
        StoredImageRelocation? relocation = null;
        try
        {
            relocation = await _fileService.RelocateStoredImageAsync(currentPath, organizationId, officeName, category, propertyCode, maxPathLength);
            if (relocation.FoundExisting)
            {
                var saved = await updatePath(relocation.NewPath);
                if (saved < 1)
                    throw new InvalidOperationException("Database row was not updated.");

                result.Moved++;
                await ReportAsync(report, "file", fileName, "saved", "");
                return true;
            }

            if (relocation.Missing)
            {
                var cleared = await updatePath(null);
                if (cleared < 1)
                    throw new InvalidOperationException("Database row was not updated.");

                result.Removed++;
                await ReportAsync(report, "file", fileName, "removed", "");
                return false;
            }

            if (relocation.AlreadyCorrect || string.IsNullOrWhiteSpace(relocation.NewPath))
            {
                result.AlreadyCorrect++;
                await ReportAsync(report, "file", fileName, "already correct", "");
                return false;
            }

            var updated = await updatePath(relocation.NewPath);
            if (updated < 1)
                throw new InvalidOperationException("Database row was not updated.");

            result.Moved++;
            await ReportAsync(report, "file", fileName, "moved", "");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StorageRepair] {Label} failed", label);
            if (relocation != null && !relocation.FoundExisting && !relocation.AlreadyCorrect && !string.IsNullOrWhiteSpace(relocation.NewPath) && relocation.Content.Length > 0 && !string.Equals(relocation.NewPath, relocation.OriginalPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _fileService.RestoreStoredImageAsync(relocation.OriginalPath, relocation.Content, relocation.ContentType);
                    await _fileService.DeleteImageAsync(organizationId, officeName, relocation.NewPath, imageType);
                }
                catch (Exception restoreEx)
                {
                    _logger.LogError(restoreEx, "[StorageRepair] {Label} could not restore the original file. New path {NewPath}", label, relocation.NewPath);
                    var restoreMessage = "move failed and the original file could not be restored. New file is at " + relocation.NewPath;
                    AddFailure(result, label + ": " + restoreMessage);
                    await ReportAsync(report, "file", fileName, "failed", restoreMessage);
                    return false;
                }
            }

            AddFailure(result, label + ": " + ex.Message);
            await ReportAsync(report, "file", fileName, "failed", ex.Message);
            return false;
        }
    }

    private static async Task ReportAsync(Func<StorageRepairEventDto, Task>? report, string kind, string file, string status, string detail, int total = 0)
    {
        if (report == null)
            return;

        try
        {
            await report(new StorageRepairEventDto { Kind = kind, File = file, Status = status, Detail = detail, Total = total });
        }
        catch (Exception)
        {
            // A dropped progress stream must not undo a file that already moved.
        }
    }

    private static string RemoveGuideImage(string html, string path)
    {
        if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(path))
            return html;

        return Regex.Replace(html, "<img\\b[^>]*" + Regex.Escape(path) + "[^>]*>", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string FileNameOf(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "(no file name)";

        var value = path.Trim();
        var query = value.IndexOf('?', StringComparison.Ordinal);
        if (query >= 0)
            value = value[..query];

        value = value.TrimEnd('/');
        var slash = value.LastIndexOf('/');
        var name = slash >= 0 && slash < value.Length - 1 ? value[(slash + 1)..] : value;
        return Uri.UnescapeDataString(name);
    }

    private static void AddFailure(StorageRepairResultDto result, string message)
    {
        result.Failed++;
        if (result.Errors.Count < ErrorLimit)
            result.Errors.Add(message);
    }

    private static List<string> CollectGuidePaths(IEnumerable<string> htmlSections)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var html in htmlSections)
        {
            if (string.IsNullOrWhiteSpace(html))
                continue;

            foreach (Match match in GuidePathRegex.Matches(html))
            {
                var path = match.Groups["path"].Value;
                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path);
            }
        }
        return paths.ToList();
    }
}
