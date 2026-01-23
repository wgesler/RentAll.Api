using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Interfaces.Managers;
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
		private readonly IReservationRepository _reservationRepository;
		private readonly IAccountingManager _accountingManager;
		private readonly ILogger<AccountingController> _logger;

		public AccountingController(
			IInvoiceRepository invoiceRepository,
			ILedgerLineRepository ledgerLineRepository,
			IReservationRepository reservationRepository,
			IAccountingManager accountingManager,
			ILogger<AccountingController> logger)
		{
			_invoiceRepository = invoiceRepository;
			_ledgerLineRepository = ledgerLineRepository;
			_reservationRepository = reservationRepository;
			_accountingManager = accountingManager;
			_logger = logger;
		}
	}
}
