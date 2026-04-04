using BlogMcpServer.Configuration;
using BlogMcpServer.Discovery;
using BlogMcpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Quick test mode: dotnet run -- --test
if (args.Length > 0 && args[0] == "--test")
{
    var httpClient = new HttpClient();
    var rsdDiscovery = new RsdDiscovery(httpClient);
    var config = new BlogConfiguration();
    var factory = new BlogClientFactory(config, httpClient);

    Console.WriteLine("=== RSD Discovery ===");
    var result = await BlogTools.DiscoverBlog(rsdDiscovery, factory, config, "https://thedasblog.com");
    Console.WriteLine(result);

    Console.WriteLine();
    Console.WriteLine("=== List Posts (no credentials, expect auth error) ===");
    try
    {
        var posts = await BlogTools.ListPosts(factory, 5);
        Console.WriteLine(posts);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Expected error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("=== Get Categories (no credentials, expect auth error) ===");
    try
    {
        var cats = await BlogTools.GetCategories(factory);
        Console.WriteLine(cats);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Expected error: {ex.Message}");
    }

    return;
}

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
