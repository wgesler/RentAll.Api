using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Common;
using RentAll.Api.Dtos.Properties;

namespace RentAll.Api.Controllers
{
	public partial class PropertyController
	{
		/// <summary>
		/// Get all properties list
		/// </summary>
		/// <returns>List of properties</returns>
		[HttpGet("list")]
		public async Task<IActionResult> GetList()
		{
			try
			{
				// Get the property summary for the list of properties
				var list = await _propertyRepository.GetListByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
				var response = list.Select(p => new PropertyListResponseDto(p));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting properties list");
				return ServerError("An error occurred while retrieving properties list");
			}
		}


		/// <summary>
		/// Get properties by the current user's selection criteria
		/// </summary>
		/// <param name="userId">User Id</param>
		/// <returns>List of properties by user selection</returns>
		[HttpGet("user/{userId}")]
		public async Task<IActionResult> GetPropertiesByUserSelection(Guid userId)
		{
			if (CurrentUser == Guid.Empty || CurrentUser != userId)
				return Unauthorized();

			try
			{
				var properties = await _propertyRepository.GetListBySelectionCriteriaAsync(CurrentUser, CurrentOrganizationId, CurrentOfficeAccess);
				var response = properties.Select(p => new PropertyListResponseDto(p));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting properties by selection criteria for user: {UserId}", CurrentUser);
				return ServerError("An error occurred while retrieving properties");
			}
		}

		/// <summary>
		/// Get iCal subscription URL for a property.
		/// </summary>
		/// <param name="id">Property ID</param>
		/// <returns>Tokenized iCal subscription URL</returns>
		[HttpGet("{id}/calendar/subscription-url")]
		public IActionResult GetCalendarSubscriptionUrl(Guid id)
		{
			if (id == Guid.Empty)
				return BadRequest("Property ID is required");

			try
			{
				var organizationId = CurrentOrganizationId;
				if (organizationId == Guid.Empty)
					return Unauthorized("Organization ID is missing from token");

				var token = _calendarService.GeneratePropertyCalendarToken(id, organizationId);
				var subscriptionUrl = $"{Request.Scheme}://{Request.Host}/api/common/calendar/property/{id}.ics?organizationId={organizationId}&token={token}";

				return Ok(new CalendarSubscriptionResponseDto
				{
					PropertyId = id,
					OrganizationId = organizationId,
					SubscriptionUrl = subscriptionUrl
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating calendar subscription URL for property: {PropertyId}", id);
				return ServerError("An error occurred while creating calendar subscription URL");
			}
		}

		/// <summary>
		/// Get property by ID
		/// </summary>
		/// <param name="id">Property ID</param>
		/// <returns>Property</returns>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(Guid id)
		{
			if (id == Guid.Empty)
				return BadRequest("Property ID is required");

			try
			{
				var property = await _propertyRepository.GetByIdAsync(id, CurrentOrganizationId);
				if (property == null)
					return NotFound("Property not found");

				return Ok(new PropertyResponseDto(property));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property by ID: {PropertyId}", id);
				return ServerError("An error occurred while retrieving the property");
			}
		}

		/// <summary>
		/// Get property by PropertyCode
		/// </summary>
		/// <param name="propertyCode">Property Code</param>
		/// <returns>Property</returns>
		[HttpGet("code/{propertyCode}")]
		public async Task<IActionResult> GetByPropertyCode(string propertyCode)
		{
			if (string.IsNullOrWhiteSpace(propertyCode))
				return BadRequest("Property Code is required");

			try
			{
				var property = await _propertyRepository.GetByPropertyCodeAsync(propertyCode, CurrentOrganizationId);
				if (property == null)
					return NotFound("Property not found");

				return Ok(new PropertyResponseDto(property));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property by PropertyCode: {PropertyCode}", propertyCode);
				return ServerError("An error occurred while retrieving the property");
			}
		}

		/// <summary>
		/// Get the current user's property selection
		/// </summary>
		/// <param name="userId">User Id</param>
		/// <returns>Property selection</returns>
		[HttpGet("selection/{userId}")]
		public async Task<IActionResult> GetPropertySelection(Guid userId)
		{
			if (CurrentUser == Guid.Empty || CurrentUser != userId)
				return Unauthorized();

			try
			{
				var selection = await _propertySelectionRepository.GetByUserIdAsync(CurrentUser);
				if (selection == null)
					return Ok();

				return Ok(new PropertySelectionResponseDto(selection));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting property selection for user: {UserId}", CurrentUser);
				return ServerError("An error occurred while retrieving the property selection");
			}
		}
	}
}