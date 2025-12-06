using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Common;

public class StateResponseDto
{
	public string Code { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;

	public StateResponseDto(State state)
	{
		Code = state.Code;
		Name = state.Name;
	}
}

