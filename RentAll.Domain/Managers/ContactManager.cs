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

        var newUser = new User
        {
            OrganizationId = contact.OrganizationId,
            ContactId = contact.ContactId,
            FirstName = contact.FirstName ?? string.Empty,
            LastName = contact.LastName ?? string.Empty,
            Email = contact.Email,
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
}

