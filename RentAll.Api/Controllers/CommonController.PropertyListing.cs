using RentAll.Api.Dtos.Properties.PropertyPhotos;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Api.Controllers
{
    public partial class CommonController
    {
        [HttpGet("property-listing/{token}")]
        public async Task<IActionResult> GetPublicPropertyListingByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token is required");

            try
            {
                var tokenHash = ComputeSha256Hex(token);
                var share = await _propertyRepository.GetPropertyListingShareByTokenHashAsync(tokenHash);
                if (share == null)
                    return NotFound("Listing not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(share.PropertyId, share.OrganizationId);
                if (property == null)
                    return NotFound("Listing not found");

                var photos = await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(share.PropertyId);
                var photoDtos = photos
                    .Select(p =>
                    {
                        var dto = new PropertyPhotoResponseDto(p);
                        if (!string.IsNullOrWhiteSpace(dto.PhotoPath))
                        {
                            var baseUrl = $"{Request.Scheme}://{Request.Host}";
                            dto.PhotoPath = $"{baseUrl}{dto.PhotoPath}";
                        }

                        return dto;
                    })
                    .ToList();

                var response = new PublicPropertyListingResponseDto
                {
                    Property = new PropertyResponseDto(property),
                    Photos = photoDtos
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public property listing by token");
                return ServerError("An error occurred while retrieving property listing");
            }
        }

        private static string ComputeSha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
