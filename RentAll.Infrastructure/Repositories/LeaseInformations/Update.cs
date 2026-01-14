using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LeaseInformations
{
	public partial class LeaseInformationRepository : ILeaseInformationRepository
	{
		public async Task<LeaseInformation> UpdateByIdAsync(LeaseInformation leaseInformation)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<LeaseInformationEntity>("dbo.LeaseInformation_UpdateById", new
			{
				PropertyId = leaseInformation.PropertyId,
				OrganizationId = leaseInformation.OrganizationId,
				ContactId = leaseInformation.ContactId,
				RentalPayment = leaseInformation.RentalPayment,
				SecurityDeposit = leaseInformation.SecurityDeposit,
				CancellationPolicy = leaseInformation.CancellationPolicy,
				KeyPickUpDropOff = leaseInformation.KeyPickUpDropOff,
				PartialMonth = leaseInformation.PartialMonth,
				DepartureNotification = leaseInformation.DepartureNotification,
				Holdover = leaseInformation.Holdover,
				DepartureServiceFee = leaseInformation.DepartureServiceFee,
				CheckoutProcedure = leaseInformation.CheckoutProcedure,
				Parking = leaseInformation.Parking,
				RulesAndRegulations = leaseInformation.RulesAndRegulations,
				OccupyingTenants = leaseInformation.OccupyingTenants,
				UtilityAllowance = leaseInformation.UtilityAllowance,
				MaidService = leaseInformation.MaidService,
				Pets = leaseInformation.Pets,
				Smoking = leaseInformation.Smoking,
				Emergencies = leaseInformation.Emergencies,
				HomeownersAssociation = leaseInformation.HomeownersAssociation,
				Indemnification = leaseInformation.Indemnification,
				DefaultClause = leaseInformation.DefaultClause,
				AttorneyCollectionFees = leaseInformation.AttorneyCollectionFees,
				ReservedRights = leaseInformation.ReservedRights,
				PropertyUse = leaseInformation.PropertyUse,
				Miscellaneous = leaseInformation.Miscellaneous,
				ModifiedBy = leaseInformation.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("LeaseInformation not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}

