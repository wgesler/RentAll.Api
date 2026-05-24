using RentAll.Domain.Models.Leads;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Api.Controllers;

public partial class LeadController
{
    #region Select

    [HttpGet("owners")]
    public async Task<IActionResult> GetOwnerLeadsAsync()
    {
        try
        {
            var all = await _leadRepository.GetOwnersByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            return Ok(all.Select(o => new LeadOwnerResponseDto(o)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner leads");
            return ServerError("An error occurred while retrieving owner leads");
        }
    }

    [HttpGet("owners/{ownerId:int}")]
    public async Task<IActionResult> GetOwnerLeadByIdAsync(int ownerId)
    {
        if (ownerId <= 0)
            return BadRequest("OwnerId is required");

        try
        {
            var owner = await _leadRepository.GetOwnerByIdAsync(ownerId);
            if (owner == null)
                return NotFound("Owner lead not found");

            if (owner.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == owner.OfficeId))
                return NotFound("Owner lead not found");

            return Ok(new LeadOwnerResponseDto(owner));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner lead {OwnerId}", ownerId);
            return ServerError("An error occurred while retrieving the owner lead");
        }
    }

    #endregion

    #region Create

    [HttpPost("owners")]
    public async Task<IActionResult> CreateOwnerLeadAsync([FromBody] CreateLeadOwnerDto dto)
    {
        if (dto == null)
            return BadRequest("Owner lead data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (dto.AgentId.HasValue && await _organizationRepository.GetAgentByIdAsync(dto.AgentId.Value, CurrentOrganizationId) == null)
            return BadRequest("AgentId is not valid for the current organization.");

        try
        {
            var created = await _leadRepository.CreateOwnerAsync(dto.ToModel(CurrentOrganizationId, CurrentUser));
            return Ok(new LeadOwnerResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner lead");
            return ServerError("An error occurred while creating the owner lead");
        }
    }

    [HttpPost("owners/{ownerId:int}/share-link")]
    public async Task<IActionResult> CreateOwnerFormShareLinkAsync(int ownerId)
    {
        if (ownerId <= 0)
            return BadRequest("OwnerId is required");

        try
        {
            var owner = await _leadRepository.GetOwnerByIdAsync(ownerId);
            if (owner == null)
                return NotFound("Owner lead not found");

            if (owner.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == owner.OfficeId))
                return NotFound("Owner lead not found");

            var token = GenerateOwnerFormShareToken();
            var tokenHash = ComputeSha256Hex(token);

            var share = new LeadOwnerFormShare
            {
                ShareId = Guid.NewGuid(),
                OwnerId = ownerId,
                OrganizationId = CurrentOrganizationId,
                TokenHash = tokenHash,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(14)
            };

            var created = await _leadRepository.UpsertOwnerFormShareByOwnerIdAsync(share);
            return Ok(new OwnerFormShareResponseDto
            {
                ShareId = created.ShareId,
                OwnerId = created.OwnerId,
                Token = token,
                ExpiresOn = created.ExpiresOn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner form share link: {OwnerId}", ownerId);
            return ServerError("An error occurred while creating the owner form share link");
        }
    }

    #endregion

    #region Update

    [HttpPut("owners")]
    public async Task<IActionResult> UpdateOwnerLeadAsync([FromBody] UpdateLeadOwnerDto dto)
    {
        if (dto == null)
            return BadRequest("Owner lead data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOfficeAccess);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (dto.AgentId.HasValue && await _organizationRepository.GetAgentByIdAsync(dto.AgentId.Value, CurrentOrganizationId) == null)
            return BadRequest("AgentId is not valid for the current organization.");

        try
        {
            var existing = await _leadRepository.GetOwnerByIdAsync(dto.OwnerId);
            if (existing == null)
                return NotFound("Owner lead not found");

            if (existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
                return NotFound("Owner lead not found");

            var updated = dto.ToModel(CurrentUser);
            updated.OrganizationId = existing.OrganizationId;
            var updatedResult = await _leadRepository.UpdateOwnerByIdAsync(updated);
            return Ok(new LeadOwnerResponseDto(updatedResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating owner lead");
            return ServerError("An error occurred while updating the owner lead");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("owners/{ownerId:int}")]
    public async Task<IActionResult> DeleteOwnerLeadAsync(int ownerId)
    {
        if (ownerId <= 0)
            return BadRequest("OwnerId is required");

        try
        {
            var existing = await _leadRepository.GetOwnerByIdAsync(ownerId);
            if (existing == null)
                return NotFound("Owner lead not found");

            if (existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Owner lead not found");

            if (!CurrentOfficeAccess.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == existing.OfficeId))
                return NotFound("Owner lead not found");

            await _leadRepository.DeleteOwnerByIdAsync(ownerId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting owner lead {OwnerId}", ownerId);
            return ServerError("An error occurred while deleting the owner lead");
        }
    }

    #endregion

    private static string GenerateOwnerFormShareToken()
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
