using RentAll.Api.Dtos.Organizations.StateForms;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        private static string GetStateFormStorageScope(string stateCode)
        {
            return $"{ImageType.StateForm}/{stateCode.Trim().ToUpperInvariant()}";
        }

        #region Get

        [HttpGet("stateform")]
        public async Task<IActionResult> GetAllStateForms([FromQuery] string stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode) || stateCode.Trim().Length != 2)
                return BadRequest("State Code is required and must be 2 characters");

            try
            {
                var organizationId = CurrentOrganizationId.ToString();
                var stateForms = await _organizationRepository.GetStateFormsAsync(organizationId, stateCode.Trim().ToUpperInvariant());
                var response = new List<StateFormResponseDto>();
                foreach (var stateForm in stateForms)
                {
                    var dto = new StateFormResponseDto(stateForm);
                    dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                        CurrentOrganizationId, GetStateFormStorageScope(stateForm.StateCode), stateForm.Path, ImageType.StateForm);
                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state forms for state code: {StateCode}", stateCode);
                return ServerError("An error occurred while retrieving state forms");
            }
        }

        [HttpGet("stateform/{stateFormId}")]
        public async Task<IActionResult> GetStateFormById(int stateFormId)
        {
            if (stateFormId <= 0)
                return BadRequest("State Form ID is required");

            try
            {
                var organizationId = CurrentOrganizationId.ToString();
                var stateForm = await _organizationRepository.GetStateFormByIdAsync(stateFormId, organizationId);
                if (stateForm == null)
                    return NotFound("State form not found");

                var response = new StateFormResponseDto(stateForm);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    CurrentOrganizationId, GetStateFormStorageScope(stateForm.StateCode), stateForm.Path, ImageType.StateForm);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state form by ID: {StateFormId}", stateFormId);
                return ServerError("An error occurred while retrieving the state form");
            }
        }

        #endregion

        #region Post

        [HttpPost("stateform")]
        public async Task<IActionResult> CreateStateForm([FromBody] StateFormCreateDto dto)
        {
            if (dto == null)
                return BadRequest("State form data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var organizationId = CurrentOrganizationId.ToString();
                var stateForm = dto.ToModel(organizationId);
                var storageScope = GetStateFormStorageScope(stateForm.StateCode);
                stateForm.Path = await _fileAttachmentHelper.SaveImageIfPresentAsync(
                    CurrentOrganizationId, storageScope, dto.FileDetails, ImageType.StateForm) ?? string.Empty;
                var createdStateForm = await _organizationRepository.CreateStateFormAsync(stateForm);
                var response = new StateFormResponseDto(createdStateForm);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    CurrentOrganizationId, GetStateFormStorageScope(createdStateForm.StateCode), createdStateForm.Path, ImageType.StateForm);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating state form");
                return ServerError("An error occurred while creating the state form");
            }
        }

        #endregion

        #region Put

        [HttpPut("stateform")]
        public async Task<IActionResult> UpdateStateForm([FromBody] StateFormUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("State form data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var organizationId = CurrentOrganizationId.ToString();
                var existingStateForm = await _organizationRepository.GetStateFormByIdAsync(dto.StateFormId, organizationId);
                if (existingStateForm == null)
                    return NotFound("State form not found");

                var stateForm = dto.ToModel(organizationId);
                var storageScope = GetStateFormStorageScope(stateForm.StateCode);
                var existingStorageScope = GetStateFormStorageScope(existingStateForm.StateCode);
                var existingPath = existingStateForm.Path;

                if (!string.Equals(existingStorageScope, storageScope, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(existingPath)
                    && (dto.FileDetails?.File?.Length > 0 || dto.Path == null))
                {
                    await _fileService.DeleteImageAsync(CurrentOrganizationId, existingStorageScope, existingPath, ImageType.StateForm);
                    existingPath = null;
                }

                stateForm.Path = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    CurrentOrganizationId, storageScope, dto.FileDetails, ImageType.StateForm, existingPath, dto.Path) ?? string.Empty;
                var updatedStateForm = await _organizationRepository.UpdateStateFormByIdAsync(stateForm);
                var response = new StateFormResponseDto(updatedStateForm);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    CurrentOrganizationId, GetStateFormStorageScope(updatedStateForm.StateCode), updatedStateForm.Path, ImageType.StateForm);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating state form: {StateFormId}", dto.StateFormId);
                return ServerError("An error occurred while updating the state form");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("stateform/{stateFormId}")]
        public async Task<IActionResult> DeleteStateFormByIdAsync(int stateFormId)
        {
            if (stateFormId <= 0)
                return BadRequest("State Form ID is required");

            try
            {
                var organizationId = CurrentOrganizationId.ToString();
                var stateForm = await _organizationRepository.GetStateFormByIdAsync(stateFormId, organizationId);
                if (stateForm == null)
                    return NotFound("State form not found");

                if (!string.IsNullOrWhiteSpace(stateForm.Path))
                    await _fileService.DeleteImageAsync(
                        CurrentOrganizationId, GetStateFormStorageScope(stateForm.StateCode), stateForm.Path, ImageType.StateForm);

                await _organizationRepository.DeleteStateFormByIdAsync(stateFormId, organizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting state form: {StateFormId}", stateFormId);
                return ServerError("An error occurred while deleting the state form");
            }
        }

        #endregion
    }
}
