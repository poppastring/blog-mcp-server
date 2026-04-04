using System.Text.Json;
using BlogMcpServer.Clients;
using BlogMcpServer.Configuration;
using BlogMcpServer.Discovery;
using BlogMcpServer.XmlRpc;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMcpServer.Configuration;

/// <summary>
/// Resolves the appropriate IBlogClient based on configuration and RSD discovery.
/// </summary>
public class BlogClientFactory
{
    private readonly BlogConfiguration _config;
    private readonly HttpClient _httpClient;

    public BlogClientFactory(BlogConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Create an IBlogClient from explicit configuration.
    /// </summary>
    public IBlogClient Create()
    {
        if (string.IsNullOrEmpty(_config.XmlRpcEndpoint))
            throw new InvalidOperationException("XmlRpcEndpoint is not configured. Run discover_blog first or set it manually.");

        var rpc = new XmlRpcClient(_httpClient, _config.XmlRpcEndpoint);
        return CreateForApi(_config.PreferredApi, rpc);
    }

    /// <summary>
    /// Create an IBlogClient from RSD discovery results, updating config.
    /// </summary>
    public IBlogClient CreateFromRsd(RsdInfo rsd)
    {
        var preferred = rsd.Apis.FirstOrDefault(a => a.Preferred) ?? rsd.Apis.FirstOrDefault();
        if (preferred == null)
            throw new InvalidOperationException("No APIs found in RSD document.");

        _config.XmlRpcEndpoint = preferred.ApiLink;
        _config.BlogId = preferred.BlogId;
        if (string.IsNullOrEmpty(_config.PreferredApi))
            _config.PreferredApi = preferred.Name;

        var rpc = new XmlRpcClient(_httpClient, preferred.ApiLink);
        return CreateForApi(_config.PreferredApi, rpc);
    }

    private IBlogClient CreateForApi(string apiName, XmlRpcClient rpc)
    {
        return apiName switch
        {
            "Blogger" => new BloggerClient(rpc, _config),
            "Moveable Type" or "MovableType" or "Movable Type"
                => new MovableTypeClient(rpc, _config, new MetaWeblogClient(rpc, _config)),
            _ => new MetaWeblogClient(rpc, _config), // Default to MetaWeblog
        };
    }
}
