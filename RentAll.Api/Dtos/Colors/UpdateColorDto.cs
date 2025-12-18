using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Colors;

public class UpdateColorDto
{
	public int ColorId { get; set; }
	public Guid OrganizationId { get; set; }
	public int ReservationStatusId { get; set; }
	public string Color { get; set; } = string.Empty;

	public Colour ToModel()
	{
		return new Colour
		{
			ColorId = ColorId,
			OrganizationId = OrganizationId,
			ReservationStatusId = ReservationStatusId,
			Color = Color
		};
	}
}

