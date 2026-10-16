using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using RentAll.Api.Dtos.Admin;
using RentAll.Api.Services;
using System.Text.Json;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/admin/storage")]
[Authorize]
public class AdminController : BaseController
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly StoragePathRepairService _storagePathRepairService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(StoragePathRepairService storagePathRepairService, ILogger<AdminController> logger)
    {
        _storagePathRepairService = storagePathRepairService;
        _logger = logger;
    }

    [HttpPost("office-folders")]
    public Task RepairOfficeFolders(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairHyphenOfficeFoldersAsync(report), cancellationToken);

    [HttpPost("listing-photos")]
    public Task RepairListingPhotos(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairListingPhotosAsync(report), cancellationToken);

    [HttpPost("inspection-photos")]
    public Task RepairInspectionPhotos(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairInspectionPhotosAsync(report), cancellationToken);

    [HttpPost("office-logos")]
    public Task RepairOfficeLogos(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairOfficeLogosAsync(report), cancellationToken);

    [HttpPost("organization-logos")]
    public Task RepairOrganizationLogos(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairOrganizationLogosAsync(report), cancellationToken);

    [HttpPost("profiles")]
    public Task RepairProfileImages(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairProfileImagesAsync(report), cancellationToken);

    [HttpPost("state-forms")]
    public Task RepairStateForms(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairStateFormsAsync(report), cancellationToken);

    [HttpPost("user-guide")]
    public Task RepairUserGuideImages(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairUserGuideImagesAsync(CurrentUser, report), cancellationToken);

    [HttpPost("receipts")]
    public Task RepairReceiptImages(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairReceiptImagesAsync(report), cancellationToken);

    [HttpPost("appliance-decals")]
    public Task RepairApplianceDecals(CancellationToken cancellationToken)
        => RepairStreamAsync(report => _storagePathRepairService.RepairApplianceDecalsAsync(report), cancellationToken);

    private async Task RepairStreamAsync(Func<Func<StorageRepairEventDto, Task>, Task<StorageRepairResultDto>> repair, CancellationToken cancellationToken)
    {
        if (!IsSuperAdmin())
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsync("Only SuperAdmin can repair stored files.", cancellationToken);
            return;
        }

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        var minRate = HttpContext.Features.Get<IHttpMinResponseDataRateFeature>();
        if (minRate != null)
            minRate.MinDataRate = null;

        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        await Response.StartAsync(cancellationToken);

        async Task Report(StorageRepairEventDto evt)
        {
            await Response.WriteAsync(JsonSerializer.Serialize(evt, JsonOptions) + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        try
        {
            var result = await repair(Report);
            await Report(new StorageRepairEventDto
            {
                Kind = "done",
                Moved = result.Moved,
                AlreadyCorrect = result.AlreadyCorrect,
                Removed = result.Removed,
                Failed = result.Failed,
                Errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[StorageRepair] Repair failed");
            try
            {
                await Report(new StorageRepairEventDto { Kind = "error", Detail = "The repair did not finish. Nothing that failed was left without its original file." });
            }
            catch (Exception)
            {
                // The client is already gone. The file move is not reversed from here.
            }
        }
    }
}
