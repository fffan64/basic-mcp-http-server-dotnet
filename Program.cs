using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StreamableHttpWebApp.Services;
using StreamableHttpWebApp.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

// Add authentication with Auth0
// builder.Services
//     .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         var auth0Authority = builder.Configuration["Auth0:Authority"];
//         var auth0Audience = builder.Configuration["Auth0:Audience"];

//         options.Authority = auth0Authority;
//         options.Audience = auth0Audience;
//         options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//         {
//             NameClaimType = "name",
//             RoleClaimType = "roles",
//             ValidateAudience = true
//         };

//         // Add event handlers for debugging
//         options.Events = new JwtBearerEvents
//         {
//             OnAuthenticationFailed = context =>
//             {
//                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//                 logger.LogError("Authentication failed: {Exception}", context.Exception.Message);
//                 return Task.CompletedTask;
//             },
//             OnTokenValidated = context =>
//             {
//                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
//                 logger.LogInformation("Token validated successfully for user: {UserId}", context.Principal?.FindFirst("sub")?.Value);
//                 return Task.CompletedTask;
//             }
//         };
//     });

// builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogger>();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();
// builder.Services.AddMcpServer().WithHttpTransport().WithTools<WeatherAlertsTool>();
builder.Services.AddHttpClient("weatherApi", client =>
{
    client.BaseAddress = new Uri("https://api.weather.gov/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
});
builder.Services.AddHttpClient("chuckNorrisApi", client =>
{
    client.BaseAddress = new Uri("https://api.chucknorris.io/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("chuck-norris-tool", "1.0"));
});
builder.Services.AddHttpClient("CatAsAServiceApi", client =>
{
    client.BaseAddress = new Uri("https://cataas.com/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("cataas-tool", "1.0"));
});
builder.Services.AddHttpClient("disneyApi", client =>
{
    client.BaseAddress = new Uri("https://api.disneyapi.dev/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("disney-tool", "1.0"));
});
builder.Services.AddHttpClient("zeldaApi", client =>
{
    client.BaseAddress = new Uri("https://zelda.fanapis.com/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("zelda-tool", "1.0"));
});
builder.Services.AddHttpClient("nasaApodApi", client =>
{
    client.BaseAddress = new Uri("https://api.nasa.gov/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("nasa-apod-tool", "1.0"));
});

// Add CORS to allow requests from localhost:5173 and Auth0
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "https://fffan64.auth0.com"
        )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowLocalhost");
// app.UseAuthentication();
// app.UseAuthorization();

app.MapMcp();
app.Run("http://localhost:5002");




// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
