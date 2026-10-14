namespace RentAll.Api.Dtos.Admin;

public class StorageRepairEventDto
{
    public string Kind { get; set; } = "";
    public string File { get; set; } = "";
    public string Status { get; set; } = "";
    public string Detail { get; set; } = "";
    public int Total { get; set; }
    public int Moved { get; set; }
    public int AlreadyCorrect { get; set; }
    public int Removed { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
