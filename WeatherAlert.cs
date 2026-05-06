namespace StreamableHttpWebApp
{
    public class WeatherAlert
    {
        public string Event { get; set; } = default!;
        public string AreaDesc { get; set; } = default!;
        public string Severity { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}