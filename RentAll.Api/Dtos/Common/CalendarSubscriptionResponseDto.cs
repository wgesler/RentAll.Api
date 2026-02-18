namespace RentAll.Api.Dtos.Common;

public class CalendarSubscriptionResponseDto
{
	public Guid PropertyId { get; set; }
	public Guid OrganizationId { get; set; }
	public string SubscriptionUrl { get; set; } = string.Empty;
}
