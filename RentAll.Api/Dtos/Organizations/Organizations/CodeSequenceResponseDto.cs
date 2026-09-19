using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Organizations;

public class CodeSequenceResponseDto
{
    public int EntityTypeId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int NextNumber { get; set; }

    public CodeSequenceResponseDto()
    {
    }

    public CodeSequenceResponseDto(CodeSequence sequence)
    {
        EntityTypeId = sequence.EntityTypeId;
        EntityType = sequence.EntityType ?? string.Empty;
        Prefix = ((EntityType)sequence.EntityTypeId).ToCode();
        NextNumber = sequence.NextNumber;
    }
}
