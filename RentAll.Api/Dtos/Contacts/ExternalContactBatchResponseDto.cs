namespace RentAll.Api.Dtos.Contacts;

public class ExternalContactBatchItemResultDto
{
    public int Index { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? ContactCode { get; set; }
    public bool Success { get; set; }
    public bool Updated { get; set; }
    public string? ErrorMessage { get; set; }
    public ExternalContactResponseDto? Contact { get; set; }
}

public class ExternalContactBatchResponseDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ExternalContactBatchItemResultDto> Results { get; set; } = [];
}
