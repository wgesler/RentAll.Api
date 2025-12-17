using RentAll.Domain.Models.Colors;

namespace RentAll.Api.Dtos.Colors;

public class UpdateColorDto
{
	public int ColorId { get; set; }
	public Guid OrganizationId { get; set; }
	public int ReservationStatusId { get; set; }
	public string Color { get; set; } = string.Empty;

	public Colour ToModel(UpdateColorDto c)
	{
		return new Colour
		{
			ColorId = c.ColorId,
			OrganizationId = c.OrganizationId,
			ReservationStatusId = c.ReservationStatusId,
			Color = c.Color
		};
	}
}

