using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/admin/receipts")]
[Authorize]
public class AdminReceiptsController : BaseController
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IAdminStorageRepository _adminStorageRepository;
    private readonly IFileService _fileService;

    public AdminReceiptsController(
        IMaintenanceRepository maintenanceRepository,
        IOrganizationRepository organizationRepository,
        IAdminStorageRepository adminStorageRepository,
        IFileService fileService)
    {
        _maintenanceRepository = maintenanceRepository;
        _organizationRepository = organizationRepository;
        _adminStorageRepository = adminStorageRepository;
        _fileService = fileService;
    }

    [HttpGet("unmatched")]
    public async Task<IActionResult> GetUnmatched([FromQuery] Guid? organizationId, [FromQuery] int? officeId, [FromQuery] DateOnly? start, [FromQuery] DateOnly? end)
    {
        if (!IsSuperAdmin())
            return Unauthorized("Only SuperAdmin can match receipts.");

        var (organization, offices) = await ResolveOfficesAsync(organizationId, officeId);
        if (offices.Count == 0)
            return Ok(new UnmatchedReceiptsResponse());

        var officeIds = string.Join(",", offices.Select(office => office.OfficeId));
        var receipts = await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organization,
            OfficeIds = officeIds,
            IsActive = true,
            StartDate = start,
            EndDate = end
        });
        var missing = receipts
            .OrderByDescending(receipt => receipt.ReceiptDate)
            .ThenBy(receipt => receipt.ReceiptCode)
            .Select(receipt => new MissingReceiptDto
            {
                ReceiptId = receipt.ReceiptId,
                OfficeId = receipt.OfficeId,
                OfficeName = receipt.OfficeName,
                ReceiptCode = receipt.ReceiptCode,
                ReceiptDate = receipt.ReceiptDate,
                Amount = receipt.Amount,
                VendorName = receipt.VendorName,
                Description = receipt.Description,
                ReceiptPath = receipt.ReceiptPath
            })
            .ToList();

        return Ok(new UnmatchedReceiptsResponse { Receipts = missing });
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles([FromQuery] Guid? organizationId, [FromQuery] int? officeId)
    {
        if (!IsSuperAdmin())
            return Unauthorized("Only SuperAdmin can match receipts.");

        var (organization, offices) = await ResolveOfficesAsync(organizationId, officeId);
        var images = new List<UnassignedReceiptImageDto>();
        foreach (var office in offices)
        {
            var urls = await _fileService.ListRootImagesAsync(organization, office.Name, "receipts", null, null);
            foreach (var url in urls)
            {
                var fileName = url.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
                images.Add(new UnassignedReceiptImageDto
                {
                    Path = url,
                    FileName = fileName,
                    ContentType = ContentTypeFromName(fileName)
                });
            }
        }

        return Ok(images);
    }

    private async Task<(Guid OrganizationId, List<Office> Offices)> ResolveOfficesAsync(Guid? organizationId, int? officeId)
    {
        var organization = IsSuperAdmin() && organizationId.HasValue && organizationId.Value != Guid.Empty
            ? organizationId.Value
            : CurrentOrganizationId;
        var offices = (await _organizationRepository.GetOfficesByOrganizationIdAsync(organization)).ToList();
        if (officeId.HasValue)
            offices = offices.Where(office => office.OfficeId == officeId.Value).ToList();
        return (organization, offices);
    }

    [HttpGet("image")]
    public async Task<IActionResult> GetImage([FromQuery] Guid organizationId, [FromQuery] string path)
    {
        if (!IsSuperAdmin())
            return Unauthorized("Only SuperAdmin can match receipts.");
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("File path is required.");

        var organization = organizationId != Guid.Empty ? organizationId : CurrentOrganizationId;
        var details = await _fileService.GetImageDetailsAsync(organization, null, path, ImageType.Receipts);
        if (details == null || string.IsNullOrWhiteSpace(details.DataUrl))
            return NotFound("That file was not found.");

        return Ok(new { fileName = details.FileName, contentType = details.ContentType, dataUrl = details.DataUrl });
    }

    private static string ContentTypeFromName(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    [HttpPost("match")]
    public async Task<IActionResult> Match([FromBody] MatchReceiptFileDto dto)
    {
        if (!IsSuperAdmin())
            return Unauthorized("Only SuperAdmin can match receipts.");
        if (dto == null || dto.ReceiptId == Guid.Empty || string.IsNullOrWhiteSpace(dto.SourcePath))
            return BadRequest("Receipt and file are required.");

        var organization = dto.OrganizationId != Guid.Empty ? dto.OrganizationId : CurrentOrganizationId;
        var receipt = await _maintenanceRepository.GetReceiptByIdAsync(dto.ReceiptId, organization);
        if (receipt == null)
            return NotFound("Receipt not found.");

        var office = await _organizationRepository.GetOfficeByIdAsync(receipt.OfficeId, organization);
        var moved = await _fileService.RelocateStoredImageAsync(dto.SourcePath.Trim(), organization, office?.Name, "receipts", receipt.ReceiptCode, 2048);
        if (moved.Missing || string.IsNullOrWhiteSpace(moved.NewPath))
            return BadRequest("That file was not found.");

        var previousPath = receipt.ReceiptPath;
        var updated = await _adminStorageRepository.UpdateReceiptPathAsync(receipt.ReceiptId, organization, moved.NewPath);
        if (updated < 1)
            return StatusCode(StatusCodes.Status500InternalServerError, "The receipt path was not saved.");

        if (!string.IsNullOrWhiteSpace(previousPath)
            && !string.Equals(previousPath, moved.NewPath, StringComparison.OrdinalIgnoreCase))
        {
            await _fileService.DeleteStoredFileAsync(previousPath);
        }

        return Ok(new { receiptPath = moved.NewPath });
    }

    public sealed class UnmatchedReceiptsResponse
    {
        public List<MissingReceiptDto> Receipts { get; set; } = new();
        public List<UnassignedReceiptImageDto> Images { get; set; } = new();
    }

    public sealed class MissingReceiptDto
    {
        public Guid ReceiptId { get; set; }
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public string ReceiptCode { get; set; } = string.Empty;
        public DateOnly ReceiptDate { get; set; }
        public decimal Amount { get; set; }
        public string? VendorName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReceiptPath { get; set; }
    }

    public sealed class UnassignedReceiptImageDto
    {
        public string Path { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string? DataUrl { get; set; }
    }

    public sealed class MatchReceiptFileDto
    {
        public Guid OrganizationId { get; set; }
        public Guid ReceiptId { get; set; }
        public string SourcePath { get; set; } = string.Empty;
    }
}
