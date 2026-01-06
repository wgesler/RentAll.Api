using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Common;

namespace RentAll.Api.Controllers
{
	public class BaseController : ControllerBase
	{
		protected Guid CurrentUser => GetCurrentUserIdFromJwt();
		protected Guid CurrentOrganizationId => GetCurrentUserOrganizationIdFromJwt();

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

		private Guid GetCurrentUserIdFromJwt()
		{
			if (User?.Identity?.IsAuthenticated != true)
				return Guid.Empty;

			var userClaim = User.FindFirst("user");
			if (userClaim == null || string.IsNullOrWhiteSpace(userClaim.Value))
				return Guid.Empty;

			try
			{
				var userJsonBytes = Convert.FromBase64String(userClaim.Value);
				var userJson = Encoding.UTF8.GetString(userJsonBytes);
				var userObject = JsonSerializer.Deserialize<JsonElement>(userJson);

				if (userObject.TryGetProperty("userId", out var userIdElement))
				{
					var userIdString = userIdElement.GetString();
					if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userId))
						return userId;
				}
			}
			catch
			{
				// If decoding fails, return empty GUID
			}

			return Guid.Empty;
		}

		private Guid GetCurrentUserOrganizationIdFromJwt()
		{
			if (User?.Identity?.IsAuthenticated != true)
				return Guid.Empty;

			var userClaim = User.FindFirst("user");
			if (userClaim == null || string.IsNullOrWhiteSpace(userClaim.Value))
				return Guid.Empty;

			try
			{
				var userJsonBytes = Convert.FromBase64String(userClaim.Value);
				var userJson = Encoding.UTF8.GetString(userJsonBytes);
				var userObject = JsonSerializer.Deserialize<JsonElement>(userJson);

				// Try a few common property names for organization id in the JWT payload
				string[] possiblePropertyNames = ["organizationId", "OrganizationId"];

				foreach (var propName in possiblePropertyNames)
				{
					if (userObject.TryGetProperty(propName, out var orgIdElement))
					{
						var orgIdString = orgIdElement.GetString();
						if (!string.IsNullOrWhiteSpace(orgIdString) && Guid.TryParse(orgIdString, out var orgId))
							return orgId;
					}
				}
			}
			catch
			{
				// If decoding fails, return empty GUID
			}

			return Guid.Empty;
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
	}
}

