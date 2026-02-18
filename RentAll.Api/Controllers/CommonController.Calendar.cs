using Microsoft.AspNetCore.Mvc;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers
{
	public partial class CommonController
	{
		/// <summary>
		/// Get iCal feed for a property.
		/// </summary>
		/// <param name="propertyId">Property ID</param>
		/// <param name="organizationId">Organization ID</param>
		/// <param name="token">Calendar token</param>
		/// <returns>iCal text/calendar response</returns>
		[HttpGet("calendar/property/{propertyId}.ics")]
		public async Task<IActionResult> GetPropertyCalendar(Guid propertyId, [FromQuery] Guid organizationId, [FromQuery] string token)
		{
			if (propertyId == Guid.Empty)
				return BadRequest("Property ID is required");
			if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(token))
				return NotFound("Calendar not found");

			try
			{
				if (!_calendarService.IsValidPropertyCalendarToken(propertyId, organizationId, token))
					return NotFound("Calendar not found");

				var reservations = await _reservationRepository.GetByPropertyIdAsync(propertyId, organizationId);
				var calendarReservations = reservations
					.Where(r => r.IsActive)
					.Where(r => r.ReservationStatus != ReservationStatus.PreBooking)
					.Where(r => r.DepartureDate > DateTimeOffset.UtcNow.AddYears(-1))
					.ToList();

				var ics = _calendarService.BuildPropertyCalendar(propertyId, calendarReservations, DateTimeOffset.UtcNow);
				Response.Headers.CacheControl = "public,max-age=300";
				return Content(ics, "text/calendar; charset=utf-8");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property calendar for property: {PropertyId}", propertyId);
				return ServerError("An error occurred while retrieving property calendar");
			}
		}
	}
}
