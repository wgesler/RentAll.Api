namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Get

        [HttpGet("terminology")]
        public async Task<IActionResult> GetAllTerminologies()
        {
            try
            {
                var terminologies = await _organizationRepository.GetTerminologiesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                return Ok(terminologies.Select(terminology => new TerminologyResponseDto(terminology)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting terminology");
                return ServerError("An error occurred while retrieving terminology");
            }
        }

        [HttpGet("terminology/by-term")]
        public async Task<IActionResult> GetTerminologyByTerm([FromQuery] string term, [FromQuery] int officeId)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest("Term is required");

            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var terminology = await _organizationRepository.GetTerminologyByTermAsync(term.Trim(), CurrentOrganizationId, officeId);
                if (terminology == null)
                    return NotFound("Term not found");

                return Ok(new TerminologyResponseDto(terminology));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting terminology by term: {Term}", term);
                return ServerError("An error occurred while retrieving the term");
            }
        }

        [HttpGet("terminology/{termId:int}")]
        public async Task<IActionResult> GetTerminologyById(int termId)
        {
            if (termId <= 0)
                return BadRequest("Term ID is required");

            try
            {
                var terminology = await _organizationRepository.GetTerminologyByIdAsync(termId, CurrentOrganizationId);
                if (terminology == null)
                    return NotFound("Term not found");

                return Ok(new TerminologyResponseDto(terminology));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting terminology by ID: {TermId}", termId);
                return ServerError("An error occurred while retrieving the term");
            }
        }

        #endregion

        #region Post

        [HttpPost("terminology")]
        public async Task<IActionResult> CreateTerminology([FromBody] TerminologyCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Terminology data is required");

            if (dto.OrganizationId != CurrentOrganizationId)
                return Unauthorized("Invalid organization Id");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var terminology = dto.ToModel();
                if (await _organizationRepository.GetTerminologyByTermAsync(terminology.Term, CurrentOrganizationId, terminology.OfficeId) != null)
                    return Conflict("Term already exists");

                var created = await _organizationRepository.CreateTerminologyAsync(terminology);
                return Ok(new TerminologyResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating terminology");
                return ServerError("An error occurred while creating the term");
            }
        }

        #endregion

        #region Put

        [HttpPut("terminology")]
        public async Task<IActionResult> UpdateTerminology([FromBody] TerminologyUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Terminology data is required");

            if (dto.OrganizationId != CurrentOrganizationId)
                return Unauthorized("Invalid organization Id");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetTerminologyByIdAsync(dto.TermId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Term not found");

                var terminology = dto.ToModel();
                var duplicate = await _organizationRepository.GetTerminologyByTermAsync(terminology.Term, CurrentOrganizationId, terminology.OfficeId);
                if (duplicate != null && duplicate.TermId != terminology.TermId)
                    return Conflict("Term already exists");

                var updated = await _organizationRepository.UpdateTerminologyByIdAsync(terminology);
                return Ok(new TerminologyResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating terminology: {TermId}", dto.TermId);
                return ServerError("An error occurred while updating the term");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("terminology/{termId:int}")]
        public async Task<IActionResult> DeleteTerminologyById(int termId)
        {
            if (termId <= 0)
                return BadRequest("Term ID is required");

            try
            {
                var terminology = await _organizationRepository.GetTerminologyByIdAsync(termId, CurrentOrganizationId);
                if (terminology == null)
                    return NotFound("Term not found");

                await _organizationRepository.DeleteTerminologyByIdAsync(termId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting terminology: {TermId}", termId);
                return ServerError("An error occurred while deleting the term");
            }
        }

        #endregion
    }
}
