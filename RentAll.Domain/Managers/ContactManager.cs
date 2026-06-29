using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class ContactManager : IContactManager
{
    private readonly IContactRepository _contactRepository;
    private readonly ICommonRepository _commonRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ContactManager(
        IContactRepository contactRepository,
        ICommonRepository commonRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _contactRepository = contactRepository;
        _commonRepository = commonRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<string> GenerateContactCodeAsync(Guid organizationId, int entityTypeId)
    {
        var entityType = (EntityType)entityTypeId;
        var prefix = entityType.ToCode();
        int nextNumber = await _commonRepository.GetNextCodeAsync(organizationId, entityTypeId, entityType.ToString());
        var code = $"C{prefix}-{nextNumber:D6}";

        return code;
    }

    public async Task<Contact> GenerateLoginForOwnerContact(Contact contact, Guid createdBy)
    {
        if (contact.EntityType != EntityType.Owner)
            return contact;

        var ownerEmail = (contact.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ownerEmail))
            return contact;

        var existingUser = await _userRepository.GetUserByEmailAsync(ownerEmail);
        if (existingUser != null)
        {
            var shouldUpdateContact = contact.UserId != existingUser.UserId;
            if (shouldUpdateContact)
                contact.UserId = existingUser.UserId;

            if (existingUser.ContactId == null)
            {
                existingUser.ContactId = contact.ContactId;
                existingUser.ModifiedBy = createdBy;
                await _userRepository.UpdateByIdAsync(existingUser);
            }

            if (shouldUpdateContact)
                return await _contactRepository.UpdateByIdAsync(contact);

            return contact;
        }

        var newUser = new User
        {
            OrganizationId = contact.OrganizationId,
            ContactId = contact.ContactId,
            FirstName = contact.FirstName ?? string.Empty,
            LastName = contact.LastName ?? string.Empty,
            Email = ownerEmail,
            Phone = contact.Phone ?? string.Empty,
            PasswordHash = _passwordHasher.HashPassword(contact.ContactCode),
            UserGroups = new List<string>() { "Owner" },
            OfficeAccess = contact.OfficeAccess != null && contact.OfficeAccess.Count > 0
                ? new List<int>(contact.OfficeAccess)
                : new List<int> { contact.OfficeId },
            ProfilePath = null,
            StartupPage = StartupPage.Dashboard,
            AgentId = null,
            CommissionRate = 0,
            CreatedBy = contact.CreatedBy
        };
        var createdUser = await _userRepository.CreateAsync(newUser);
        if (createdUser.UserId != Guid.Empty)
        {
            contact.UserId = createdUser.UserId;
            return await _contactRepository.UpdateByIdAsync(contact);
        }

        return contact;
    }

    public async Task<Contact> RetriggerLoginForOwnerContact(Contact contact, Guid currentUser)
    {
        if (contact.EntityType != EntityType.Owner)
            throw new Exception("Only owner contacts can be retriggered for login creation");

        var ownerEmail = (contact.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new Exception("Owner email is required to retrigger login creation");

        var contactCode = (contact.ContactCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(contactCode))
            throw new Exception("ContactCode is required to reset or create owner login");

        var passwordHash = _passwordHasher.HashPassword(contactCode);
        var existingUser = await _userRepository.GetUserByEmailAsync(ownerEmail);
        if (existingUser != null)
        {
            existingUser.PasswordHash = passwordHash;

            if (existingUser.ContactId == null || existingUser.ContactId == Guid.Empty)
                existingUser.ContactId = contact.ContactId;

            existingUser.ModifiedBy = currentUser;
            await _userRepository.UpdateByIdAsync(existingUser);

            if (contact.UserId != existingUser.UserId)
            {
                contact.UserId = existingUser.UserId;
                contact.ModifiedBy = currentUser;
                return await _contactRepository.UpdateByIdAsync(contact);
            }

            return contact;
        }

        var newUser = new User
        {
            OrganizationId = contact.OrganizationId,
            ContactId = contact.ContactId,
            FirstName = contact.FirstName ?? string.Empty,
            LastName = contact.LastName ?? string.Empty,
            Email = ownerEmail,
            Phone = contact.Phone ?? string.Empty,
            PasswordHash = passwordHash,
            UserGroups = new List<string>() { "Owner" },
            OfficeAccess = contact.OfficeAccess != null && contact.OfficeAccess.Count > 0
                ? new List<int>(contact.OfficeAccess)
                : new List<int> { contact.OfficeId },
            ProfilePath = null,
            StartupPage = StartupPage.Dashboard,
            AgentId = null,
            CommissionRate = 0,
            CreatedBy = currentUser
        };

        var createdUser = await _userRepository.CreateAsync(newUser);
        if (createdUser.UserId == Guid.Empty)
            throw new Exception("Unable to create owner user account");

        contact.UserId = createdUser.UserId;
        contact.ModifiedBy = currentUser;
        return await _contactRepository.UpdateByIdAsync(contact);
    }
}

