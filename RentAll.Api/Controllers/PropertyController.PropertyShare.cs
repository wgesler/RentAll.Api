using System.Security.Cryptography;
using System.Text;
using RentAll.Api.Dtos.Properties.PropertyShares;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {
        #region Post

        [HttpPost("{propertyId:guid}/share-link")]
        public async Task<IActionResult> CreatePropertyListingShareLinkAsync(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var token = GenerateListingShareToken();
                var tokenHash = ComputeSha256Hex(token);

                var share = new PropertyListingShare
                {
                    ShareId = Guid.NewGuid(),
                    PropertyId = propertyId,
                    OrganizationId = CurrentOrganizationId,
                    TokenHash = tokenHash,
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(30),
                    IsActive = true
                };

                var created = await _propertyRepository.UpsertPropertyListingShareByPropertyIdAsync(share, CurrentUser);

                return Ok(new PropertyListingShareResponseDto
                {
                    ShareId = created.ShareId,
                    PropertyId = created.PropertyId,
                    Token = token,
                    ExpiresOn = created.ExpiresOn
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property listing share link: {PropertyId}", propertyId);
                return ServerError("An error occurred while creating the property listing share link");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("{propertyId:guid}/share-link")]
        public async Task<IActionResult> RevokePropertyListingShareLinkAsync(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                await _propertyRepository.RevokePropertyListingShareByPropertyIdAsync(propertyId, CurrentUser);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking property listing share link: {PropertyId}", propertyId);
                return ServerError("An error occurred while revoking the property listing share link");
            }
        }

        #endregion

        private static string GenerateListingShareToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return token;
        }

        private static string ComputeSha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
