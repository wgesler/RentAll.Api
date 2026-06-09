using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.ESignature;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/esignature")]
[Authorize]
public class ESignatureController : BaseController
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IDocuSignService _docuSignService;
    private readonly ILogger<ESignatureController> _logger;

    public ESignatureController(
        IOrganizationRepository organizationRepository,
        IPdfGenerationService pdfGenerationService,
        IDocuSignService docuSignService,
        ILogger<ESignatureController> logger)
    {
        _organizationRepository = organizationRepository;
        _pdfGenerationService = pdfGenerationService;
        _docuSignService = docuSignService;
        _logger = logger;
    }

    [HttpPost("send-for-signature")]
    public async Task<IActionResult> SendForSignature([FromBody] SendDocumentForSignatureDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Request data is required" });

        var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId, CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(new { message = errorMessage ?? "Invalid request data" });

        try
        {
            var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
            if (organization == null)
                return BadRequest(new { message = "Organization not found" });

            var signers = dto.ToSigners();
            var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(dto.HtmlContent);
            var fileName = string.IsNullOrWhiteSpace(dto.FileName) ? "document.pdf" : dto.FileName;

            var result = await _docuSignService.SendEnvelopeAsync(
                organization.Name,
                pdfBytes,
                fileName,
                dto.Subject,
                signers,
                dto.ReturnUrl,
                dto.SenderEmail,
                dto.SenderName,
                dto.UserId,
                dto.ApiAccountId);

            return Ok(new SendDocumentForSignatureResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document for signature");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
