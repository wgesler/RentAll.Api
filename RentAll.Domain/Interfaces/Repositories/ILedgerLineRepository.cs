using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILedgerLineRepository
{
    #region Selects
    Task<LedgerLine?> GetByIdAsync(Guid ledgerLineId);
    #endregion

    #region Creates
    Task<LedgerLine> CreateAsync(LedgerLine ledgerLine);
    #endregion

    #region Updates
    Task<LedgerLine> UpdateByIdAsync(LedgerLine ledgerLine);
    #endregion

    #region Deletes
    Task DeleteLedgerLineByIdAsync(Guid ledgerLineId);
    #endregion
}
