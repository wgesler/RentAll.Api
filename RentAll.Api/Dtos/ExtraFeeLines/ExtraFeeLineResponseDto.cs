using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.ExtraFeeLines;

public class ExtraFeeLineResponseDto
{
	public int ExtraFeeLineId { get; set; }
	public Guid? ReservationId { get; set; }
	public string FeeDescription { get; set; } = string.Empty;
	public decimal FeeAmount { get; set; }
	public int FeeFrequencyId { get; set; }
	public int CostCodeId { get; set; }

	public ExtraFeeLineResponseDto(ExtraFeeLine extraFeeLine)
	{
		ExtraFeeLineId = extraFeeLine.ExtraFeeLineId;
		ReservationId = extraFeeLine.ReservationId;
		FeeDescription = extraFeeLine.FeeDescription;
		FeeAmount = extraFeeLine.FeeAmount;
		FeeFrequencyId = (int)extraFeeLine.FeeFrequency;
		CostCodeId = extraFeeLine.CostCodeId;
	}
}
