using RentAll.Api.Dtos.Contacts;
using RentAll.Api.Dtos.Contacts.ContactCards;

namespace RentAll.Api.Controllers;

public partial class ContactController
{
    #region Contact Card Get
    [HttpGet("{contactId:guid}/card/{contactCardId:int}/pan")]
    public async Task<IActionResult> GetContactCardPanAsync(Guid contactId, int contactCardId)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (contactCardId <= 0)
            return BadRequest("Contact card ID is required");

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            if (contact.ContactCardId != contactCardId)
                return NotFound("Contact card not found");

            var existing = await _contactRepository.GetContactCardByIdAsync(contactCardId, contact.OrganizationId, contact.OfficeId);
            if (existing == null)
                return NotFound("Contact card not found");

            // Cards held by Number have no number here to decrypt.
            if (existing.IsExternallyStored || string.IsNullOrWhiteSpace(existing.CardNumber))
                return Ok(new ContactCardPanResponseDto(string.Empty));

            var cipherBytes = Convert.FromBase64String(existing.CardNumber);
            var cardNumber = await _encryptionService.DecryptAsync(cipherBytes);
            return Ok(new ContactCardPanResponseDto(cardNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact card PAN {ContactCardId} for contact {ContactId}", contactCardId, contactId);
            return ServerError("An error occurred while retrieving the contact card number");
        }
    }
    #endregion

    #region Contact Card Post
    [HttpPost("{contactId:guid}/card")]
    public async Task<IActionResult> CreateContactCardAsync(Guid contactId, [FromBody] CreateContactCardDto dto)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (dto == null)
            return BadRequest("Contact card data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid contact card data");

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            var created = await CreateContactCardInternalAsync(contact.OrganizationId, contact.OfficeId, dto);
            contact.ContactCardId = created.ContactCardId;
            contact.ModifiedBy = CurrentUser;
            await _contactRepository.UpdateByIdAsync(contact);
            return Ok(new ContactCardResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact card for contact {ContactId}", contactId);
            return ServerError("An error occurred while creating the contact card");
        }
    }
    #endregion

    #region Contact Card Put
    [HttpPut("{contactId:guid}/card/{contactCardId:int}")]
    public async Task<IActionResult> UpdateContactCardAsync(Guid contactId, int contactCardId, [FromBody] UpdateContactCardDto dto)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (contactCardId <= 0)
            return BadRequest("Contact card ID is required");

        if (dto == null)
            return BadRequest("Contact card data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid contact card data");

        if (dto.ContactCardId != contactCardId)
            return BadRequest("ContactCardId does not match route");

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            if (contact.ContactCardId != contactCardId)
                return NotFound("Contact card not found");

            var existing = await _contactRepository.GetContactCardByIdAsync(contactCardId, contact.OrganizationId, contact.OfficeId);
            if (existing == null)
                return NotFound("Contact card not found");

            var updated = await UpdateContactCardInternalAsync(contact.OrganizationId, contact.OfficeId, dto, existing);
            return Ok(new ContactCardResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact card {ContactCardId} for contact {ContactId}", contactCardId, contactId);
            return ServerError("An error occurred while updating the contact card");
        }
    }
    #endregion

    #region Contact Card Delete
    [HttpDelete("{contactId:guid}/card/{contactCardId:int}")]
    public async Task<IActionResult> DeleteContactCardAsync(Guid contactId, int contactCardId)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (contactCardId <= 0)
            return BadRequest("Contact card ID is required");

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            if (contact.ContactCardId != contactCardId)
                return NotFound("Contact card not found");

            var existing = await _contactRepository.GetContactCardByIdAsync(contactCardId, contact.OrganizationId, contact.OfficeId);
            if (existing == null)
                return NotFound("Contact card not found");

            await _contactRepository.DeleteContactCardByIdAsync(contactCardId, contact.OrganizationId, contact.OfficeId);
            contact.ContactCardId = null;
            contact.ModifiedBy = CurrentUser;
            await _contactRepository.UpdateByIdAsync(contact);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact card {ContactCardId} for contact {ContactId}", contactCardId, contactId);
            return ServerError("An error occurred while deleting the contact card");
        }
    }
    #endregion

    #region Contact Card Private Methods
    private async Task<Contact?> GetAuthorizedContactAsync(Guid contactId)
    {
        if (CurrentOrganizationId == SuperAdminOrganizationId && !IsSuperAdmin())
            return null;

        return IsSuperAdmin()
            ? await _contactRepository.GetContactByIdAsync(contactId)
            : await _contactRepository.GetContactByIdsAsync(contactId, CurrentOrganizationId);
    }

    private async Task AttachContactCardAsync(ContactResponseDto response, Contact contact)
    {
        if (contact.ContactCardId is not > 0)
            return;

        var card = await _contactRepository.GetContactCardByIdAsync(contact.ContactCardId.Value, contact.OrganizationId, contact.OfficeId);
        if (card == null)
            return;

        response.ContactCardId = card.ContactCardId;
        response.ContactCard = new ContactCardResponseDto(card);
    }

    private async Task<int?> ResolveSavedContactCardIdAsync(Guid organizationId, int officeId, int? existingCardId, int? requestedCardId, UpsertContactCardDto? card)
    {
        var cardId = card != null
            ? await SyncContactCardAsync(organizationId, officeId, existingCardId, card)
            : requestedCardId is > 0 ? requestedCardId : existingCardId is > 0 ? existingCardId : null;

        if (cardId is not > 0)
            return null;

        var saved = await _contactRepository.GetContactCardByIdAsync(cardId.Value, organizationId, officeId);
        if (saved != null)
            return cardId;

        _logger.LogInformation("[ContactSaveTrace] Step=DropMissingCard ContactCardId={ContactCardId} OfficeId={OfficeId}", cardId, officeId);
        return null;
    }

    private async Task<int?> SyncContactCardAsync(Guid organizationId, int officeId, int? existingCardId, UpsertContactCardDto? card)
    {
        if (card == null)
            return existingCardId is > 0 ? existingCardId : null;

        var cardId = card.ContactCardId is > 0 ? card.ContactCardId : existingCardId;
        if (card.IsEmpty())
        {
            if (cardId is > 0)
                await _contactRepository.DeleteContactCardByIdAsync(cardId.Value, organizationId, officeId);

            return null;
        }

        if (cardId is > 0)
        {
            var existing = await _contactRepository.GetContactCardByIdAsync(cardId.Value, organizationId, officeId);
            if (existing == null)
                return existingCardId;

            var updateDto = new UpdateContactCardDto { ContactCardId = cardId.Value, CardTypeId = card.CardTypeId, CardName = card.CardName, CardNumber = card.CardNumber };
            var updated = await UpdateContactCardInternalAsync(organizationId, officeId, updateDto, existing);
            return updated.ContactCardId;
        }

        var createDto = new CreateContactCardDto { CardTypeId = card.CardTypeId, CardName = card.CardName, CardNumber = card.CardNumber };
        var created = await CreateContactCardInternalAsync(organizationId, officeId, createDto);
        return created.ContactCardId;
    }

    // ContactCardStore skips encryption when the office's payment integration is Number.
    private Task<ContactCard> CreateContactCardInternalAsync(Guid organizationId, int officeId, CreateContactCardDto dto)
    {
        var model = dto.ToModel(organizationId, officeId);
        return _contactCardStore.CreateAsync(model, model.CardNumber);
    }

    private Task<ContactCard> UpdateContactCardInternalAsync(Guid organizationId, int officeId, UpdateContactCardDto dto, ContactCard existing)
    {
        var model = dto.ToModel(organizationId, officeId);
        return _contactCardStore.UpdateAsync(model, dto.CardNumber, existing);
    }
    #endregion
}
