using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Common;
using RentAll.Domain.Configuration;

namespace RentAll.Api.Controllers;

public partial class CommonController
{
    [HttpGet("feature-flags")]
    public IActionResult GetFeatureFlags()
    {
        return Ok(new FeatureFlagsResponseDto(_featureFlagService.GetAll()));
    }

    [Authorize]
    [HttpPut("feature-flags")]
    public IActionResult UpdateFeatureFlags([FromBody] UpdateFeatureFlagsRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Feature flag data is required");

        if (!IsSuperAdmin())
            return Unauthorized();

        if (dto.Accounting.HasValue)
            _featureFlagService.Set(FeatureFlagKeys.Accounting, dto.Accounting.Value);

        return Ok(new FeatureFlagsResponseDto(_featureFlagService.GetAll()));
    }
}
