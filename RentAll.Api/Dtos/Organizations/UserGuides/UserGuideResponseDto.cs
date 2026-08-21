namespace RentAll.Api.Dtos.Organizations.UserGuides;

public class UserGuideResponseDto
{
    public Guid UserGuideId { get; set; }
    public Dictionary<string, string> Sections { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public UserGuideResponseDto(UserGuide userGuide)
    {
        UserGuideId = userGuide.UserGuideId;
        Sections = userGuide.Sections ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
