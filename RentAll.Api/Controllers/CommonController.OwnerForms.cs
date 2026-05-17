using RentAll.Api.Dtos.Leads.Owners;
using RentAll.Domain.Models.Leads;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Api.Controllers
{
    public partial class CommonController
    {
        [HttpGet("owner-form/{token}")]
        public async Task<IActionResult> GetPublicOwnerFormByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token is required");

            try
            {
                token = NormalizeOwnerFormShareToken(token);
                if (string.IsNullOrWhiteSpace(token))
                    return BadRequest("Token is required");

                var tokenHash = ComputeOwnerFormSha256Hex(token);
                var share = await _leadRepository.GetOwnerFormShareByTokenHashAsync(tokenHash);
                if (share == null)
                    return NotFound("Owner form not found");

                var owner = await _leadRepository.GetOwnerByIdAsync(share.OwnerId);
                if (owner == null || owner.OrganizationId != share.OrganizationId)
                    return NotFound("Owner form not found");

                var ownerInventoryInformation = await _leadRepository.GetOwnerInventoryInformationByOwnerIdAsync(owner.OwnerId, owner.OrganizationId);
                return Ok(new PublicOwnerFormResponseDto(owner, ownerInventoryInformation, share.ExpiresOn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner form by token");
                return ServerError("An error occurred while retrieving owner form");
            }
        }

        [HttpPut("owner-form/{token}")]
        public async Task<IActionResult> SubmitPublicOwnerFormByTokenAsync(string token, [FromBody] SubmitPublicOwnerFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token is required");

            if (dto == null)
                return BadRequest("Owner form data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                token = NormalizeOwnerFormShareToken(token);
                if (string.IsNullOrWhiteSpace(token))
                    return BadRequest("Token is required");

                var tokenHash = ComputeOwnerFormSha256Hex(token);
                var share = await _leadRepository.GetOwnerFormShareByTokenHashAsync(tokenHash);
                if (share == null)
                    return NotFound("Owner form not found");

                var owner = await _leadRepository.GetOwnerByIdAsync(share.OwnerId);
                if (owner == null || owner.OrganizationId != share.OrganizationId)
                    return NotFound("Owner form not found");

                dto.ApplyTo(owner);
                var updated = await _leadRepository.UpdateOwnerByIdAsync(owner);
                var existingInventory = await _leadRepository.GetOwnerInventoryInformationByOwnerIdAsync(owner.OwnerId, owner.OrganizationId);
                if (existingInventory == null)
                {
                    var inventoryToCreate = new OwnerInventoryInformation
                    {
                        OwnerId = owner.OwnerId,
                        OrganizationId = owner.OrganizationId,
                        IsActive = true,
                        CreatedBy = CurrentUser
                    };
                    dto.ApplyTo(inventoryToCreate);
                    existingInventory = await _leadRepository.CreateOwnerInventoryInformationAsync(inventoryToCreate);
                }
                else
                {
                    dto.ApplyTo(existingInventory);
                    existingInventory.ModifiedBy = CurrentUser;
                    existingInventory = await _leadRepository.UpdateOwnerInventoryInformationByIdAsync(existingInventory);
                }

                return Ok(new PublicOwnerFormResponseDto(updated, existingInventory, share.ExpiresOn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting public owner form by token");
                return ServerError("An error occurred while submitting owner form");
            }
        }

        private static string NormalizeOwnerFormShareToken(string token)
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

        private static string ComputeOwnerFormSha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
