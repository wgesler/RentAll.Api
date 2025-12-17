using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Properties;

namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {

		/// <summary>
		/// Get all properties
		/// </summary>
		/// <returns>List of properties</returns>
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var properties = await _propertyRepository.GetAllAsync(CurrentOrganizationId);
				var response = properties.Select(p => new PropertyResponseDto(p));
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all properties");
				return StatusCode(500, new { message = "An error occurred while retrieving properties" });
			}
		}
        
        /// <summary>
		/// Get property by ID
		/// </summary>
		/// <param name="id">Property ID</param>
		/// <returns>Property</returns>
		[HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Property ID is required" });

            try
            {
                var property = await _propertyRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (property == null)
                    return NotFound(new { message = "Property not found" });

                return Ok(new PropertyResponseDto(property));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property by ID: {PropertyId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the property" });
            }
        }

        /// <summary>
        /// Get property by PropertyCode
        /// </summary>
        /// <param name="propertyCode">Property Code</param>
        /// <returns>Property</returns>
        [HttpGet("code/{propertyCode}")]
        public async Task<IActionResult> GetByPropertyCode(string propertyCode)
        {
            if (string.IsNullOrWhiteSpace(propertyCode))
                return BadRequest(new { message = "Property Code is required" });

            try
            {
                var property = await _propertyRepository.GetByPropertyCodeAsync(propertyCode, CurrentOrganizationId);
                if (property == null)
                    return NotFound(new { message = "Property not found" });

                return Ok(new PropertyResponseDto(property));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property by PropertyCode: {PropertyCode}", propertyCode);
                return StatusCode(500, new { message = "An error occurred while retrieving the property" });
            }
        }

        /// <summary>
        /// Get properties by state
        /// </summary>
        /// <param name="state">State code</param>
        /// <returns>List of properties</returns>
        [HttpGet("state/{state}")]
        public async Task<IActionResult> GetByState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return BadRequest(new { message = "State is required" });

            try
            {
                var properties = await _propertyRepository.GetByStateAsync(state, CurrentOrganizationId);
                var response = properties.Select(p => new PropertyResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties by state: {State}", state);
                return StatusCode(500, new { message = "An error occurred while retrieving properties" });
            }
        }
    }
}