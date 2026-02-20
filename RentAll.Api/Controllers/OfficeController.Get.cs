using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations.Offices;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers
{
    public partial class OfficeController
    {
        /// <summary>
        /// Get all offices
        /// </summary>
        /// <returns>List of offices</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                IEnumerable<Office> offices;
                if (IsAdmin())
                    offices = await _officeRepository.GetAllAsync(CurrentOrganizationId);
                else
                    offices = await _officeRepository.GetAllByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);

                var response = new List<OfficeResponseDto>();
                foreach (var office in offices)
                {
                    var dto = new OfficeResponseDto(office);
                    if (!string.IsNullOrWhiteSpace(office.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(office.OrganizationId, null, office.LogoPath);

                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all offices");
                return ServerError("An error occurred while retrieving offices");
            }
        }

        /// <summary>
        /// Get office by ID
        /// </summary>
        /// <param name="officeId">Office ID</param>
        /// <returns>Office</returns>
        [HttpGet("{officeId}")]
        public async Task<IActionResult> GetById(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var office = await _officeRepository.GetByIdAsync(officeId, CurrentOrganizationId);
                if (office == null)
                    return NotFound("Office not found");

                var response = new OfficeResponseDto(office);
                if (!string.IsNullOrWhiteSpace(office.LogoPath))
                    response.FileDetails = await _fileService.GetFileDetailsAsync(office.OrganizationId, null, office.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting office by ID: {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving the office");
            }
        }
    }
}

