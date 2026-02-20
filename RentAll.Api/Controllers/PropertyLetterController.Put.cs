using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Properties.PropertyLetters;

namespace RentAll.Api.Controllers
{
    public partial class PropertyLetterController
    {
        /// <summary>
        /// Update an existing property letter
        /// </summary>
        /// <param name="dto">Property letter data</param>
        /// <returns>Updated property letter</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePropertyLetterDto dto)
        {
            if (dto == null)
                return BadRequest("Property letter data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var propertyLetter = dto.ToModel(CurrentUser);

                // Check if property letter exists
                var existing = await _propertyRepository.GetPropertyLetterByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (existing == null)
                {
                    var addPropertyLetter = await _propertyRepository.CreatePropertyLetterAsync(propertyLetter);
                    return Ok(new PropertyLetterResponseDto(addPropertyLetter));
                }
                else
                {
                    var updatedPropertyLetter = await _propertyRepository.UpdatePropertyLetterByIdAsync(propertyLetter);
                    return Ok(new PropertyLetterResponseDto(updatedPropertyLetter));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property letter: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while updating the property letter");
            }
        }
    }
}


