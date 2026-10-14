namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalOwnerBatchItemResultDto
{
    public int Index { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string? ContactCode { get; set; }
    public bool Success { get; set; }
    public bool Updated { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ExternalOwnerBatchResponseDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ExternalOwnerBatchItemResultDto> Results { get; set; } = [];
}
