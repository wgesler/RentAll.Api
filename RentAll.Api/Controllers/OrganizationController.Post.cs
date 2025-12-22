using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        /// <summary>
        /// Create a new organization
        /// </summary>
        /// <param name="dto">Organization data</param>
        /// <returns>Created organization</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Organization data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
				// Get a new organization code
				var code = await _organizationManager.GenerateEntityCodeAsync();
				var model = dto.ToModel(code, CurrentUser);

                var created = await _organizationRepository.CreateAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = created.OrganizationId }, new OrganizationResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                return StatusCode(500, new { message = "An error occurred while creating the organization" });
            }
        }
    }
}




