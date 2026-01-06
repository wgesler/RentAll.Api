using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.OfficeConfigurations;
using RentAll.Api.Dtos.Offices;

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
                var offices = await _officeRepository.GetAllAsync(CurrentOrganizationId);
                var response = new List<OfficeResponseDto>();
                foreach (var office in offices)
                {
                    var dto = new OfficeResponseDto(office);
                    if (!string.IsNullOrWhiteSpace(office.LogoPath))
                        dto.FileDetails = await _fileService.GetFileDetailsAsync(office.LogoPath);

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
                    response.FileDetails = await _fileService.GetFileDetailsAsync(office.LogoPath);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting office by ID: {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving the office");
            }
        }

		/// <summary>
		/// Get all office configurations
		/// </summary>
		/// <returns>List of office configurations</returns>
		[HttpGet("configuration")]
		public async Task<IActionResult> GetConfigurations()
		{
			try
			{
				var configurations = await _officeConfigurationRepository.GetAllAsync(CurrentOrganizationId);
				var response = configurations.Select(c => new OfficeConfigurationResponseDto(c));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all office configurations");
				return ServerError("An error occurred while retrieving office configurations");
			}
		}

		/// <summary>
		/// Get office configuration by Office ID
		/// </summary>
		/// <param name="officeId">Office ID</param>
		/// <returns>Office configuration</returns>
		[HttpGet("{officeId}/configuration")]
		public async Task<IActionResult> GetConfiguration(int officeId)
		{
			if (officeId <= 0)
				return BadRequest("Office ID is required");

			try
			{
				var configuration = await _officeConfigurationRepository.GetByOfficeIdAsync(officeId, CurrentOrganizationId);
			    if (configuration == null)
				    return NotFound("Office configuration not found");

				return Ok(new OfficeConfigurationResponseDto(configuration));
			}
			catch (Exception ex)
			{
			_logger.LogError(ex, "Error getting office configuration: {OfficeId}", officeId);
			return ServerError("An error occurred while retrieving the office configuration");
			}
		}
	}
}

