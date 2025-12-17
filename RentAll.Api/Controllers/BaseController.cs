using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public class BaseController : ControllerBase
	{
		protected Guid CurrentUser => GetCurrentUserIdFromJwt();
		protected Guid CurrentOrganizationId => GetCurrentUserOrganizationIdFromJwt();

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

