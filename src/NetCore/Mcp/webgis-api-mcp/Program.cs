using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebGIS.API.MCP.Extensions;
using WebGIS.API.MCP.Extensions.DependencyInjection;
using WebGIS.API.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpAuthentication(builder.Configuration)
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<WebGISApiTools>();

//builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.MapDefaultEndpoints();

if(builder.Configuration.UseAuthentication())
{
    app.Logger.LogInformation("Authentication is enabled. Authority: {Authority}, Audience: {Audience}, Scopes: {Scopes}",
        builder.Configuration.GetAuthenticationAuthority(),
        builder.Configuration.GetAuthenticationAudience(),
        String.Join(", ", builder.Configuration.GetAuthenticationScopes()));

    app.UseAuthentication();
    app.UseAuthorization();
}
else
{
    app.Logger.LogWarning("Authentication is disabled. The MCP API will be accessible without authentication.");
}


var mcpRoutes = app.MapMcp();
if (builder.Configuration.UseAuthentication())
{
    mcpRoutes.RequireAuthorization();
}

app.Run();
