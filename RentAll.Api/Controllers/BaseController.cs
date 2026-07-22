using RentAll.Api.Dtos.Accounting.ClosedDate;
using RentAll.Domain.Interfaces.Repositories;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RentAll.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        private (Guid UserId, Guid OrganizationId, string OfficeAccess, string UserGroups, string Properties)? _cachedUserInfo;

        protected Guid CurrentUser => GetUserInfoFromJwt().UserId;
        protected Guid CurrentOrganizationId => GetUserInfoFromJwt().OrganizationId;
        protected string CurrentOfficeAccess => GetUserInfoFromJwt().OfficeAccess;
        protected string CurrentUserGroups => GetUserInfoFromJwt().UserGroups;
        protected string CurrentUserProperties => GetUserInfoFromJwt().Properties;

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

        private (Guid UserId, Guid OrganizationId, string OfficeAccess, string UserGroups, string Properties) GetUserInfoFromJwt()
        {
            // Return cached value if available
            if (_cachedUserInfo.HasValue)
                return _cachedUserInfo.Value;

            var result = (UserId: Guid.Empty, OrganizationId: Guid.Empty, OfficeAccess: string.Empty, UserGroups: string.Empty, Properties: string.Empty);

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

                // Extract Properties (usually comma-delimited string, but also support JSON array for compatibility).
                string[] possiblePropertiesPropertyNames = ["properties", "Properties"];
                foreach (var propName in possiblePropertiesPropertyNames)
                {
                    if (!userObject.TryGetProperty(propName, out var propertiesElement))
                        continue;

                    if (propertiesElement.ValueKind == JsonValueKind.String)
                    {
                        var propertiesString = propertiesElement.GetString();
                        if (!string.IsNullOrWhiteSpace(propertiesString))
                            result.Properties = propertiesString;
                    }
                    else if (propertiesElement.ValueKind == JsonValueKind.Array)
                    {
                        var propertiesValues = propertiesElement
                            .EnumerateArray()
                            .Where(v => v.ValueKind == JsonValueKind.String)
                            .Select(v => v.GetString())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .Cast<string>()
                            .ToList();

                        if (propertiesValues.Any())
                            result.Properties = string.Join(",", propertiesValues);
                    }

                    break;
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

        protected bool IsSuperAdmin()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserGroups))
                return false;

            return CurrentUserGroups.Split(',').Any(g => g.Trim().Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
        }

        protected bool IsAdmin()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserGroups))
                return false;

            return CurrentUserGroups.Split(',').Any(g => g.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase));
        }

        protected bool IsOfficeAdmin()
        {
            if (string.IsNullOrWhiteSpace(CurrentUserGroups))
                return false;

            return CurrentUserGroups.Split(',').Any(g => g.Trim().Equals("OfficeAdmin", StringComparison.OrdinalIgnoreCase));
        }

        protected bool HasUserGroup(RoleType role)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserGroups))
                return false;

            var roleName = role.ToString();
            return CurrentUserGroups.Split(',').Any(g => g.Trim().Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        protected bool HasAnyUserGroup(params RoleType[] roles)
            => roles.Any(HasUserGroup);

        protected bool CanModifyPostedDocument()
            => HasAnyUserGroup(
                RoleType.SuperAdmin,
                RoleType.Admin,
                RoleType.OfficeAdmin,
                RoleType.Accounting,
                RoleType.AccountingAdmin);

        protected bool CanModifySoftClosedDocument()
            => HasAnyUserGroup(RoleType.SuperAdmin, RoleType.Admin, RoleType.OfficeAdmin);

        protected IActionResult? RefuseIfDocumentUpdateNotAllowed(PostingStatus postingStatus, string documentLabel)
        {
            var hardClosedResult = RefuseIfDocumentHardClosed(postingStatus, documentLabel, "update");
            if (hardClosedResult != null)
                return hardClosedResult;

            switch (postingStatus)
            {
                case PostingStatus.Open:
                    return null;
                case PostingStatus.Posted:
                    if (!CanModifyPostedDocument())
                        return Unauthorized($"You are not authorized to update a posted {documentLabel}.");
                    return null;
                case PostingStatus.SoftClosed:
                    if (!CanModifySoftClosedDocument())
                        return Unauthorized($"You are not authorized to update a soft-closed {documentLabel}.");
                    return null;
                default:
                    return Unauthorized($"You are not authorized to update this {documentLabel}.");
            }
        }

        protected IActionResult? RefuseIfDocumentUpdateNotAllowed(int? postingStatusId, string documentLabel)
            => RefuseIfDocumentUpdateNotAllowed(ResolvePostingStatus(postingStatusId), documentLabel);

        protected IActionResult? RefuseIfDocumentDeleteNotAllowed(PostingStatus postingStatus, string documentLabel)
        {
            var hardClosedResult = RefuseIfDocumentHardClosed(postingStatus, documentLabel, "delete");
            if (hardClosedResult != null)
                return hardClosedResult;

            switch (postingStatus)
            {
                case PostingStatus.Open:
                    return null;
                case PostingStatus.SoftClosed:
                    if (!CanModifySoftClosedDocument())
                        return Unauthorized($"You are not authorized to delete a soft-closed {documentLabel}.");
                    return null;
                case PostingStatus.Posted:
                    return Unauthorized($"You are not authorized to delete a posted {documentLabel}.");
                default:
                    return Unauthorized($"You are not authorized to delete this {documentLabel}.");
            }
        }

        protected IActionResult? RefuseIfDocumentDeleteNotAllowed(int? postingStatusId, string documentLabel)
            => RefuseIfDocumentDeleteNotAllowed(ResolvePostingStatus(postingStatusId), documentLabel);

        protected IActionResult? RefuseIfJournalEntryUpdateNotAllowed(int? postingStatusId, string documentLabel = "journal entry")
            => RefuseIfDocumentUpdateNotAllowed(postingStatusId, documentLabel);

        protected IActionResult? RefuseIfJournalEntryUpdateNotAllowed(PostingStatus postingStatus, string documentLabel = "journal entry")
            => RefuseIfDocumentUpdateNotAllowed(postingStatus, documentLabel);

        protected IActionResult? RefuseIfJournalEntryDeleteNotAllowed(int? postingStatusId, string documentLabel = "journal entry")
            => RefuseIfDocumentDeleteNotAllowed(postingStatusId, documentLabel);

        protected IActionResult? RefuseIfJournalEntryDeleteNotAllowed(PostingStatus postingStatus, string documentLabel = "journal entry")
            => RefuseIfDocumentDeleteNotAllowed(postingStatus, documentLabel);

        private static PostingStatus ResolvePostingStatus(int? postingStatusId)
            => Enum.IsDefined(typeof(PostingStatus), postingStatusId ?? 0)
                ? (PostingStatus)(postingStatusId ?? 0)
                : PostingStatus.Open;

        protected static PostingStatus StrictestPostingStatus(IEnumerable<int?> postingStatusIds)
            => postingStatusIds
                .Select(ResolvePostingStatus)
                .DefaultIfEmpty(PostingStatus.Open)
                .Max();

        protected bool CanOverrideSoftClosedAccountingPeriod()
        {
            return IsSuperAdmin() || IsAdmin() || IsOfficeAdmin();
        }

        protected IActionResult AccountingPeriodClosedConflict(PostingStatus closedStatus, string actionLabel)
        {
            return StatusCode(StatusCodes.Status409Conflict, new ClosedPeriodResponseDto
            {
                ClosedStatus = (int)closedStatus,
                Message = $"Cannot {actionLabel} because the accounting period has been closed."
            });
        }

        protected async Task<IActionResult?> RefuseIfAccountingPeriodClosedAsync(IAccountingRepository accountingRepository, Guid organizationId, int officeId, DateOnly accountingPeriod, string actionLabel)
        {
            var periodStatus = await accountingRepository.CheckAccountingPeriodAsync(organizationId, officeId, accountingPeriod);
            if (periodStatus == PostingStatus.HardClosed)
                return AccountingPeriodClosedConflict(periodStatus, actionLabel);

            if (periodStatus == PostingStatus.SoftClosed && !CanOverrideSoftClosedAccountingPeriod())
                return AccountingPeriodClosedConflict(periodStatus, actionLabel);

            return null;
        }

        protected IActionResult? RefuseIfDocumentHardClosed(int? postingStatusId, string documentLabel)
            => RefuseIfDocumentHardClosed(ResolvePostingStatus(postingStatusId), documentLabel, "update");

        protected IActionResult? RefuseIfJournalEntryHardClosed(int? postingStatusId, string documentLabel)
            => RefuseIfDocumentHardClosed(postingStatusId, documentLabel);

        private IActionResult? RefuseIfDocumentHardClosed(PostingStatus postingStatus, string documentLabel, string action)
        {
            if (postingStatus == PostingStatus.HardClosed)
                return Conflict($"Cannot {action} {documentLabel} because it is hard closed.");

            return null;
        }

    }
}
