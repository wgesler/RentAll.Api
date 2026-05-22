using RentAll.Api.Dtos.Organizations.StateForms;
using RentAll.Api.Dtos.Properties.PropertyAgreements;
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

        [HttpGet("owner-form/{token}/stateforms")]
        public async Task<IActionResult> GetPublicOwnerFormStateFormsByTokenAsync(string token)
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

                var ownerStateCode = (owner.State ?? string.Empty).Trim().ToUpperInvariant();
                var requestedStates = new[] { "XX", ownerStateCode }
                    .Where(state => !string.IsNullOrWhiteSpace(state) && state.Length == 2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (requestedStates.Length == 0)
                    return Ok(Array.Empty<StateFormResponseDto>());

                var allForms = new List<StateForm>();
                foreach (var stateCode in requestedStates)
                {
                    var stateForms = await _organizationRepository.GetStateFormsAsync(share.OrganizationId.ToString(), stateCode);
                    allForms.AddRange(stateForms ?? Enumerable.Empty<StateForm>());
                }

                if (allForms.Count == 0)
                    return Ok(Array.Empty<StateFormResponseDto>());

                var organizationGuid = share.OrganizationId;
                var response = new List<StateFormResponseDto>();
                foreach (var stateForm in allForms)
                {
                    var dto = new StateFormResponseDto(stateForm);
                    if (organizationGuid != Guid.Empty)
                    {
                        dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                            organizationGuid,
                            GetStateFormStorageScope(stateForm.StateCode),
                            stateForm.Path,
                            ImageType.StateForm);
                    }
                    response.Add(dto);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner state forms by token");
                return ServerError("An error occurred while retrieving owner state forms");
            }
        }

        [HttpGet("owner-form/{token}/lead-owner")]
        public async Task<IActionResult> GetPublicOwnerLeadByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            return Ok(new LeadOwnerResponseDto(owner));
        }

        [HttpGet("owner-form/{token}/organization")]
        public async Task<IActionResult> GetPublicOwnerOrganizationByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            var organization = await _organizationRepository.GetOrganizationByIdAsync(owner.OrganizationId);
            if (organization == null)
                return NotFound("Organization not found");

            return Ok(new OrganizationResponseDto(organization));
        }

        [HttpGet("owner-form/{token}/office")]
        public async Task<IActionResult> GetPublicOwnerOfficeByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            var office = await _organizationRepository.GetOfficeByIdAsync(owner.OfficeId, owner.OrganizationId);
            if (office == null)
                return NotFound("Office not found");

            return Ok(new OfficeResponseDto(office));
        }

        [HttpGet("owner-form/{token}/accounting-office")]
        public async Task<IActionResult> GetPublicOwnerAccountingOfficeByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(owner.OrganizationId, owner.OfficeId);
            if (accountingOffice == null)
                return Ok();

            return Ok(new AccountingOfficeResponseDto(accountingOffice));
        }

        [HttpGet("owner-form/{token}/property")]
        public async Task<IActionResult> GetPublicOwnerPropertyByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            var property = await ResolvePropertyForOwnerAsync(owner);
            if (property == null)
                return Ok();

            return Ok(new PropertyResponseDto(property));
        }

        [HttpGet("owner-form/{token}/property-agreement")]
        public async Task<IActionResult> GetPublicOwnerPropertyAgreementByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            var property = await ResolvePropertyForOwnerAsync(owner);
            if (property == null)
                return Ok();

            var propertyAgreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(property.PropertyId);
            if (propertyAgreement == null)
                return Ok();

            return Ok(new PropertyAgreementResponseDto(propertyAgreement));
        }

        [HttpGet("owner-form/{token}/agreement-information")]
        public async Task<IActionResult> GetPublicOwnerAgreementInformationByTokenAsync(string token)
        {
            var (share, owner, tokenError) = await ResolveOwnerFormContextAsync(token);
            if (!string.IsNullOrWhiteSpace(tokenError))
                return BadRequest(tokenError);
            if (share == null || owner == null)
                return NotFound("Owner form not found");

            var property = await ResolvePropertyForOwnerAsync(owner);
            var propertyId = property?.PropertyId;
            var agreementInformation = await _leadRepository.GetOwnerAgreementInformationByScopeAsync(
                owner.OrganizationId,
                owner.OfficeId,
                propertyId
            );

            if (agreementInformation == null)
                return Ok();

            return Ok(new OwnerAgreementInformationResponseDto(agreementInformation));
        }

        private async Task<(LeadOwnerFormShare? share, LeadOwner? owner, string? tokenError)> ResolveOwnerFormContextAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (null, null, "Token is required");

            token = NormalizeOwnerFormShareToken(token);
            if (string.IsNullOrWhiteSpace(token))
                return (null, null, "Token is required");

            var tokenHash = ComputeOwnerFormSha256Hex(token);
            var share = await _leadRepository.GetOwnerFormShareByTokenHashAsync(tokenHash);
            if (share == null)
                return (null, null, null);

            var owner = await _leadRepository.GetOwnerByIdAsync(share.OwnerId);
            if (owner == null || owner.OrganizationId != share.OrganizationId)
                return (null, null, null);

            return (share, owner, null);
        }

        private async Task<Property?> ResolvePropertyForOwnerAsync(LeadOwner owner)
        {
            if (owner == null)
                return null;

            var propertyCode = (owner.PropertyCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(propertyCode))
                return null;

            var officeAccess = owner.OfficeId > 0 ? owner.OfficeId.ToString() : string.Empty;
            if (string.IsNullOrWhiteSpace(officeAccess))
                return null;

            var properties = await _propertyRepository.GetPropertyListByOfficeIdsAsync(owner.OrganizationId, officeAccess);
            var propertyListMatch = (properties ?? Enumerable.Empty<PropertyList>()).FirstOrDefault(p =>
                string.Equals((p.PropertyCode ?? string.Empty).Trim(), propertyCode, StringComparison.OrdinalIgnoreCase)
            );
            if (propertyListMatch == null)
                return null;

            return await _propertyRepository.GetPropertyByIdAsync(propertyListMatch.PropertyId, owner.OrganizationId);
        }

        private static string GetStateFormStorageScope(string stateCode)
        {
            return $"{ImageType.StateForm}/{stateCode.Trim().ToUpperInvariant()}";
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
