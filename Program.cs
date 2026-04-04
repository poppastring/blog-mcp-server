using BlogMcpServer.Configuration;
using BlogMcpServer.Discovery;
using BlogMcpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Test modes: dotnet run -- --test <blogUrl>
//             dotnet run -- --post <blogUrl> <username> <password>
if (args.Length > 0 && args[0] is "--test" or "--post")
{
    var httpClient = new HttpClient();
    var rsdDiscovery = new RsdDiscovery(httpClient);
    var blogUrl = args.Length > 1 ? args[1] : "https://thedasblog.com";
    var config = new BlogConfiguration();
    var factory = new BlogClientFactory(config, httpClient);

    Console.WriteLine("=== RSD Discovery ===");
    var discoverResult = await BlogTools.DiscoverBlog(rsdDiscovery, factory, config, blogUrl);
    Console.WriteLine(discoverResult);

    if (args[0] == "--post" && args.Length >= 4)
    {
        config.Username = args[2];
        config.Password = args[3];

        Console.WriteLine();
        Console.WriteLine("=== Creating Post ===");
        var postResult = await BlogTools.CreatePost(factory,
            title: "Testing the Blog MCP Server",
            content: "<p>This post was created by the Blog MCP Server, an MCP server that supports MetaWeblog, Blogger, and Movable Type APIs.</p>"
                + "<p>The server auto-discovered this blog's capabilities via RSD (Really Simple Discovery) and posted using the MetaWeblog API.</p>",
            publish: true,
            categories: "dasblog-core");
        Console.WriteLine(postResult);

        Console.WriteLine();
        Console.WriteLine("=== Listing Recent Posts ===");
        var posts = await BlogTools.ListPosts(factory, 3);
        Console.WriteLine(posts);
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
