using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.LeaseInformations
{
	public partial class LeaseInformationRepository : ILeaseInformationRepository
	{
		private readonly string _dbConnectionString;

		public LeaseInformationRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private LeaseInformation ConvertEntityToModel(LeaseInformationEntity e)
		{
			var response = new LeaseInformation()
			{
				LeaseInformationId = e.LeaseInformationId,
				PropertyId = e.PropertyId,
				OrganizationId = e.OrganizationId,
				ContactId = e.ContactId,
				RentalPayment = e.RentalPayment,
				SecurityDeposit = e.SecurityDeposit,
				SecurityDepositWaiver = e.SecurityDepositWaiver,
				CancellationPolicy = e.CancellationPolicy,
				KeyPickUpDropOff = e.KeyPickUpDropOff,
				PartialMonth = e.PartialMonth,
				DepartureNotification = e.DepartureNotification,
				Holdover = e.Holdover,
				DepartureServiceFee = e.DepartureServiceFee,
				CheckoutProcedure = e.CheckoutProcedure,
				Parking = e.Parking,
				RulesAndRegulations = e.RulesAndRegulations,
				OccupyingTenants = e.OccupyingTenants,
				UtilityAllowance = e.UtilityAllowance,
				MaidService = e.MaidService,
				Pets = e.Pets,
				Smoking = e.Smoking,
				Emergencies = e.Emergencies,
				HomeownersAssociation = e.HomeownersAssociation,
				Indemnification = e.Indemnification,
				DefaultClause = e.DefaultClause,
				AttorneyCollectionFees = e.AttorneyCollectionFees,
				ReservedRights = e.ReservedRights,
				PropertyUse = e.PropertyUse,
				Miscellaneous = e.Miscellaneous,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}

