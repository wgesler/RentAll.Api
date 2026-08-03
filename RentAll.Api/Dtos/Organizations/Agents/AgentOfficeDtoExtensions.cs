namespace RentAll.Api.Dtos.Organizations.Agents;

public static class AgentOfficeDtoExtensions
{
    public static List<int> ResolveOffices(int? officeId, List<int>? offices)
    {
        var resolvedOffices = (offices ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (officeId.HasValue && officeId.Value > 0 && !resolvedOffices.Contains(officeId.Value))
            resolvedOffices.Add(officeId.Value);

        if (!resolvedOffices.Any() && officeId.HasValue && officeId.Value > 0)
            resolvedOffices.Add(officeId.Value);

        return resolvedOffices;
    }
}
