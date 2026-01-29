using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("accounting/office")]
	[Authorize]
	public partial class AccountingOfficeController : BaseController
	{
		private readonly IAccountingOfficeRepository _accountingOfficeRepository;
		private readonly IFileService _fileService;
		private readonly ILogger<AccountingOfficeController> _logger;

		public AccountingOfficeController(
			IAccountingOfficeRepository accountingOfficeRepository,
			IFileService fileService,
			ILogger<AccountingOfficeController> logger)
		{
			_accountingOfficeRepository = accountingOfficeRepository;
			_fileService = fileService;
			_logger = logger;
		}
	}
}
