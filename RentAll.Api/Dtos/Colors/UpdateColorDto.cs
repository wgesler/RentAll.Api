using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Xml.Linq;

namespace RentAll.Api.Dtos.Colors;

public class UpdateColorDto
{
	public int ColorId { get; set; }
	public Guid OrganizationId { get; set; }
	public int ReservationStatusId { get; set; }
	public string Color { get; set; } = string.Empty;

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (ColorId <= 0)
			return (false, "Color ID is required");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (ReservationStatusId < 0)
			return (false, "ReservationStatusId is required");

		if (!Enum.IsDefined(typeof(ReservationStatus), ReservationStatusId))
			return (false, $"Invalid ReservationStatusId value: {ReservationStatusId}");

		if (string.IsNullOrWhiteSpace(Color))
			return (false, "Color value is required");

		// Remove # prefix if present for validation
		var colorValue = Color.TrimStart('#');

		// Validate Color format (should be 6 hex characters)
		if (colorValue.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(colorValue, @"^[0-9A-Fa-f]{6}$"))
			return (false, "Color must be a 6-character hexadecimal value (e.g., FF0000 or #FF0000)");

		return (true, null);
	}

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

