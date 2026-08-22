using System.Text.RegularExpressions;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;

namespace RentAll.Api.Services;

public partial class UserGuideHtmlImageService
{
    private static readonly Guid SystemUserGuideOrganizationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public const string ImagePathAttribute = "data-rentall-guide-path";

    private readonly IFileAttachmentHelper _fileAttachmentHelper;

    public UserGuideHtmlImageService(IFileAttachmentHelper fileAttachmentHelper)
    {
        _fileAttachmentHelper = fileAttachmentHelper;
    }

    public async Task<UserGuide> HydrateForResponseAsync(UserGuide userGuide)
    {
        if (userGuide.Sections == null || userGuide.Sections.Count == 0)
            return userGuide;

        var hydratedSections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (topicKey, html) in userGuide.Sections)
        {
            hydratedSections[topicKey] = await HydrateHtmlAsync(html).ConfigureAwait(false);
        }

        return new UserGuide
        {
            UserGuideId = userGuide.UserGuideId,
            Sections = hydratedSections
        };
    }

    public async Task<string> HydrateHtmlAsync(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html ?? string.Empty;

        var matches = ImgTagRegex().Matches(html).Cast<Match>().ToList();
        if (matches.Count == 0)
            return html;

        var result = html;
        foreach (var match in matches.OrderByDescending(m => m.Index))
        {
            var attrs = match.Groups["attrs"].Value;
            var storagePath = ExtractStoragePath(attrs);
            if (string.IsNullOrWhiteSpace(storagePath))
                continue;

            var fileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                SystemUserGuideOrganizationId,
                null,
                storagePath,
                ImageType.UserGuide).ConfigureAwait(false);

            if (fileDetails == null || string.IsNullOrWhiteSpace(fileDetails.DataUrl))
                continue;

            var hydratedAttrs = ReplaceSrcAttribute(attrs, fileDetails.DataUrl);
            if (!hydratedAttrs.Contains(ImagePathAttribute, StringComparison.OrdinalIgnoreCase))
            {
                hydratedAttrs = $"{hydratedAttrs.TrimEnd()} {ImagePathAttribute}=\"{EscapeAttributeValue(storagePath)}\"";
            }

            var replacement = $"<img {hydratedAttrs.Trim()}>";
            result = result[..match.Index] + replacement + result[(match.Index + match.Length)..];
        }

        return result;
    }

    private static string? ExtractStoragePath(string attrs)
    {
        var explicitPathMatch = ExplicitPathAttributeRegex().Match(attrs);
        if (explicitPathMatch.Success)
            return explicitPathMatch.Groups["path"].Value;

        var srcMatch = SrcAttributeRegex().Match(attrs);
        if (!srcMatch.Success)
            return null;

        var src = srcMatch.Groups["src"].Value;
        if (string.IsNullOrWhiteSpace(src)
            || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || src.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return src.Contains("/userguide/", StringComparison.OrdinalIgnoreCase) ? src : null;
    }

    private static string ReplaceSrcAttribute(string attrs, string dataUrl)
    {
        if (SrcAttributeRegex().IsMatch(attrs))
        {
            return SrcAttributeRegex().Replace(attrs, $"src=\"{EscapeAttributeValue(dataUrl)}\"");
        }

        return $"src=\"{EscapeAttributeValue(dataUrl)}\" {attrs}";
    }

    private static string EscapeAttributeValue(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"<img\b(?<attrs>[^>]*?)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImgTagRegex();

    [GeneratedRegex(@"\bsrc=""(?<src>[^""]*)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SrcAttributeRegex();

    [GeneratedRegex($@"\b{ImagePathAttribute}=""(?<path>[^""]*)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitPathAttributeRegex();
}
