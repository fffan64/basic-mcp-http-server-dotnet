using System.ComponentModel;

namespace StreamableHttpWebApp.Tools;

public class NasaApod
{
    [Description("Title of the APOD")]
    public string? Title { get; set; }

    [Description("Date of the APOD")]
    public string? Date { get; set; }

    [Description("URL of the image")]
    public string? Url { get; set; }

    [Description("URL of the high-definition image (if available)")]
    public string? HdUrl { get; set; }

    [Description("Type of media (image, video)")]
    public string? MediaType { get; set; }

    [Description("Explanation of the APOD")]
    public string? Explanation { get; set; }

    [Description("Copyright information (if available)")]
    public string? Copyright { get; set; }

    [Description("Video thumbnail URL (if available)")]
    public string? ThumbnailUrl { get; set; }
}