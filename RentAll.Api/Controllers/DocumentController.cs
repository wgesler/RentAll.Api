using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("document")]
	[Authorize]
	public partial class DocumentController : BaseController
	{
		private readonly IDocumentRepository _documentRepository;
		private readonly IFileService _fileService;
		private readonly ILogger<DocumentController> _logger;

		public DocumentController(
			IDocumentRepository documentRepository,
			IFileService fileService,
			ILogger<DocumentController> logger)
		{
			_documentRepository = documentRepository;
			_fileService = fileService;
			_logger = logger;
		}
	}
}

