using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IInvoiceLedgerLineRepository
{
	// Creates
	Task<InvoiceLedgerLine> CreateAsync(InvoiceLedgerLine invoiceLedgerLine);

	// Selects
	Task<IEnumerable<InvoiceLedgerLine>> GetByInvoiceIdAsync(Guid invoiceId);
	Task<IEnumerable<InvoiceLedgerLine>> GetByLedgerLineIdAsync(int ledgerLineId);

	// Deletes
	Task DeleteAsync(Guid invoiceId, int ledgerLineId);
	Task DeleteByInvoiceIdAsync(Guid invoiceId);
	Task DeleteByLedgerLineIdAsync(int ledgerLineId);
}
