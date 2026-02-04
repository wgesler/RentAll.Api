namespace RentAll.Api.Dtos.Invoices;

public class InvoicePaymentRequestDto
{
	public int CostCodeId { get; set; }
	public string Description { get; set; } = string.Empty;
	public decimal Amount { get; set; }
	public List<Guid> Invoices { get; set; } = new List<Guid>();


	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (CostCodeId < 0)
			return (false, "CostCodeId is required");

		if (Amount <= 0)
			return (false, "No payment submitted");

		if (Invoices.Count <= 0)
			return (false, "No invoices submitted for payment");

		return (true, null);
	}
}


