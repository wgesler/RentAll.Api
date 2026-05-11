using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<IEnumerable<LeadOwner>> GetOwnersAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerEntity>("dLeads.Owner_GetAll", new { });

        if (res == null || !res.Any())
            return Enumerable.Empty<LeadOwner>();

        return res.Select(ConvertOwnerEntityToModel);
    }

    public async Task<LeadOwner?> GetOwnerByIdAsync(int ownerId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerEntity>("dLeads.Owner_GetById", new { OwnerId = ownerId });

        if (res == null || !res.Any())
            return null;

        return ConvertOwnerEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<LeadOwner> CreateOwnerAsync(LeadOwner owner)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerEntity>("dLeads.Owner_Add", new
        {
            LeadStateId = (int)owner.LeadState,
            AgentId = owner.AgentId,
            FirstName = owner.FirstName,
            LastName = owner.LastName,
            Email = owner.Email,
            Phone = owner.Phone,
            LocationOfProperty = owner.LocationOfProperty,
            ProgramInterest = owner.ProgramInterest,
            WhatIsPromptingContact = owner.WhatIsPromptingContact,
            TimeFrame = owner.TimeFrame,
            TargetRentReadyDate = ToSqlDate(owner.TargetRentReadyDate),
            PropertyGoals = owner.PropertyGoals,
            TellUsMoreAboutYourGoals = owner.TellUsMoreAboutYourGoals,
            YearsOfExperienceWithRentals = owner.YearsOfExperienceWithRentals,
            TellUsMoreAboutProperty = owner.TellUsMoreAboutProperty,
            Address = owner.Address,
            City = owner.City,
            State = owner.State,
            Zip = owner.Zip,
            NumberOfBeds = owner.NumberOfBeds,
            NumberOfBaths = owner.NumberOfBaths,
            ApproxSqFootage = owner.ApproxSqFootage,
            TypeOfProperty = owner.TypeOfProperty,
            TellUsWhatYouLikeMostAboutYourProperty = owner.TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = owner.TellUsAnyDrawbacks,
            PreferredContactMethod = owner.PreferredContactMethod,
            TimeDateForContact = owner.TimeDateForContact,
            EmailPhoneConsent = owner.EmailPhoneConsent,
            SmsConsent = owner.SmsConsent,
            IsActive = owner.IsActive
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner lead was not created.");

        return ConvertOwnerEntityToModel(res.First());
    }

    #endregion

    #region Updates

    public async Task<LeadOwner> UpdateOwnerByIdAsync(LeadOwner owner)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerEntity>("dLeads.Owner_UpdateById", new
        {
            OwnerId = owner.OwnerId,
            LeadStateId = (int)owner.LeadState,
            AgentId = owner.AgentId,
            FirstName = owner.FirstName,
            LastName = owner.LastName,
            Email = owner.Email,
            Phone = owner.Phone,
            LocationOfProperty = owner.LocationOfProperty,
            ProgramInterest = owner.ProgramInterest,
            WhatIsPromptingContact = owner.WhatIsPromptingContact,
            TimeFrame = owner.TimeFrame,
            TargetRentReadyDate = ToSqlDate(owner.TargetRentReadyDate),
            PropertyGoals = owner.PropertyGoals,
            TellUsMoreAboutYourGoals = owner.TellUsMoreAboutYourGoals,
            YearsOfExperienceWithRentals = owner.YearsOfExperienceWithRentals,
            TellUsMoreAboutProperty = owner.TellUsMoreAboutProperty,
            Address = owner.Address,
            City = owner.City,
            State = owner.State,
            Zip = owner.Zip,
            NumberOfBeds = owner.NumberOfBeds,
            NumberOfBaths = owner.NumberOfBaths,
            ApproxSqFootage = owner.ApproxSqFootage,
            TypeOfProperty = owner.TypeOfProperty,
            TellUsWhatYouLikeMostAboutYourProperty = owner.TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = owner.TellUsAnyDrawbacks,
            PreferredContactMethod = owner.PreferredContactMethod,
            TimeDateForContact = owner.TimeDateForContact,
            EmailPhoneConsent = owner.EmailPhoneConsent,
            SmsConsent = owner.SmsConsent,
            IsActive = owner.IsActive
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Owner lead was not found or not updated.");

        return ConvertOwnerEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeleteOwnerByIdAsync(int ownerId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("dLeads.Owner_DeleteById", new { OwnerId = ownerId });
    }

    #endregion
}
