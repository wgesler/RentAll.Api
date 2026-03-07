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
        private readonly IFileAttachmentHelper _fileAttachmentHelper;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentRepository documentRepository,
            IPhotoRepository photoRepository,
            IOrganizationRepository organizationRepository,
            IFileService fileService,
            IFileAttachmentHelper fileAttachmentHelper,
            IPdfGenerationService pdfGenerationService,
            ILogger<DocumentController> logger)
        {
            _documentRepository = documentRepository;
            _photoRepository = photoRepository;
            _organizationRepository = organizationRepository;
            _fileService = fileService;
            _fileAttachmentHelper = fileAttachmentHelper;
            _pdfGenerationService = pdfGenerationService;
            _logger = logger;
        }

        private async Task<string?> GetOfficeNameAsync(int? officeId)
        {
            if (!officeId.HasValue)
                return null;
            var office = await _organizationRepository.GetOfficeByIdAsync(officeId.Value, CurrentOrganizationId);
            return office?.Name;
        }
    }
}

