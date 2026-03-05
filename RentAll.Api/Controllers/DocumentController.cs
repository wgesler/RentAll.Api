using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/document")]
    [Authorize]
    public partial class DocumentController : BaseController
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IPhotoRepository _photoRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IFileService _fileService;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentRepository documentRepository,
            IPhotoRepository photoRepository,
            IOrganizationRepository organizationRepository,
            IFileService fileService,
            IPdfGenerationService pdfGenerationService,
            ILogger<DocumentController> logger)
        {
            _documentRepository = documentRepository;
            _photoRepository = photoRepository;
            _organizationRepository = organizationRepository;
            _fileService = fileService;
            _pdfGenerationService = pdfGenerationService;
            _logger = logger;
        }

        private string? GetOfficeName(int? officeId)
        {
            if (!officeId.HasValue)
                return null;
            var office = _organizationRepository.GetOfficeByIdAsync(officeId.Value, CurrentOrganizationId).GetAwaiter().GetResult();
            return office?.Name;
        }
    }
}

