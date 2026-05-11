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
                token = NormalizeListingShareToken(token);
                if (string.IsNullOrWhiteSpace(token))
                    return BadRequest("Token is required");

                var tokenHash = ComputeSha256Hex(token);
                var share = await _propertyRepository.GetPropertyListingShareByTokenHashAsync(tokenHash);
                if (share == null)
                    return NotFound("Listing not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(share.PropertyId, share.OrganizationId);
                if (property == null)
                    return NotFound("Listing not found");

                var listingScope = BuildListingPhotoScope(property.OfficeName, property.PropertyCode);
                var photos = await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(share.PropertyId);
                var photoDtos = photos
                    .Select(async p =>
                    {
                        var dto = new PropertyPhotoResponseDto(p);
                        dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                            share.OrganizationId,
                            listingScope,
                            dto.PhotoPath,
                            ImageType.Photos);

                        var hasInlineBytes = dto.FileDetails != null
                            && (!string.IsNullOrWhiteSpace(dto.FileDetails.DataUrl)
                                || !string.IsNullOrWhiteSpace(dto.FileDetails.File));
                        if (hasInlineBytes)
                        {
                            // Public listing should render from bytes, not direct blob links.
                            dto.PhotoPath = string.Empty;
                            return dto;
                        }

                        if (!string.IsNullOrWhiteSpace(dto.PhotoPath))
                        {
                            var normalizedPath = dto.PhotoPath.Trim().Replace("\\", "/");
                            if (!Uri.TryCreate(normalizedPath, UriKind.Absolute, out _))
                            {
                                if (!normalizedPath.StartsWith("/"))
                                    normalizedPath = "/" + normalizedPath;

                                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                                normalizedPath = $"{baseUrl}{normalizedPath}";
                            }

                            dto.PhotoPath = normalizedPath;
                        }

                        return dto;
                    })
                    .ToList();
                var resolvedPhotoDtos = await Task.WhenAll(photoDtos);

                var response = new PublicPropertyListingResponseDto
                {
                    Property = new PropertyResponseDto(property),
                    Photos = resolvedPhotoDtos.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public property listing by token");
                return ServerError("An error occurred while retrieving property listing");
            }
        }

        private static string NormalizeListingShareToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            var t = token.Trim()
                .Replace("\u00AD", string.Empty, StringComparison.Ordinal)
                .Replace("\u200B", string.Empty, StringComparison.Ordinal)
                .Replace("\u200C", string.Empty, StringComparison.Ordinal)
                .Replace("\u200D", string.Empty, StringComparison.Ordinal)
                .Replace("\uFEFF", string.Empty, StringComparison.Ordinal);

            Span<char> buffer = stackalloc char[t.Length];
            var written = 0;
            foreach (var ch in t)
            {
                buffer[written++] = ch switch
                {
                    '\u2010' or '\u2011' or '\u2012' or '\u2013' or '\u2014' or '\u2015' or '\u2212' or '\uFE58' or '\uFE63' or '\uFF0D' => '-',
                    _ => ch
                };
            }

            return new string(buffer[..written]);
        }

        private static string ComputeSha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static string BuildListingPhotoScope(string? officeName, string? propertyCode)
        {
            var normalizedOffice = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
            var normalizedCode = string.IsNullOrWhiteSpace(propertyCode) ? "unknown-property" : propertyCode.Trim();
            return $"{normalizedOffice}/listings/{normalizedCode}";
        }
    }
}
