using RentAll.Api.Dtos.Organizations.StateForms;
using RentAll.Api.Dtos.Properties.PropertyAgreements;
using RentAll.Api.Dtos.Contacts;
using RentAll.Domain.Models.Leads;
using System.Security.Cryptography;
using System.Text;

namespace RentAll.Api.Controllers
{
    public partial class CommonController
    {
        #region Get
        [HttpGet("owner-form/{token}")]
        public async Task<IActionResult> GetPublicOwnerFormByTokenAsync(string token)
        {
            try
            {
                var (share, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var ownerInventoryInformation = await _leadRepository.GetOwnerInventoryInformationByOwnerIdAsync(owner.OwnerId, owner.OrganizationId);
                return Ok(new PublicOwnerFormResponseDto(owner, ownerInventoryInformation, share.ExpiresOn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner form by token");
                return ServerError("An error occurred while retrieving owner form");
            }
        }

        [HttpGet("owner-form/{token}/stateforms")]
        public async Task<IActionResult> GetPublicOwnerFormStateFormsByTokenAsync(string token, [FromQuery] string? stateCode = null, [FromQuery] Guid? organizationId = null)
        {
            try
            {
                var (share, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var resolvedOrganizationId = organizationId ?? owner.OrganizationId;
                if (organizationId.HasValue && organizationId.Value != owner.OrganizationId)
                    return BadRequest("OrganizationId does not match owner form token");

                var ownerStateCode = (owner.State ?? string.Empty).Trim().ToUpperInvariant();
                if (ownerStateCode.Length != 2)
                {
                    Contact? ownerContact = null;
                    var ownerEmail = (owner.Email ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(ownerEmail))
                    {
                        ownerContact = await _contactRepository.GetContactByEmailAsync(ownerEmail, owner.OrganizationId);
                        if (ownerContact != null && (ownerContact.OwnerLeadId == null || ownerContact.OwnerLeadId <= 0))
                        {
                            ownerContact.OwnerLeadId = owner.OwnerId;
                            ownerContact.ModifiedBy = Guid.Empty;
                            ownerContact = await _contactRepository.UpdateByIdAsync(ownerContact);
                        }
                    }
                    if (ownerContact == null)
                    {
                        var officeAccess = owner.OfficeId > 0 ? owner.OfficeId.ToString() : string.Empty;
                        if (!string.IsNullOrWhiteSpace(officeAccess))
                        {
                            ownerContact = await _contactRepository.GetContactByLeadAsync(
                                owner.OrganizationId,
                                officeAccess,
                                owner.OwnerId,
                                owner.FirstName,
                                owner.LastName,
                                owner.Address
                            );
                        }
                    }
                    ownerStateCode = (ownerContact?.State ?? string.Empty).Trim().ToUpperInvariant();
                }
                var requestedStates = string.IsNullOrWhiteSpace(stateCode)
                    ? new[] { "XX", ownerStateCode }
                        .Where(state => !string.IsNullOrWhiteSpace(state) && state.Length == 2)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                    : new[] { stateCode.Trim().ToUpperInvariant() }
                        .Where(state => state.Length == 2)
                        .ToArray();

                if (requestedStates.Length == 0)
                    return Ok(Array.Empty<StateFormResponseDto>());

                var allForms = new List<StateForm>();
                foreach (var requestedStateCode in requestedStates)
                {
                    var stateForms = await _organizationRepository.GetStateFormsAsync(resolvedOrganizationId.ToString(), requestedStateCode);
                    allForms.AddRange(stateForms ?? Enumerable.Empty<StateForm>());
                }

                if (allForms.Count == 0)
                    return Ok(Array.Empty<StateFormResponseDto>());

                var organizationGuid = resolvedOrganizationId;
                var response = new List<StateFormResponseDto>();
                foreach (var stateForm in allForms)
                {
                    var dto = new StateFormResponseDto(stateForm);
                    if (organizationGuid != Guid.Empty)
                    {
                        dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                            organizationGuid,
                            $"{ImageType.StateForm}/{stateForm.StateCode.Trim().ToUpperInvariant()}",
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

        [HttpGet("owner-form/{token}/stateforms/{stateCode}")]
        public Task<IActionResult> GetPublicOwnerFormStateFormsByTokenAndStateAsync(string token, string stateCode, [FromQuery] Guid? organizationId = null)
        {
            return GetPublicOwnerFormStateFormsByTokenAsync(token, stateCode, organizationId);
        }

        [HttpGet("owner-form/{token}/lead-owner")]
        public async Task<IActionResult> GetPublicOwnerLeadByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                return Ok(new LeadOwnerResponseDto(owner));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner lead by token");
                return ServerError("An error occurred while retrieving owner lead");
            }
        }

        [HttpGet("owner-form/{token}/organization")]
        public async Task<IActionResult> GetPublicOwnerOrganizationByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var organization = await _organizationRepository.GetOrganizationByIdAsync(owner.OrganizationId);
                if (organization == null)
                    return NotFound("Organization not found");

                return Ok(new OrganizationResponseDto(organization));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner organization by token");
                return ServerError("An error occurred while retrieving owner organization");
            }
        }

        [HttpGet("owner-form/{token}/office")]
        public async Task<IActionResult> GetPublicOwnerOfficeByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var office = await _organizationRepository.GetOfficeByIdAsync(owner.OfficeId, owner.OrganizationId);
                if (office == null)
                    return NotFound("Office not found");

                return Ok(new OfficeResponseDto(office));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner office by token");
                return ServerError("An error occurred while retrieving owner office");
            }
        }

        [HttpGet("owner-form/{token}/offices")]
        public async Task<IActionResult> GetPublicOwnerOfficesByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var offices = await _organizationRepository.GetOfficesByOrganizationIdAsync(owner.OrganizationId);
                var response = (offices ?? Enumerable.Empty<Office>())
                    .Where(office => office.IsActive)
                    .Select(office => new OfficeResponseDto(office));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner offices by token");
                return ServerError("An error occurred while retrieving owner offices");
            }
        }

        [HttpGet("owner-form/{token}/accounting-office")]
        public async Task<IActionResult> GetPublicOwnerAccountingOfficeByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(owner.OrganizationId, owner.OfficeId);
                if (accountingOffice == null)
                    return Ok();

                var response = new AccountingOfficeResponseDto(accountingOffice);
                if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                {
                    var office = await _organizationRepository.GetOfficeByIdAsync(owner.OfficeId, owner.OrganizationId);
                    var officeName = office?.Name;
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                        accountingOffice.OrganizationId,
                        officeName,
                        accountingOffice.LogoPath,
                        ImageType.Logos);

                    // Fallback for legacy/variant storage scopes where logos may not resolve by office name.
                    if (response.FileDetails == null)
                    {
                        response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                            accountingOffice.OrganizationId,
                            null,
                            accountingOffice.LogoPath,
                            ImageType.Logos);
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner accounting office by token");
                return ServerError("An error occurred while retrieving owner accounting office");
            }
        }

        [HttpGet("owner-form/{token}/property")]
        public async Task<IActionResult> GetPublicOwnerPropertyByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var propertyCode = (owner.PropertyCode ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(propertyCode))
                    return Ok();

                var property = await _propertyRepository.GetPropertyByCodeAsync(propertyCode, owner.OrganizationId);
                if (property == null)
                    return Ok();

                return Ok(new PropertyResponseDto(property));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner property by token");
                return ServerError("An error occurred while retrieving owner property");
            }
        }

        [HttpGet("owner-form/{token}/property-agreement")]
        public async Task<IActionResult> GetPublicOwnerPropertyAgreementByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var propertyCode = (owner.PropertyCode ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(propertyCode))
                    return Ok();

                var property = await _propertyRepository.GetPropertyByCodeAsync(propertyCode, owner.OrganizationId);
                if (property == null)
                    return Ok();

                var propertyAgreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(property.PropertyId);
                if (propertyAgreement == null)
                    return Ok();

                return Ok(new PropertyAgreementResponseDto(propertyAgreement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner property agreement by token");
                return ServerError("An error occurred while retrieving owner property agreement");
            }
        }

        [HttpGet("owner-form/{token}/agreement-information")]
        public async Task<IActionResult> GetPublicOwnerAgreementInformationByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var property = await _propertyRepository.GetPropertyByCodeAsync(owner.PropertyCode!, owner.OrganizationId);
                var agreementInformation = await _leadRepository.GetOwnerAgreementInformationByScopeAsync(
                    owner.OrganizationId,
                    owner.OfficeId,
                    property != null ? property.PropertyId : null
                );

                if (agreementInformation == null)
                    return Ok();

                return Ok(new OwnerAgreementInformationResponseDto(agreementInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner agreement information by token");
                return ServerError("An error occurred while retrieving owner agreement information");
            }
        }

        [HttpGet("owner-form/{token}/templates")]
        public async Task<IActionResult> GetPublicOwnerTemplateHtmlByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var property = await _propertyRepository.GetPropertyByCodeAsync(owner.PropertyCode!, owner.OrganizationId);
                if (property == null)
                    return Ok();

                var ownerHtml = await _leadRepository.GetOwnerHtmlByPropertyIdAsync(property.PropertyId, owner.OrganizationId);
                if (ownerHtml == null)
                    return Ok();

                return Ok(new OwnerHtmlResponseDto(ownerHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner template html by token");
                return ServerError("An error occurred while retrieving owner template");
            }
        }

        [HttpGet("owner-form/{token}/owner-html")]
        public async Task<IActionResult> GetPublicOwnerHtmlByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var property = await _propertyRepository.GetPropertyByCodeAsync(owner.PropertyCode!, owner.OrganizationId);
                var propertyId = property?.PropertyId ?? Guid.Empty;
                var ownerHtml = await _leadRepository.GetOwnerHtmlByPropertyIdAsync(propertyId, owner.OrganizationId);
                if (ownerHtml == null)
                    return Ok();

                return Ok(new OwnerHtmlResponseDto(ownerHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner HTML by token");
                return ServerError("An error occurred while retrieving owner HTML");
            }
        }

        [HttpGet("owner-form/{token}/contact")]
        public async Task<IActionResult> GetPublicOwnerContactByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                Contact? contact = null;
                var email = (owner.Email ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    contact = await _contactRepository.GetContactByEmailAsync(email, owner.OrganizationId);
                    if (contact != null && (contact.OwnerLeadId == null || contact.OwnerLeadId <= 0))
                    {
                        contact.OwnerLeadId = owner.OwnerId;
                        contact.ModifiedBy = Guid.Empty;
                        contact = await _contactRepository.UpdateByIdAsync(contact);
                    }
                }
                if (contact == null)
                {
                    var officeAccess = owner.OfficeId > 0 ? owner.OfficeId.ToString() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(officeAccess))
                    {
                        contact = await _contactRepository.GetContactByLeadAsync(
                            owner.OrganizationId,
                            officeAccess,
                            owner.OwnerId,
                            owner.FirstName,
                            owner.LastName,
                            owner.Address
                        );
                    }
                }
                if (contact == null)
                    return Ok();

                return Ok(new ContactResponseDto(contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner contact by token");
                return ServerError("An error occurred while retrieving owner contact");
            }
        }

        [HttpGet("owner-form/{token}/contacts")]
        public async Task<IActionResult> GetPublicOwnerContactsByTokenAsync(string token)
        {
            try
            {
                var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                Contact? contact = null;
                var email = (owner.Email ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    contact = await _contactRepository.GetContactByEmailAsync(email, owner.OrganizationId);
                    if (contact != null && (contact.OwnerLeadId == null || contact.OwnerLeadId <= 0))
                    {
                        contact.OwnerLeadId = owner.OwnerId;
                        contact.ModifiedBy = Guid.Empty;
                        contact = await _contactRepository.UpdateByIdAsync(contact);
                    }
                }
                if (contact == null)
                {
                    var officeAccess = owner.OfficeId > 0 ? owner.OfficeId.ToString() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(officeAccess))
                    {
                        contact = await _contactRepository.GetContactByLeadAsync(
                            owner.OrganizationId,
                            officeAccess,
                            owner.OwnerId,
                            owner.FirstName,
                            owner.LastName,
                            owner.Address
                        );
                    }
                }
                if (contact == null)
                    return Ok(Array.Empty<ContactResponseDto>());

                return Ok(new[] { new ContactResponseDto(contact) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public owner contacts by token");
                return ServerError("An error occurred while retrieving owner contacts");
            }
        }
        #endregion

        #region Put
        [HttpPut("owner-form/{token}")]
        public async Task<IActionResult> SubmitPublicOwnerFormByTokenAsync(string token, [FromBody] SubmitPublicOwnerFormDto dto)
        {
            if (dto == null)
                return BadRequest("Owner form data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var (share, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

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

        [HttpPut("owner-form/{token}/contact")]
        public async Task<IActionResult> UpsertPublicOwnerContactByTokenAsync(string token, [FromBody] UpsertPublicOwnerContactDto? dto)
        {
            var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
            if (tokenErrorResult != null)
                return tokenErrorResult;

            try
            {
                var request = dto ?? new UpsertPublicOwnerContactDto();
                Contact? contact = null;
                var email = (owner.Email ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    contact = await _contactRepository.GetContactByEmailAsync(email, owner.OrganizationId);
                    if (contact != null && (contact.OwnerLeadId == null || contact.OwnerLeadId <= 0))
                    {
                        contact.OwnerLeadId = owner.OwnerId;
                        contact.ModifiedBy = Guid.Empty;
                        contact = await _contactRepository.UpdateByIdAsync(contact);
                    }
                }
                if (contact == null)
                {
                    var officeAccess = owner.OfficeId > 0 ? owner.OfficeId.ToString() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(officeAccess))
                    {
                        contact = await _contactRepository.GetContactByLeadAsync(
                            owner.OrganizationId,
                            officeAccess,
                            owner.OwnerId,
                            owner.FirstName,
                            owner.LastName,
                            owner.Address
                        );
                    }
                }
                var resolvedOfficeId = request.OfficeId.HasValue && request.OfficeId.Value > 0
                    ? request.OfficeId.Value
                    : owner.OfficeId;

                if (contact == null)
                {
                    var code = await _contactManager.GenerateContactCodeAsync(owner.OrganizationId, (int)EntityType.Owner);
                    var createdBy = Guid.Empty;
                    contact = await _contactRepository.CreateAsync(
                        request.ToNewOwnerContactModel(owner, code, resolvedOfficeId, createdBy)
                    );
                }
                else
                {
                    request.ApplyToExistingOwnerContact(contact, owner, resolvedOfficeId, Guid.Empty);
                    contact = await _contactRepository.UpdateByIdAsync(contact);
                }

                return Ok(new ContactResponseDto(contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting public owner contact by token");
                return ServerError("An error occurred while saving owner contact");
            }
        }

        [HttpPut("owner-form/{token}/property")]
        public async Task<IActionResult> UpsertPublicOwnerPropertyByTokenAsync(string token, [FromBody] UpsertPublicOwnerPropertyDto? dto)
        {
            var (_, owner, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
            if (tokenErrorResult != null)
                return tokenErrorResult;

            try
            {
                var request = dto ?? new UpsertPublicOwnerPropertyDto();
                var requestOrganizationId = Guid.TryParse((request.OrganizationId ?? string.Empty).Trim(), out var parsedOrganizationId)
                    ? parsedOrganizationId
                    : (Guid?)null;
                if (requestOrganizationId.HasValue && requestOrganizationId.Value != owner.OrganizationId)
                    return BadRequest("OrganizationId does not match owner form token");

                Contact? ownerContact = null;
                var ownerEmail = (owner.Email ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(ownerEmail))
                {
                    ownerContact = await _contactRepository.GetContactByEmailAsync(ownerEmail, owner.OrganizationId);
                    if (ownerContact != null && (ownerContact.OwnerLeadId == null || ownerContact.OwnerLeadId <= 0))
                    {
                        ownerContact.OwnerLeadId = owner.OwnerId;
                        ownerContact.ModifiedBy = Guid.Empty;
                        ownerContact = await _contactRepository.UpdateByIdAsync(ownerContact);
                    }
                }
                if (ownerContact == null)
                {
                    var officeAccess = owner.OfficeId > 0 ? owner.OfficeId.ToString() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(officeAccess))
                    {
                        ownerContact = await _contactRepository.GetContactByLeadAsync(
                            owner.OrganizationId,
                            officeAccess,
                            owner.OwnerId,
                            owner.FirstName,
                            owner.LastName,
                            owner.Address
                        );
                    }
                }

                var createDto = request.ToCreatePropertyDto(owner, ownerContact?.ContactId);
                createDto.Owner2Id = null;
                createDto.Owner3Id = null;

                var (isValid, errorMessage) = createDto.IsValid();
                if (!isValid)
                    return BadRequest(errorMessage ?? "Invalid request data");

                var existingProperty = await _propertyRepository.GetPropertyByCodeAsync(createDto.PropertyCode, owner.OrganizationId);
                if (existingProperty != null && existingProperty.OfficeId != createDto.OfficeId)
                    existingProperty = null;
                if (existingProperty == null)
                {
                    var created = await _propertyRepository.CreateAsync(createDto.ToModel(Guid.Empty));
                    var normalizedCreatedCode = (created.PropertyCode ?? string.Empty).Trim();
                    var ownerChanged = false;
                    if (!string.IsNullOrWhiteSpace(normalizedCreatedCode) &&
                        !string.Equals((owner.PropertyCode ?? string.Empty).Trim(), normalizedCreatedCode, StringComparison.OrdinalIgnoreCase))
                    {
                        owner.PropertyCode = normalizedCreatedCode;
                        ownerChanged = true;
                    }
                    if (created.OfficeId > 0 && owner.OfficeId != created.OfficeId)
                    {
                        owner.OfficeId = created.OfficeId;
                        ownerChanged = true;
                    }
                    if (ownerChanged)
                        await _leadRepository.UpdateOwnerByIdAsync(owner);
                    if (created.PropertyId != Guid.Empty && created.OfficeId > 0)
                    {
                        var existingAgreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(created.PropertyId);
                        if (existingAgreement == null)
                        {
                            await _propertyRepository.CreatePropertyAgreementAsync(new PropertyAgreement
                            {
                                PropertyId = created.PropertyId,
                                OfficeId = created.OfficeId,
                                ManagementFeeType = ManagementFeeType.FlatRate,
                                FlatRateAmount = 0m,
                                W9Path = null,
                                InsurancePath = null,
                                InsuranceExpiration = null,
                                AgreementPath = null,
                                Markup = 25,
                                RevenueSplitOwner = 75,
                                RevenueSplitOffice = 25,
                                WorkingCapitalBalance = 0m,
                                LinenAndTowelFee = 0m,
                                HourlyLaborCost = 0m,
                                BankName = null,
                                RoutingNumber = null,
                                AccountNumber = null,
                                RentalIncomeCcId = null,
                                RentalExpenseCcId = null,
                                Notes = null,
                                AgreementLines = new List<AgreementLine>()
                            });
                        }
                    }
                    return Ok(new PropertyResponseDto(created));
                }

                var updateDto = createDto.ToUpdateDto(existingProperty.PropertyId);
                var property = updateDto.ToModel(Guid.Empty);
                if (existingProperty.OfficeId != updateDto.OfficeId)
                    await _propertyManager.UpdatePropertyOfficeAsync(property, Guid.Empty);

                var updated = await _propertyRepository.UpdateByIdAsync(property);
                var normalizedUpdatedCode = (updated.PropertyCode ?? string.Empty).Trim();
                var ownerUpdated = false;
                if (!string.IsNullOrWhiteSpace(normalizedUpdatedCode) &&
                    !string.Equals((owner.PropertyCode ?? string.Empty).Trim(), normalizedUpdatedCode, StringComparison.OrdinalIgnoreCase))
                {
                    owner.PropertyCode = normalizedUpdatedCode;
                    ownerUpdated = true;
                }
                if (updated.OfficeId > 0 && owner.OfficeId != updated.OfficeId)
                {
                    owner.OfficeId = updated.OfficeId;
                    ownerUpdated = true;
                }
                if (ownerUpdated)
                    await _leadRepository.UpdateOwnerByIdAsync(owner);
                if (updated.PropertyId != Guid.Empty && updated.OfficeId > 0)
                {
                    var existingAgreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(updated.PropertyId);
                    if (existingAgreement == null)
                    {
                        await _propertyRepository.CreatePropertyAgreementAsync(new PropertyAgreement
                        {
                            PropertyId = updated.PropertyId,
                            OfficeId = updated.OfficeId,
                            ManagementFeeType = ManagementFeeType.FlatRate,
                            FlatRateAmount = 0m,
                            W9Path = null,
                            InsurancePath = null,
                            InsuranceExpiration = null,
                            AgreementPath = null,
                            Markup = 25,
                            RevenueSplitOwner = 75,
                            RevenueSplitOffice = 25,
                            WorkingCapitalBalance = 0m,
                            LinenAndTowelFee = 0m,
                            HourlyLaborCost = 0m,
                            BankName = null,
                            RoutingNumber = null,
                            AccountNumber = null,
                            RentalIncomeCcId = null,
                            RentalExpenseCcId = null,
                            Notes = null,
                            AgreementLines = new List<AgreementLine>()
                        });
                    }
                }
                return Ok(new PropertyResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting public owner property by token");
                return ServerError("An error occurred while saving owner property");
            }
        }
        #endregion

        #region Post
        [HttpPost("owner-form/{token}/generate-download")]
        public async Task<IActionResult> GeneratePublicOwnerDocumentDownloadByTokenAsync(string token, [FromBody] PublicOwnerDocumentDownloadDto dto)
        {
            if (dto == null)
                return BadRequest("Document data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var (_, _, tokenErrorResult) = await GetOwnerFromTokenAsync(token);
                if (tokenErrorResult != null)
                    return tokenErrorResult;

                var pdfBytes = await _pdfGenerationService.ConvertHtmlToPdfAsync(dto.HtmlContent);
                var fileName = string.IsNullOrWhiteSpace(dto.FileName) ? $"document-{Guid.NewGuid()}.pdf" : dto.FileName;
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating public owner document for download");
                return ServerError("An error occurred while generating the document");
            }
        }
        #endregion

        #region Delete
        // No DELETE owner-form endpoints at this time.
        #endregion

        #region Owner Form Helpers
        private async Task<(LeadOwnerFormShare Share, IActionResult? ErrorResult)> ValidateOwnerFormTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (null!, BadRequest("Token is required"));

            token = token.Trim()
                .Replace("\u00AD", string.Empty, StringComparison.Ordinal)
                .Replace("\u200B", string.Empty, StringComparison.Ordinal)
                .Replace("\u200C", string.Empty, StringComparison.Ordinal)
                .Replace("\u200D", string.Empty, StringComparison.Ordinal)
                .Replace("\uFEFF", string.Empty, StringComparison.Ordinal);
            var buffer = new char[token.Length];
            var written = 0;
            foreach (var ch in token)
            {
                buffer[written++] = ch switch
                {
                    '\u2010' or '\u2011' or '\u2012' or '\u2013' or '\u2014' or '\u2015' or '\u2212' or '\uFE58' or '\uFE63' or '\uFF0D' => '-',
                    _ => ch
                };
            }
            token = new string(buffer, 0, written);
            if (string.IsNullOrWhiteSpace(token))
                return (null!, BadRequest("Token is required"));

            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            var share = await _leadRepository.GetOwnerFormShareByTokenHashAsync(tokenHash);
            if (share == null)
                return (null!, NotFound("Owner form not found"));

            return (share, null);
        }

        private async Task<(LeadOwnerFormShare Share, LeadOwner Owner, IActionResult? ErrorResult)> GetOwnerFromTokenAsync(string token)
        {
            var (share, tokenErrorResult) = await ValidateOwnerFormTokenAsync(token);
            if (tokenErrorResult != null)
                return (null!, null!, tokenErrorResult);

            var owner = await _leadRepository.GetOwnerByIdAsync(share.OwnerId);
            if (owner == null || owner.OrganizationId != share.OrganizationId)
                return (null!, null!, NotFound("Owner form not found"));

            return (share, owner, null);
        }
        #endregion
    }
}
