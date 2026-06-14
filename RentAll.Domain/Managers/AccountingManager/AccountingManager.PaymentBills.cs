using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Bills
    public async Task<BillPayment> ApplyPaymentToBillsAsync(List<int> billIds, Guid organizationId, string offices, int chartOfAccountId,
        string description, decimal amountPaid, DateOnly paymentDate, PaymentType paymentType, Guid currentUser)
    {
        var bills = new List<Receipt>();
        foreach (var billId in billIds)
        {
            var bill = await _maintenanceRepository.GetReceiptByIdAsync(billId, organizationId);
            if (bill == null)
                throw new Exception("Invalid Bill");

            bills.Add(bill);
        }

        // Order bills by due date from the oldest to the newest
        bills = bills.Where(b => b.IsActive).OrderBy(b => b.DueDate).ThenBy(b => b.ReceiptId).ToList();

        var availableAmount = amountPaid;
        var paymentApplications = new List<BillPaymentApplication>();
        for (var billIndex = 0; billIndex < bills.Count && availableAmount != 0; billIndex++)
        {
            var bill = bills[billIndex];
            var isLastBill = billIndex == bills.Count - 1;
            decimal amountForBill;

            if (availableAmount > 0 && !isLastBill)
            {
                // For positive multi-bill runs, fill current due first, then carry remainder.
                var remainingBalance = bill.Amount - bill.PaidAmount;
                if (remainingBalance <= 0)
                    continue;

                amountForBill = Math.Min(availableAmount, remainingBalance);
            }
            else
            {
                // For single-bill runs, last bill in a multi-run, and all negative adjustments:
                // apply as entered so receipt math can naturally go negative/overpaid.
                amountForBill = availableAmount;
            }

            if (amountForBill == 0)
                continue;

            bill.PaidAmount += amountForBill;
            bill.PaidDate = DateOnly.FromDateTime(DateTime.Today);
            bill.PaymentTypeId = (int)paymentType;
            bill.CheckPrinted = false;
            bill.ModifiedBy = currentUser;
            bill.ModifiedOn = DateTimeOffset.UtcNow;

            availableAmount -= amountForBill;
            var updatedBill = await _maintenanceRepository.UpdateReceiptAsync(bill);
            bills[billIndex] = updatedBill;
            paymentApplications.Add(new BillPaymentApplication
            {
                Bill = updatedBill,
                AmountApplied = amountForBill,
                PaymentDate = paymentDate,
                ChartOfAccountId = chartOfAccountId,
                Description = description,
                PaymentSequence = await GetNextBillPaymentSequenceAsync(updatedBill)
            });
        }

        return new BillPayment
        {
            Bills = bills,
            PaymentApplications = paymentApplications
        };
    }
    #endregion
}
