namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyBatchItemResultDto
{
    public int Index { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool Updated { get; set; }
    public string? ErrorMessage { get; set; }
    public PropertyResponseDto? Property { get; set; }
}

public class ExternalPropertyBatchResponseDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ExternalPropertyBatchItemResultDto> Results { get; set; } = [];
}
