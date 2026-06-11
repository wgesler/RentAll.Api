using Microsoft.AspNetCore.Authorization;

namespace RentAll.Api.Controllers
{
    public partial class CommonController
    {
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
                var response = Content(ics, "text/calendar; charset=utf-8");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property calendar for property: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving property calendar");
            }
        }

        [Authorize]
        [HttpPost("calendar/import")]
        public async Task<IActionResult> ImportExternalCalendar([FromBody] CalendarImportRequestDto dto, CancellationToken cancellationToken)
        {
            if (dto == null)
                return BadRequest("Calendar import request is required");

            var validation = dto.IsValid();
            if (!validation.IsValid)
                return BadRequest(validation.ErrorMessage!);

            try
            {
                var importedEvents = await _calendarService.ImportExternalCalendarAsync(dto.ExternalCalendarUrl, cancellationToken);
                var response = CalendarImportResponseDto.FromImportedEvents(dto.ExternalCalendarUrl, importedEvents);
                return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Calendar import request failed for URL: {ExternalCalendarUrl}", dto.ExternalCalendarUrl);
                return BadRequest("Unable to fetch external calendar URL.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing external calendar for URL: {ExternalCalendarUrl}", dto.ExternalCalendarUrl);
                return ServerError("An error occurred while importing external calendar");
            }
        }
    }
}
