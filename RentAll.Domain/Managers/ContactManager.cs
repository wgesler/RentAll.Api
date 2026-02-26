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
        int nextNumber = await _commonRepository.GetNextAsync(organizationId, entityTypeId, entityType.ToString());
        var code = $"C{prefix}-{nextNumber:D6}";

        return code;
    }

    public async Task GenerateLoginForOwnerContact(Contact contact, Guid createdBy)
    {
        if (contact.EntityType == EntityType.Owner)
        {
            var newUser = new User
            {
                OrganizationId = contact.OrganizationId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                Phone = contact.Phone,
                PasswordHash = _passwordHasher.HashPassword(contact.ContactCode),
                UserGroups = new List<string>() { "Owner" },
                OfficeAccess = new List<int>() { contact.OfficeId },
                ProfilePath = null,
                StartupPage = StartupPage.Dashboard,
                AgentId = null,
                CommissionRate = 0,
                CreatedBy = contact.CreatedBy
            };
            await _userRepository.CreateAsync(newUser);
        }
    }
}

