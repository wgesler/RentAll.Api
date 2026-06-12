using RentAll.Api.Dtos.Maintenances.Receipts;

namespace RentAll.Api.Dtos.Accounting.Bills;

public class BillPaymentResponseDto
{
    public List<ReceiptResponseDto> Bills { get; set; } = new List<ReceiptResponseDto>();

    public BillPaymentResponseDto(BillPayment payment)
    {
        Bills = payment.Bills.Select(bill => new ReceiptResponseDto(bill)).ToList();
    }
}
