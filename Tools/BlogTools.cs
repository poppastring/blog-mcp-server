using System.ComponentModel;
using System.Text.Json;
using BlogMcpServer.Clients;
using BlogMcpServer.Configuration;
using BlogMcpServer.Discovery;
using ModelContextProtocol.Server;

namespace BlogMcpServer.Tools;

[McpServerToolType]
public class BlogTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [McpServerTool(Name = "discover_blog"), Description("Discover supported blogging APIs via RSD (Really Simple Discovery). Returns engine info and available APIs.")]
    public static async Task<string> DiscoverBlog(
        RsdDiscovery rsdDiscovery,
        BlogClientFactory factory,
        BlogConfiguration config,
        [Description("The blog URL to discover (e.g. https://www.poppastring.com)")] string blogUrl,
        [Description("Profile name for this blog (e.g. poppastring, thedasblog)")] string blogName)
    {
        var rsd = await rsdDiscovery.DiscoverAsync(blogUrl);
        if (rsd == null)
            return "No RSD document found. The blog may not support XML-RPC discovery. You can configure the endpoint manually.";

        // Derive profile name from URL if not provided
        if (string.IsNullOrEmpty(blogName))
            blogName = new Uri(blogUrl).Host.Replace("www.", "").Split('.')[0];

        var profile = config.EnsureProfile(blogName);
        profile.Url = blogUrl;
        factory.CreateFromRsd(rsd, blogName);

        var needsCredentials = string.IsNullOrEmpty(profile.Username) || string.IsNullOrEmpty(profile.Password);

        return JsonSerializer.Serialize(new
        {
            ProfileName = blogName,
            rsd.EngineName,
            rsd.EngineLink,
            rsd.HomePageLink,
            Apis = rsd.Apis.Select(a => new { a.Name, a.Preferred, a.ApiLink, a.BlogId }),
            ConfiguredApi = profile.PreferredApi,
            ConfiguredEndpoint = profile.XmlRpcEndpoint,
            NeedsCredentials = needsCredentials,
            SetupHint = needsCredentials
                ? $"Credentials missing for '{blogName}'. Ask the user for their username and password, then call configure_blog to save them."
                : null,
        }, JsonOptions);
    }

    [McpServerTool(Name = "configure_blog"), Description("Configure blog connection and save credentials. Persists to appsettings.local.json so credentials survive restarts.")]
    public static string ConfigureBlog(
        BlogConfiguration config,
        [Description("Profile name for this blog (e.g. poppastring, thedasblog)")] string blogName,
        [Description("The XML-RPC endpoint URL (e.g. https://www.poppastring.com/feed/blogger)")] string xmlRpcEndpoint,
        [Description("Username for authentication")] string username,
        [Description("Password for authentication")] string password,
        [Description("Blog ID (often the blog URL)")] string blogId = "",
        [Description("API to use: MetaWeblog, Blogger, or Moveable Type")] string api = "MetaWeblog")
    {
        var profile = config.EnsureProfile(blogName);
        profile.XmlRpcEndpoint = xmlRpcEndpoint;
        profile.Username = username;
        profile.Password = password;
        profile.BlogId = blogId;
        profile.PreferredApi = api;

        if (string.IsNullOrEmpty(config.DefaultBlog))
            config.DefaultBlog = blogName;

        var settingsPath = ConfigPersistence.GetLocalSettingsPath();
        ConfigPersistence.Save(config);

        return $"Configured '{blogName}': endpoint={xmlRpcEndpoint}, api={api}\n"
            + $"Credentials saved to: {settingsPath}";
    }

    [McpServerTool(Name = "list_blogs"), Description("List all configured blog profiles. Call this first to check setup status.")]
    public static string ListBlogs(BlogConfiguration config)
    {
        if (config.Blogs.Count == 0)
            return "No blog profiles configured. Ask the user for their blog URL and credentials, "
                + "then call discover_blog with the URL to detect APIs, followed by configure_blog to save credentials.";

        var profiles = config.Blogs.Select(b => new
        {
            Name = b.Key,
            b.Value.Url,
            b.Value.XmlRpcEndpoint,
            b.Value.PreferredApi,
            HasCredentials = !string.IsNullOrEmpty(b.Value.Username) && !string.IsNullOrEmpty(b.Value.Password),
            NeedsDiscovery = string.IsNullOrEmpty(b.Value.XmlRpcEndpoint),
        });

        return JsonSerializer.Serialize(profiles, JsonOptions);
    }

    [McpServerTool(Name = "list_posts"), Description("Get recent blog posts. Returns titles, dates, IDs, and excerpts.")]
    public static async Task<string> ListPosts(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName,
        [Description("Number of recent posts to retrieve (default 10)")] int count = 10)
    {
        var client = factory.Create(blogName);
        var posts = await client.GetRecentPostsAsync(count);

        return JsonSerializer.Serialize(posts.Select(p => new
        {
            p.PostId,
            p.Title,
            p.DateCreated,
            p.Categories,
            p.Permalink,
            Excerpt = Truncate(p.Excerpt.Length > 0 ? p.Excerpt : StripHtml(p.Content), 200),
        }), JsonOptions);
    }

    [McpServerTool(Name = "get_post"), Description("Get a single blog post by its ID. Returns full content.")]
    public static async Task<string> GetPost(
        BlogClientFactory factory,
        [Description("The post ID to retrieve")] string postId,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName)
    {
        var client = factory.Create(blogName);
        var post = await client.GetPostAsync(postId);

        return JsonSerializer.Serialize(post, JsonOptions);
    }

    [McpServerTool(Name = "create_post"), Description("Create a new blog post.")]
    public static async Task<string> CreatePost(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName,
        [Description("Post title")] string title,
        [Description("Post content (HTML)")] string content,
        [Description("Publish immediately (true) or save as draft (false)")] bool publish = false,
        [Description("Comma-separated category names")] string categories = "",
        [Description("Post excerpt/description")] string excerpt = "")
    {
        var client = factory.Create(blogName);
        var post = new Models.BlogPost
        {
            Title = title,
            Content = content,
            Excerpt = excerpt,
            Categories = string.IsNullOrEmpty(categories) ? [] : categories.Split(',', StringSplitOptions.TrimEntries),
            DateCreated = DateTime.UtcNow,
        };

        var postId = await client.CreatePostAsync(post, publish);
        return JsonSerializer.Serialize(new { PostId = postId, Status = publish ? "Published" : "Draft" }, JsonOptions);
    }

    [McpServerTool(Name = "edit_post"), Description("Update an existing blog post.")]
    public static async Task<string> EditPost(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName,
        [Description("The post ID to update")] string postId,
        [Description("New post title")] string title,
        [Description("New post content (HTML)")] string content,
        [Description("Publish the update (true) or keep as draft (false)")] bool publish = true,
        [Description("Comma-separated category names")] string categories = "",
        [Description("Post excerpt/description")] string excerpt = "")
    {
        var client = factory.Create(blogName);
        var post = new Models.BlogPost
        {
            Title = title,
            Content = content,
            Excerpt = excerpt,
            Categories = string.IsNullOrEmpty(categories) ? [] : categories.Split(',', StringSplitOptions.TrimEntries),
        };

        var success = await client.EditPostAsync(postId, post, publish);
        return success ? $"Post {postId} updated successfully." : $"Failed to update post {postId}.";
    }

    [McpServerTool(Name = "delete_post"), Description("Delete a blog post by ID.")]
    public static async Task<string> DeletePost(
        BlogClientFactory factory,
        [Description("The post ID to delete")] string postId,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName)
    {
        var client = factory.Create(blogName);
        var success = await client.DeletePostAsync(postId);
        return success ? $"Post {postId} deleted." : $"Failed to delete post {postId}.";
    }

    [McpServerTool(Name = "get_categories"), Description("List all available blog categories.")]
    public static async Task<string> GetCategories(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName)
    {
        var client = factory.Create(blogName);
        var categories = await client.GetCategoriesAsync();
        return JsonSerializer.Serialize(categories, JsonOptions);
    }

    [McpServerTool(Name = "set_post_categories"), Description("Assign categories to a blog post.")]
    public static async Task<string> SetPostCategories(
        BlogClientFactory factory,
        [Description("The post ID")] string postId,
        [Description("Comma-separated category IDs")] string categoryIds,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName)
    {
        var client = factory.Create(blogName);
        var ids = categoryIds.Split(',', StringSplitOptions.TrimEntries);
        var success = await client.SetPostCategoriesAsync(postId, ids);
        return success ? $"Categories set on post {postId}." : $"Failed to set categories on post {postId}.";
    }

    [McpServerTool(Name = "upload_media"), Description("Upload a media file (image, etc.) to the blog. Provide base64-encoded file data.")]
    public static async Task<string> UploadMedia(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName,
        [Description("File name (e.g. photo.jpg)")] string fileName,
        [Description("MIME type (e.g. image/jpeg)")] string mimeType,
        [Description("Base64-encoded file content")] string base64Data = "",
        [Description("Absolute file path to read from disk (use instead of base64Data for large files)")] string filePath = "")
    {
        var client = factory.Create(blogName);

        byte[] data;
        if (!string.IsNullOrEmpty(filePath))
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";
            data = await File.ReadAllBytesAsync(filePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(mimeType))
                mimeType = GuessContentType(filePath);
        }
        else if (!string.IsNullOrEmpty(base64Data))
        {
            data = Convert.FromBase64String(base64Data);
        }
        else
        {
            return "Provide either filePath or base64Data.";
        }

        var result = await client.UploadMediaAsync(fileName, mimeType, data);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private static string GuessContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }

    [McpServerTool(Name = "get_user_info"), Description("Get information about the authenticated blog user.")]
    public static async Task<string> GetUserInfo(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName)
    {
        var client = factory.Create(blogName);
        var user = await client.GetUserInfoAsync();
        return JsonSerializer.Serialize(user, JsonOptions);
    }

    [McpServerTool(Name = "get_users_blogs"), Description("List blogs accessible to the authenticated user.")]
    public static async Task<string> GetUsersBlogs(
        BlogClientFactory factory,
        [Description("Blog profile name (e.g. poppastring, thedasblog)")] string blogName)
    {
        var client = factory.Create(blogName);
        var blogs = await client.GetUsersBlogsAsync();
        return JsonSerializer.Serialize(blogs, JsonOptions);
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "...";

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;
        // Simple HTML tag removal
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        return System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
    }
}
