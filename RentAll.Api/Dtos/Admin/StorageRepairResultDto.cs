namespace RentAll.Api.Dtos.Admin;

public class StorageRepairResultDto
{
    public int Moved { get; set; }
    public int AlreadyCorrect { get; set; }
    public int Removed { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
