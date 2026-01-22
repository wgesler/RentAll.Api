using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
	[ApiController]
	[Route("accounting")]
	[Authorize]
	public partial class AccountingController : BaseController
	{
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly ILedgerLineRepository _ledgerLineRepository;
		private readonly ILogger<AccountingController> _logger;

		public AccountingController(
			IInvoiceRepository invoiceRepository,
			ILedgerLineRepository ledgerLineRepository,
			ILogger<AccountingController> logger)
		{
			_invoiceRepository = invoiceRepository;
			_ledgerLineRepository = ledgerLineRepository;
			_logger = logger;
		}
	}
}
