using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAccountingRepository
{
    #region Invoices
    Task<IEnumerable<Invoice>> GetInvoicesAsync(InvoiceGetCriteria criteria);
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid organizationId);

    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateByIdAsync(Invoice invoice);
    Task<int> DeactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy);
    Task<int> ReactivateInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId, Guid modifiedBy);
    Task DeleteInvoiceByIdAsync(Guid invoiceId, Guid organizationId);
    #endregion

    #region CostCodes
    Task<List<CostCode>> GetCostCodesByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<List<CostCode>> GetCostCodesByOfficeIdAsync(Guid organizationId, int officeId);
    Task<CostCode?> GetCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId);
    Task<CostCode?> GetByCostCodeAsync(string costCode, Guid organizationId, int officeId);
    Task<CostCode?> GetByDescriptionAsync(string description, Guid organizationId, int officeId);
    Task<bool> ExistsByCostCodeAsync(string costCode, Guid organizationI, int officeId);

    Task<CostCode> CreateAsync(CostCode costCode);
    Task<CostCode> UpdateByIdAsync(CostCode costCode);
    Task DeleteCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId);
    #endregion

    #region BankCards
    Task<List<BankCard>> GetBankCardsByOfficeIdAsync(Guid organizationId, int officeId);
    Task<List<BankCard>> GetBankCardsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<BankCard?> GetBankCardByIdAsync(int bankCardId, Guid organizationId, int officeId);
    Task<BankCard> CreateAsync(BankCard bankCard, byte[] encryptedCardNumber);
    Task<BankCard> UpdateByIdAsync(BankCard bankCard, byte[] encryptedCardNumber);
    Task DeleteBankCardByIdAsync(int bankCardId, Guid organizationId, int officeId);
    #endregion

    #region ChartOfAccounts
    Task<List<ChartOfAccount>> GetChartOfAccountsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<List<ChartOfAccount>> GetChartOfAccountsByOfficeIdAsync(Guid organizationId, int officeId);
    Task<List<ChartOfAccount>> GetChartOfAccountsByOrganizationIdAsync(Guid organizationId);
    Task<ChartOfAccount?> GetChartOfAccountByIdAsync(Guid organizationId, int officeId, int accountId);
    Task<ChartOfAccount?> GetChartOfAccountByAccountNoAsync(Guid organizationId, int officeId, string accountNo);
    Task<bool> ExistsChartOfAccountByAccountIdAsync(Guid organizationId, int officeId, int accountId);
    Task<bool> ExistsChartOfAccountByAccountNoAsync(Guid organizationId, int officeId, string accountNo, int? excludeAccountId = null);

    Task<ChartOfAccount> CreateAsync(ChartOfAccount chartOfAccount);
    Task<ChartOfAccount> UpdateChartOfAccountByIdAsync(ChartOfAccount chartOfAccount);
    Task DeleteChartOfAccountByIdAsync(Guid organizationId, int officeId, int accountId);
    #endregion

    #region CheckHtml
    Task<CheckHtml?> GetCheckHtmlByScopeAsync(Guid organizationId, int? officeId);
    Task<CheckHtml?> GetCheckHtmlByIdAsync(Guid checkHtmlId);
    Task<List<CheckHtml>> GetCheckHtmlAllAsync(Guid? organizationId = null);
    Task<CheckHtml> CreateCheckHtmlAsync(CheckHtml checkHtml);
    Task<CheckHtml> UpdateCheckHtmlByIdAsync(CheckHtml checkHtml);
    Task DeleteCheckHtmlByIdAsync(Guid checkHtmlId);
    #endregion
}
