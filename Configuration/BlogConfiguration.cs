namespace BlogMcpServer.Configuration;

public class BlogProfile
{
    public string Url { get; set; } = string.Empty;
    public string XmlRpcEndpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BlogId { get; set; } = string.Empty;
    public string PreferredApi { get; set; } = string.Empty;
}

public class BlogConfiguration
{
    public Dictionary<string, BlogProfile> Blogs { get; set; } = new();
    public string DefaultBlog { get; set; } = string.Empty;

    public BlogProfile GetProfile(string? blogName = null)
    {
        var name = string.IsNullOrEmpty(blogName) ? DefaultBlog : blogName;

        if (!string.IsNullOrEmpty(name) && Blogs.TryGetValue(name, out var profile))
            return profile;

        // Return first profile if no name specified
        if (Blogs.Count > 0)
            return Blogs.Values.First();

        throw new InvalidOperationException(
            "No blog profiles configured. Use configure_blog or set up profiles in user-secrets.");
    }

    public string GetProfileName(string? blogName = null)
    {
        if (!string.IsNullOrEmpty(blogName) && Blogs.ContainsKey(blogName))
            return blogName;

        if (!string.IsNullOrEmpty(DefaultBlog) && Blogs.ContainsKey(DefaultBlog))
            return DefaultBlog;

        return Blogs.Keys.FirstOrDefault() ?? "default";
    }

    public BlogProfile EnsureProfile(string blogName)
    {
        if (!Blogs.TryGetValue(blogName, out var profile))
        {
            profile = new BlogProfile();
            Blogs[blogName] = profile;
        }
        return profile;
    }
}
