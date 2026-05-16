# Streamable HTTP Web App

A minimal MCP (Model Context Protocol) server that exposes a set of tools via HTTP. It demonstrates how to build an authenticated ASP.NET Core service that can be called by an MCPC client such as the Claude Code browser extension.

## Features
- **MCP Server** – Uses `ModelContextProtocol.Server` to expose tools automatically.
- **External APIs** – Weather alerts, Chuck Norris jokes, Disney characters, Zelda games, NASA APOD, and Cat‑as‑a‑Service.
- **JWT Bearer Authentication** – Configured for Auth0 (authority & audience in `appsettings.json`). Tools check the token before executing; requests without a valid token are rejected.
- **CORS** – Allows calls from `http://localhost:5173` (typical React dev server) and from your Auth0 domain.
- **Logging** – Audit logger records tool usage and authentication attempts.

## Getting Started
```bash
# Restore packages
dotnet restore

# Run the application
dotnet run --project StreamableHttpWebApp/StreamableHttpWebApp.csproj
```
The server listens on `http://localhost:5002` (configurable in `Program.cs`). You can test it with a tool like `curl`:
```bash
curl -H "Authorization: Bearer <YOUR_JWT>" \
     http://localhost:5002/mcp \
     -d '{"prompt":"Get a random Chuck Norris joke"}'
```
## Tool list
| Tool | Description |
|------|-------------|
| `GetHelloWorld` | Simple hello world response. |
| `GetAlerts(stateCode)` | Weather alerts from the National Weather Service. |
| `GetChuckNorrisJoke()` | Random Chuck Norris joke with icon URL. |
| `GetCharacters(name?)` | Disney character data, optionally filtered by name. |
| `GetRandomCatImage()` | Random cat image from Cataas API. |
| `GetZeldaGames()` | Zelda game information (placeholder). |
| `GetNasaApod()` | NASA Astronomy Picture of the Day. (API key loaded from appsettings.json under the key `NasaApod:ApiKey`). |
(For full signatures see the classes in `Tools/*.cs`.)
## Configuration
- **Auth0** – Set the Authority and Audience values in `appsettings.json`. The middleware is currently commented out; enable it by uncommenting lines 13–47 in `Program.cs`.
- **HTTP Clients** – Base URLs are configured for each external service.
## Extending
Add a new tool by creating a class marked with `[McpServerToolType]` and adding one or more `[McpServerTool]` methods. The server will automatically discover it when you call `builder.Services.AddMcpServer().WithToolsFromAssembly();`.
## License
MIT