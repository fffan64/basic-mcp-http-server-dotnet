using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace StreamableHttpWebApp.Tools
{
  public class WeatherAlertsTool(IHttpClientFactory httpClientFactory)
  {
    [McpServerTool, Description("Hello world from Streamable http tool!")]
    public string GetHelloWorld()
    {
      return "Hello world from Streamable http tool!";
    }

    [McpServerTool, Description("This tool returns alerts from the https://api.weather.gov/ API based on the state code.")]
    public async Task<List<WeatherAlert>> GetAlerts([Description("2 character state code, e.g. CA for California")] string stateCode)
    {
      var client = httpClientFactory.CreateClient("weatherApi");
      using var response = await client.GetStreamAsync($"/alerts?area={stateCode}&limit=10");
      using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the alerts endpoint");
      var alerts = new List<WeatherAlert>();
      foreach (var element in jsonDoc.RootElement.GetProperty("features").EnumerateArray())
      {
        alerts.Add(new WeatherAlert
        {
          Event = element.GetProperty("properties").GetProperty("event").GetString() ?? string.Empty,
          AreaDesc = element.GetProperty("properties").GetProperty("areaDesc").GetString() ?? string.Empty,
          Severity = element.GetProperty("properties").GetProperty("severity").GetString() ?? string.Empty,
          Description = element.GetProperty("properties").GetProperty("description").GetString() ?? string.Empty
        });
      }
      return alerts;
    }
  }
}