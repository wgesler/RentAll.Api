namespace RentAll.Api.Dtos.Common;

public class ErrorResponseDto
{
	public string Controller { get; set; } = string.Empty;
	public string HttpMethod { get; set; } = string.Empty;
	public string ActionName { get; set; } = string.Empty;
	public string Route { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
}

