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
    private readonly IUserRepository _userRepository;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IDocuSignService _docuSignService;
    private readonly ILogger<ESignatureController> _logger;

    public ESignatureController(
        IUserRepository userRepository,
        IPdfGenerationService pdfGenerationService,
        IDocuSignService docuSignService,
        ILogger<ESignatureController> logger)
    {
        _userRepository = userRepository;
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
            var signers = dto.ToSigners();
            var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(dto.HtmlContent);
            var fileName = string.IsNullOrWhiteSpace(dto.FileName) ? "document.pdf" : dto.FileName;

            var currentUser = await _userRepository.GetUserByIdsAsync(CurrentUser, CurrentOrganizationId);
            var docuSignUserId = currentUser?.DocuSignUserId;

            var result = await _docuSignService.SendEnvelopeAsync(
                pdfBytes,
                fileName,
                dto.Subject,
                signers,
                dto.ReturnUrl,
                dto.SenderEmail,
                dto.SenderName,
                docuSignUserId,
                dto.ApiAccountId,
                dto.BaseUri);

            return Ok(new SendDocumentForSignatureResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document for signature");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
