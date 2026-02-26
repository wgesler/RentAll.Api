using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository : IOrganizationRepository
{
    private readonly string _dbConnectionString;

    public OrganizationRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    private Organization ConvertEntityToModel(OrganizationEntity e)
    {
        return new Organization
        {
            OrganizationId = e.OrganizationId,
            OrganizationCode = e.OrganizationCode,
            Name = e.Name,
            Address1 = e.Address1,
            Address2 = e.Address2,
            Suite = e.Suite,
            City = e.City,
            State = e.State,
            Zip = e.Zip,
            Phone = e.Phone,
            Fax = e.Fax,
            ContactName = e.ContactName,
            ContactEmail = e.ContactEmail,
            Website = e.Website,
            LogoPath = e.LogoPath,
            IsInternational = e.IsInternational,
            CurrentInvoiceNo = e.CurrentInvoiceNo,
            OfficeFee = e.OfficeFee,
            UserFee = e.UserFee,
            Unit50Fee = e.Unit50Fee,
            Unit100Fee = e.Unit100Fee,
            Unit200Fee = e.Unit200Fee,
            Unit500Fee = e.Unit500Fee,
            SendGridName = e.SendGridName,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    #region Offices
    private Office ConvertEntityToModel(OfficeEntity e)
    {
        return new Office
        {
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeCode = e.OfficeCode,
            Name = e.Name,
            Address1 = e.Address1,
            Address2 = e.Address2,
            Suite = e.Suite,
            City = e.City,
            State = e.State,
            Zip = e.Zip,
            Phone = e.Phone,
            Fax = e.Fax,
            Website = e.Website,
            LogoPath = e.LogoPath,
            MaintenanceEmail = e.MaintenanceEmail,
            AfterHoursPhone = e.AfterHoursPhone,
            AfterHoursInstructions = e.AfterHoursInstructions,
            DaysToRefundDeposit = e.DaysToRefundDeposit,
            DefaultDeposit = e.DefaultDeposit,
            DefaultSdw = e.DefaultSdw,
            DefaultKeyFee = e.DefaultKeyFee,
            UndisclosedPetFee = e.UndisclosedPetFee,
            MinimumSmokingFee = e.MinimumSmokingFee,
            UtilityOneBed = e.UtilityOneBed,
            UtilityTwoBed = e.UtilityTwoBed,
            UtilityThreeBed = e.UtilityThreeBed,
            UtilityFourBed = e.UtilityFourBed,
            UtilityHouse = e.UtilityHouse,
            MaidOneBed = e.MaidOneBed,
            MaidTwoBed = e.MaidTwoBed,
            MaidThreeBed = e.MaidThreeBed,
            MaidFourBed = e.MaidFourBed,
            ParkingLowEnd = e.ParkingLowEnd,
            ParkingHighEnd = e.ParkingHighEnd,
            IsInternational = e.IsInternational,
            IsActive = e.IsActive
        };
    }
    #endregion

    #region Accounting
    private AccountingOffice ConvertEntityToModel(AccountingOfficeEntity e)
    {
        return new AccountingOffice
        {
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            Name = e.Name,
            Address1 = e.Address1,
            Address2 = e.Address2,
            Suite = e.Suite,
            City = e.City,
            State = e.State,
            Zip = e.Zip,
            Phone = e.Phone,
            Fax = e.Fax,
            Email = e.Email,
            Website = e.Website,
            BankName = e.BankName,
            BankRouting = e.BankRouting,
            BankAccount = e.BankAccount,
            BankSwiftCode = e.BankSwiftCode,
            BankAddress = e.BankAddress,
            BankPhone = e.BankPhone,
            LogoPath = e.LogoPath,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }
    #endregion

    #region Agents
    private Agent ConvertEntityToModel(AgentEntity e)
    {
        var response = new Agent()
        {
            AgentId = e.AgentId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            AgentCode = e.AgentCode,
            Name = e.Name,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };

        return response;
    }
    #endregion

    #region Areas
    private Area ConvertEntityToModel(AreaEntity e)
    {
        return new Area
        {
            OrganizationId = e.OrganizationId,
            AreaId = e.AreaId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            AreaCode = e.AreaCode,
            Name = e.Name,
            Description = e.Description,
            IsActive = e.IsActive
        };
    }
    #endregion

    #region Buildings
    private Building ConvertEntityToModel(BuildingEntity e)
    {
        return new Building
        {
            OrganizationId = e.OrganizationId,
            BuildingId = e.BuildingId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            BuildingCode = e.BuildingCode,
            Name = e.Name,
            Description = e.Description,
            HoaName = e.HoaName,
            HoaPhone = e.HoaPhone,
            HoaEmail = e.HoaEmail,
            IsActive = e.IsActive
        };
    }
    #endregion

    #region Colors
    private Colour ConvertEntityToModel(ColorEntity e)
    {
        return new Colour
        {
            ColorId = e.ColorId,
            OrganizationId = e.OrganizationId,
            ReservationStatusId = e.ReservationStatusId,
            Color = e.Color
        };
    }
    #endregion

    #region Regions
    private Region ConvertEntityToModel(RegionEntity e)
    {
        return new Region
        {
            OrganizationId = e.OrganizationId,
            RegionId = e.RegionId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            RegionCode = e.RegionCode,
            Name = e.Name,
            Description = e.Description,
            IsActive = e.IsActive
        };
    }
    #endregion
}




