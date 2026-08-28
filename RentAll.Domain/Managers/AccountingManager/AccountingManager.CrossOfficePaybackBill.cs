using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task CreateCrossOfficePaybackBillAsync(Receipt receipt, BankCard bankCard, Guid currentUser)
    {
        receipt = await LoadReceiptWithSplitsAsync(receipt);

        await CreateCrossOfficeOfficeBillAsync(receipt, bankCard, receipt.OfficeId, bankCard.OfficeId, 1m, currentUser);
        await CreateCrossOfficeOfficeBillAsync(receipt, bankCard, bankCard.OfficeId, receipt.OfficeId, -1m, currentUser);
    }

    private async Task CreateCrossOfficeOfficeBillAsync(Receipt receipt, BankCard bankCard, int billOfficeId, int vendorOfficeId, decimal amountSign, Guid currentUser)
    {
        var vendorId = await ResolveInterOfficeVendorContactIdAsync(receipt.OrganizationId, billOfficeId, vendorOfficeId, currentUser);
        var vendorName = await GetOfficeNameAsync(receipt.OrganizationId, vendorOfficeId);
        var receiptCode = receipt.ReceiptCode.Trim();
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(receipt.OrganizationId, billOfficeId);
        var isOffendingOfficeBill = billOfficeId == receipt.OfficeId;
        var splitAccountId = isOffendingOfficeBill
            ? GetDefaultInterOfficeAccount(chartOfAccounts, billOfficeId, accountingOffice)
            : GetCreditCardAccountId(bankCard);
        var splitAccountName = isOffendingOfficeBill ? "Inter-Office" : "Visa";
        var splits = new List<ReceiptSplit>
        {
            new()
            {
                Amount = receipt.Amount * amountSign,
                Description = string.IsNullOrWhiteSpace(receipt.Description) ? receiptCode : receipt.Description,
                ReceiptTypeId = (int)ReceiptType.Company,
                ChartOfAccountId = splitAccountId,
                ChartOfAccountDisplayName = splitAccountName
            }
        };

        var existingBill = await FindCrossOfficePaybackBillAsync(receipt.OrganizationId, billOfficeId, receiptCode);
        if (existingBill != null)
        {
            existingBill.Amount = receipt.Amount * amountSign;
            existingBill.Description = receipt.Description;
            existingBill.VendorId = vendorId;
            existingBill.VendorName = vendorName;
            existingBill.PropertyIds = receipt.PropertyIds?.ToList() ?? new List<Guid>();
            existingBill.ReceiptDate = receipt.ReceiptDate;
            existingBill.DueDate = receipt.DueDate == default ? receipt.ReceiptDate : receipt.DueDate;
            existingBill.AccountingPeriod = receipt.AccountingPeriod;
            existingBill.ReceiptPath = receipt.ReceiptPath;
            existingBill.Splits = splits;
            existingBill.ModifiedBy = currentUser;
            await _maintenanceRepository.UpdateReceiptAsync(existingBill);
            await ReplaceJournalEntriesFromBillAsync(existingBill, currentUser);
            return;
        }

        var billCode = await _organizationManager.GenerateEntityCodeAsync(receipt.OrganizationId, EntityType.Receipt);
        if (string.IsNullOrWhiteSpace(billCode))
            throw new Exception("Unable to generate bill code for cross-office payback");

        var bill = new Receipt
        {
            OrganizationId = receipt.OrganizationId,
            OfficeId = billOfficeId,
            ReceiptCode = billCode.Trim(),
            PropertyIds = receipt.PropertyIds?.ToList() ?? new List<Guid>(),
            ReceiptDate = receipt.ReceiptDate,
            DueDate = receipt.DueDate == default ? receipt.ReceiptDate : receipt.DueDate,
            AccountingPeriod = receipt.AccountingPeriod,
            BillNumber = receiptCode,
            Amount = receipt.Amount * amountSign,
            Description = receipt.Description,
            BankCardId = null,
            VendorId = vendorId,
            VendorName = vendorName,
            PaidAmount = 0,
            Splits = splits,
            ReceiptPath = receipt.ReceiptPath,
            IsUtility = receipt.IsUtility,
            BusinessPrivate = receipt.BusinessPrivate,
            IsActive = true,
            CreatedBy = currentUser
        };

        var createdBill = await _maintenanceRepository.CreateReceiptAsync(bill);
        await CreateJournalEntryFromBillAsync(createdBill, currentUser);
    }

    private async Task<Receipt?> FindCrossOfficePaybackBillAsync(Guid organizationId, int billOfficeId, string sourceReceiptCode)
    {
        var bills = await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = billOfficeId.ToString(),
            ReceiptKind = ReceiptKind.Bill,
            IncludeInactive = false
        });

        return bills.FirstOrDefault(bill =>
            bill.BankCardId is not > 0 &&
            string.Equals(bill.BillNumber?.Trim(), sourceReceiptCode, StringComparison.OrdinalIgnoreCase));
    }

}
