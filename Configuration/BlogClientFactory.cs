using BlogMcpServer.Clients;
using BlogMcpServer.Discovery;
using BlogMcpServer.XmlRpc;

namespace BlogMcpServer.Configuration;

/// <summary>
/// Resolves the appropriate IBlogClient for a given blog profile.
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
    /// Create an IBlogClient for the named blog profile.
    /// </summary>
    public IBlogClient Create(string blogName)
    {
        var profile = _config.GetProfile(blogName);

        if (string.IsNullOrEmpty(profile.XmlRpcEndpoint))
            throw new InvalidOperationException(
                $"XmlRpcEndpoint not set for '{blogName}'. Run discover_blog first or configure manually.");

        var rpc = new XmlRpcClient(_httpClient, profile.XmlRpcEndpoint);
        return CreateForApi(profile.PreferredApi, rpc, profile);
    }

    /// <summary>
    /// Create an IBlogClient from RSD discovery results, updating the named profile.
    /// </summary>
    public IBlogClient CreateFromRsd(RsdInfo rsd, string blogName)
    {
        var profile = _config.EnsureProfile(blogName);

        var preferred = rsd.Apis.FirstOrDefault(a => a.Preferred) ?? rsd.Apis.FirstOrDefault();
        if (preferred == null)
            throw new InvalidOperationException("No APIs found in RSD document.");

        profile.XmlRpcEndpoint = preferred.ApiLink;
        profile.BlogId = preferred.BlogId;
        if (string.IsNullOrEmpty(profile.PreferredApi))
            profile.PreferredApi = preferred.Name;

        if (string.IsNullOrEmpty(_config.DefaultBlog))
            _config.DefaultBlog = blogName;

        var rpc = new XmlRpcClient(_httpClient, preferred.ApiLink);
        return CreateForApi(profile.PreferredApi, rpc, profile);
    }

    private static IBlogClient CreateForApi(string apiName, XmlRpcClient rpc, BlogProfile profile)
    {
        return apiName switch
        {
            "Blogger" => new BloggerClient(rpc, profile),
            "Moveable Type" or "MovableType" or "Movable Type"
                => new MovableTypeClient(rpc, profile, new MetaWeblogClient(rpc, profile)),
            _ => new MetaWeblogClient(rpc, profile),
        };
    }
}
