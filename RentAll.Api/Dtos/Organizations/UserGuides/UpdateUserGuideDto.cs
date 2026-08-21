namespace RentAll.Api.Dtos.Organizations.UserGuides;

public class UpdateUserGuideDto
{
    public Guid UserGuideId { get; set; }
    public Dictionary<string, string> Sections { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        return (true, null);
    }

    public UserGuide ToModel()
    {
        return new UserGuide
        {
            UserGuideId = UserGuideId,
            Sections = Sections ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
