using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILedgerLineRepository
{
	// Creates
	Task<LedgerLine> CreateAsync(LedgerLine ledgerLine);

	// Selects
	Task<LedgerLine?> GetByIdAsync(int ledgerLineId);

	// Updates
	Task<LedgerLine> UpdateByIdAsync(LedgerLine ledgerLine);

	// Deletes
	Task DeleteByIdAsync(int ledgerLineId);
}
