using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    #region Selects

    public async Task<IEnumerable<LeadRental>> GetRentalsByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RentalEntity>("Lead.Rental_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeIds
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<LeadRental>();

        return res.Select(ConvertRentalEntityToModel);
    }

    public async Task<LeadRental?> GetRentalByIdAsync(int rentalId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RentalEntity>("Lead.Rental_GetById", new { RentalId = rentalId });

        if (res == null || !res.Any())
            return null;

        return ConvertRentalEntityToModel(res.First());
    }

    #endregion

    #region Creates

    public async Task<LeadRental> CreateRentalAsync(LeadRental rental)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RentalEntity>("Lead.Rental_Add", new
        {
            OrganizationId = rental.OrganizationId,
            OfficeId = rental.OfficeId,
            LeadStateId = (int)rental.LeadState,
            AgentId = rental.AgentId,
            FirstName = rental.FirstName,
            LastName = rental.LastName,
            Email = rental.Email,
            Phone = rental.Phone,
            DesiredLocation = rental.DesiredLocation,
            PropertyRefId = rental.PropertyRefId,
            EstimatedArrivalDate = rental.EstimatedArrivalDate,
            EstimatedDepartureDate = rental.EstimatedDepartureDate,
            MaxMonthlyBudget = rental.MaxMonthlyBudget,
            MinBedrooms = rental.MinBedrooms,
            NumberOfOccupants = rental.NumberOfOccupants,
            WhatBringsYouToTown = rental.WhatBringsYouToTown,
            HowDidYouFindUs = rental.HowDidYouFindUs,
            TellUsMoreAboutHowYouFoundUs = rental.TellUsMoreAboutHowYouFoundUs,
            PetFriendly = rental.PetFriendly,
            DecisionDate = ToSqlDate(rental.DecisionDate),
            OrganizationName = rental.OrganizationName,
            AdditionalInformation = rental.AdditionalInformation,
            INeedAsap = rental.INeedAsap,
            EmailPhoneConsent = rental.EmailPhoneConsent,
            SmsConsent = rental.SmsConsent,
            IsActive = rental.IsActive
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Rental lead was not created.");

        return ConvertRentalEntityToModel(res.First());
    }

    #endregion

    #region Updates

    public async Task<LeadRental> UpdateRentalByIdAsync(LeadRental rental)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RentalEntity>("Lead.Rental_UpdateById", new
        {
            RentalId = rental.RentalId,
            OrganizationId = rental.OrganizationId,
            OfficeId = rental.OfficeId,
            LeadStateId = (int)rental.LeadState,
            AgentId = rental.AgentId,
            FirstName = rental.FirstName,
            LastName = rental.LastName,
            Email = rental.Email,
            Phone = rental.Phone,
            DesiredLocation = rental.DesiredLocation,
            PropertyRefId = rental.PropertyRefId,
            EstimatedArrivalDate = rental.EstimatedArrivalDate,
            EstimatedDepartureDate = rental.EstimatedDepartureDate,
            MaxMonthlyBudget = rental.MaxMonthlyBudget,
            MinBedrooms = rental.MinBedrooms,
            NumberOfOccupants = rental.NumberOfOccupants,
            WhatBringsYouToTown = rental.WhatBringsYouToTown,
            HowDidYouFindUs = rental.HowDidYouFindUs,
            TellUsMoreAboutHowYouFoundUs = rental.TellUsMoreAboutHowYouFoundUs,
            PetFriendly = rental.PetFriendly,
            DecisionDate = ToSqlDate(rental.DecisionDate),
            OrganizationName = rental.OrganizationName,
            AdditionalInformation = rental.AdditionalInformation,
            INeedAsap = rental.INeedAsap,
            EmailPhoneConsent = rental.EmailPhoneConsent,
            SmsConsent = rental.SmsConsent,
            IsActive = rental.IsActive
        });

        if (res == null || !res.Any())
            throw new InvalidOperationException("Rental lead was not found or not updated.");

        return ConvertRentalEntityToModel(res.First());
    }

    #endregion

    #region Deletes

    public async Task DeleteRentalByIdAsync(int rentalId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Lead.Rental_DeleteById", new { RentalId = rentalId });
    }

    #endregion
}
