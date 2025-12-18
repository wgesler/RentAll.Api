using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Colors;

public class ColorResponseDto
{
	public int ColorId { get; set; }
	public Guid OrganizationId { get; set; }
	public int ReservationStatusId { get; set; }
	public string Color { get; set; } = string.Empty;

	public ColorResponseDto()
	{
	}

	public ColorResponseDto(Colour c)
	{
		ColorId = c.ColorId;
		OrganizationId = c.OrganizationId;
		ReservationStatusId = c.ReservationStatusId;
		Color = c.Color;
	}
}

