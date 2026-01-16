using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Common;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RentAll.Api.Controllers
{
	public class BaseController : ControllerBase
	{
		private (Guid UserId, Guid OrganizationId, string OfficeAccess, string UserGroups)? _cachedUserInfo;

		protected Guid CurrentUser => GetUserInfoFromJwt().UserId;
		protected Guid CurrentOrganizationId => GetUserInfoFromJwt().OrganizationId;
		protected string CurrentOfficeAccess => GetUserInfoFromJwt().OfficeAccess;
		protected string CurrentUserGroups => GetUserInfoFromJwt().UserGroups;

		protected ErrorResponseDto ErrorResponse(string message)
		{
			var controllerName = this.GetType().Name.Replace("Controller", "");
			var httpMethod = HttpContext.Request.Method;
			var actionName = ControllerContext.ActionDescriptor?.ActionName ?? "";
			
			// Get route - try multiple sources
			var route = "";
			var actionDescriptor = ControllerContext.ActionDescriptor;
			if (actionDescriptor != null)
			{
				// Try AttributeRouteInfo template first (e.g., "propertyhtml/{propertyId}")
				route = actionDescriptor.AttributeRouteInfo?.Template ?? "";
				
				// If no template, build from controller and action route values
				if (string.IsNullOrEmpty(route))
				{
					var routeValues = actionDescriptor.RouteValues;
					if (routeValues != null)
					{
						var controller = routeValues.TryGetValue("controller", out var controllerValue) ? controllerValue : "";
						var action = routeValues.TryGetValue("action", out var actionValue) ? actionValue : "";
						if (!string.IsNullOrEmpty(controller) && !string.IsNullOrEmpty(action))
						{
							route = $"{controller}/{action}";
						}
					}
				}
			}

			return new ErrorResponseDto
			{
				Controller = controllerName,
				HttpMethod = httpMethod,
				ActionName = actionName,
				Route = route,
				Message = message
			};
		}

		// Wrapper methods for common HTTP error responses
		protected IActionResult BadRequest(string message)
		{
			return base.BadRequest(ErrorResponse(message));
		}

		protected IActionResult NotFound(string message)
		{
			return base.NotFound(ErrorResponse(message));
		}

		protected IActionResult Unauthorized(string message)
		{
			return base.Unauthorized(ErrorResponse(message));
		}

		protected IActionResult Conflict(string message)
		{
			return base.Conflict(ErrorResponse(message));
		}

		protected IActionResult ServerError(string message)
		{
			return StatusCode(500, ErrorResponse(message));
		}

		private (Guid UserId, Guid OrganizationId, string OfficeAccess, string UserGroups) GetUserInfoFromJwt()
		{
			// Return cached value if available
			if (_cachedUserInfo.HasValue)
				return _cachedUserInfo.Value;

			var result = (UserId: Guid.Empty, OrganizationId: Guid.Empty, OfficeAccess: string.Empty, UserGroups: string.Empty);

			if (User?.Identity?.IsAuthenticated != true)
			{
				_cachedUserInfo = result;
				return result;
			}

			var userClaim = User.FindFirst("user");
			if (userClaim == null || string.IsNullOrWhiteSpace(userClaim.Value))
			{
				_cachedUserInfo = result;
				return result;
			}

			try
			{
				var userJsonBytes = Convert.FromBase64String(userClaim.Value);
				var userJson = Encoding.UTF8.GetString(userJsonBytes);
				var userObject = JsonSerializer.Deserialize<JsonElement>(userJson);

				// Extract userId
				if (userObject.TryGetProperty("userId", out var userIdElement))
				{
					var userIdString = userIdElement.GetString();
					if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userId))
						result.UserId = userId;
				}

				// Extract organizationId (try a few common property names)
				string[] possibleOrgPropertyNames = ["organizationId", "OrganizationId"];
				foreach (var propName in possibleOrgPropertyNames)
				{
					if (userObject.TryGetProperty(propName, out var orgIdElement))
					{
						var orgIdString = orgIdElement.GetString();
						if (!string.IsNullOrWhiteSpace(orgIdString) && Guid.TryParse(orgIdString, out var orgId))
						{
							result.OrganizationId = orgId;
							break;
						}
					}
				}

				// Extract OfficeAccess
				string[] possibleOfficeAccessPropertyNames = ["officeAccess", "OfficeAccess"];
				foreach (var propName in possibleOfficeAccessPropertyNames)
				{
					if (userObject.TryGetProperty(propName, out var officeAccessElement))
					{
						var officeAccessString = officeAccessElement.GetString();
						if (!string.IsNullOrWhiteSpace(officeAccessString))
							result.OfficeAccess = officeAccessString;
						break;
					}
				}

				// Extract UserGroups (try both property name variations - it's a comma-delimited string)
				string[] possibleUserGroupsPropertyNames = ["userGroups", "UserGroups"];
				foreach (var propName in possibleUserGroupsPropertyNames)
				{
					if (userObject.TryGetProperty(propName, out var userGroupsElement))
					{
						var userGroupsString = userGroupsElement.GetString();
						if (!string.IsNullOrWhiteSpace(userGroupsString))
							result.UserGroups = userGroupsString;
						break;
					}
				}
			}
			catch
			{
				// If decoding fails, return empty values
			}

			_cachedUserInfo = result;
			return result;
		}

		protected bool IsValidEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
				return false;

			// RFC 5322 compliant email regex pattern
			var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
			
			try
			{
				return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		protected bool IsAdmin()
		{
			if (string.IsNullOrWhiteSpace(CurrentUserGroups))
				return false;

			return CurrentUserGroups.Split(',').Any(g => g.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase));
		}
	}
}

