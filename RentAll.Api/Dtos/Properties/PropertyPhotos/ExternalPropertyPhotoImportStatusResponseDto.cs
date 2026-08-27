namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotoImportStatusResponseDto
{
    public Guid ImportId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public List<ExternalPropertyPhotoImportItemStatusDto> Items { get; set; } = [];
}

public class ExternalPropertyPhotoImportItemStatusDto
{
    public int Index { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? PhotoId { get; set; }
    public string? ErrorMessage { get; set; }
}
