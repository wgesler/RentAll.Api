using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Common;

namespace RentAll.Api.Controllers
{
    public partial class CommonController
    {
        /// <summary>
        /// Get iCal feed for a property.
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <param name="dto">Calendar URL request query values</param>
        /// <returns>iCal text/calendar response</returns>
        [HttpGet("calendar/property/{propertyId}.ics")]
        public async Task<IActionResult> GetPropertyCalendar(Guid propertyId, [FromQuery] CalendarUrlRequestDto dto)
        {
            var validation = dto.IsValid(propertyId);
            if (!validation.IsValid)
                return BadRequest(validation.ErrorMessage!);

            try
            {
                var ics = await _calendarManager.BuildPropertyCalendarFeedAsync(dto.PropertyId, dto.OrganizationId, dto.Token);
                if (ics == null)
                    return NotFound("Calendar not found");

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
