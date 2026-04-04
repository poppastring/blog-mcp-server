using BlogMcpServer.Configuration;
using BlogMcpServer.Discovery;
using BlogMcpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Blog configuration (singleton, mutable at runtime via discover/configure tools)
var blogConfig = new BlogConfiguration();
builder.Services.AddSingleton(blogConfig);

// HTTP client for XML-RPC and RSD
builder.Services.AddSingleton<HttpClient>();

// RSD discovery
builder.Services.AddSingleton<RsdDiscovery>();

// Blog client factory
builder.Services.AddSingleton<BlogClientFactory>();

// MCP server with stdio transport
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<BlogTools>();

// Log to stderr so stdout stays clean for MCP protocol
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});

await builder.Build().RunAsync();
