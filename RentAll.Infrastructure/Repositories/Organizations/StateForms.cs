using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<StateForm>> GetStateFormsAsync(string organizationId, string stateCode)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<StateFormEntity>("Organization.StateForm_GetAllForms", new
        {
            OrganizationId = organizationId,
            StateCode = stateCode
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<StateForm>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<StateForm?> GetStateFormByIdAsync(int stateFormId, string organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<StateFormEntity>("Organization.StateForm_GetById", new
        {
            StateFormId = stateFormId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<StateForm> CreateStateFormAsync(StateForm stateForm)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<StateFormEntity>("Organization.StateForm_Add", new
        {
            OrganizationId = stateForm.OrganizationId,
            StateCode = stateForm.StateCode,
            FormName = stateForm.FormName,
            Path = stateForm.Path,
            FormAsHtml = stateForm.FormAsHtml
        });

        if (res == null || !res.Any())
            throw new Exception("State form not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<StateForm> UpdateStateFormByIdAsync(StateForm stateForm)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<StateFormEntity>("Organization.StateForm_UpdateById", new
        {
            StateFormId = stateForm.StateFormId,
            OrganizationId = stateForm.OrganizationId,
            StateCode = stateForm.StateCode,
            FormName = stateForm.FormName,
            Path = stateForm.Path,
            FormAsHtml = stateForm.FormAsHtml
        });

        if (res == null || !res.Any())
            throw new Exception("State form not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteStateFormByIdAsync(int stateFormId, string organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.StateForm_DeleteById", new
        {
            StateFormId = stateFormId,
            OrganizationId = organizationId
        });
    }
    #endregion
}
