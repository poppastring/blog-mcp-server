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

    public BlogProfile GetProfile(string blogName)
    {
        if (Blogs.TryGetValue(blogName, out var profile))
            return profile;

        throw new InvalidOperationException(
            $"Blog profile '{blogName}' not found. Configured profiles: {string.Join(", ", Blogs.Keys)}");
    }

    public string GetProfileName(string blogName)
    {
        if (Blogs.ContainsKey(blogName))
            return blogName;

        throw new InvalidOperationException(
            $"Blog profile '{blogName}' not found. Configured profiles: {string.Join(", ", Blogs.Keys)}");
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
