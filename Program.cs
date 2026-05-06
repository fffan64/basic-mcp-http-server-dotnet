using System.Net.Http.Headers;
using StreamableHttpWebApp.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

builder.Services.AddMcpServer().WithHttpTransport(options => options.Stateless = true).WithTools<WeatherAlertsTool>();
// builder.Services.AddMcpServer().WithHttpTransport().WithTools<WeatherAlertsTool>();
builder.Services.AddHttpClient("weatherApi", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
});

// Add CORS to allow requests from localhost:5173
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowLocalhost");
app.MapMcp();
app.Run();




// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
