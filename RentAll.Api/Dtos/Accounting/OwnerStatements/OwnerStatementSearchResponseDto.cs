using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementSearchResponseDto
{
    public List<OwnerStatementResponseDto> Summaries { get; set; } = new List<OwnerStatementResponseDto>();
    public List<OwnerStatementPropertyActivityLineResponseDto> PropertyActivityLines { get; set; } = new List<OwnerStatementPropertyActivityLineResponseDto>();

    public OwnerStatementSearchResponseDto(OwnerStatementSearchResult result)
    {
        Summaries = result.Summaries.Select(summary => new OwnerStatementResponseDto(summary)).ToList();
        PropertyActivityLines = result.PropertyActivityLines.Select(line => new OwnerStatementPropertyActivityLineResponseDto(line)).ToList();
    }
}
