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
        [Description("The blog URL to discover (e.g. https://www.poppastring.com)")] string blogUrl)
    {
        var rsd = await rsdDiscovery.DiscoverAsync(blogUrl);
        if (rsd == null)
            return "No RSD document found. The blog may not support XML-RPC discovery. You can configure the endpoint manually.";

        // Update config with discovery results
        config.BlogUrl = blogUrl;
        factory.CreateFromRsd(rsd);

        return JsonSerializer.Serialize(new
        {
            rsd.EngineName,
            rsd.EngineLink,
            rsd.HomePageLink,
            Apis = rsd.Apis.Select(a => new { a.Name, a.Preferred, a.ApiLink, a.BlogId }),
            ConfiguredApi = config.PreferredApi,
            ConfiguredEndpoint = config.XmlRpcEndpoint,
        }, JsonOptions);
    }

    [McpServerTool(Name = "configure_blog"), Description("Manually configure blog connection when RSD is not available.")]
    public static string ConfigureBlog(
        BlogConfiguration config,
        [Description("The XML-RPC endpoint URL (e.g. https://www.poppastring.com/feed/blogger)")] string xmlRpcEndpoint,
        [Description("Username for authentication")] string username,
        [Description("Password for authentication")] string password,
        [Description("Blog ID (often the blog URL)")] string blogId = "",
        [Description("API to use: MetaWeblog, Blogger, or Moveable Type")] string api = "MetaWeblog")
    {
        config.XmlRpcEndpoint = xmlRpcEndpoint;
        config.Username = username;
        config.Password = password;
        config.BlogId = blogId;
        config.PreferredApi = api;

        return $"Configured: endpoint={xmlRpcEndpoint}, api={api}, blogId={blogId}";
    }

    [McpServerTool(Name = "list_posts"), Description("Get recent blog posts. Returns titles, dates, IDs, and excerpts.")]
    public static async Task<string> ListPosts(
        BlogClientFactory factory,
        [Description("Number of recent posts to retrieve (default 10)")] int count = 10)
    {
        var client = factory.Create();
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
        [Description("The post ID to retrieve")] string postId)
    {
        var client = factory.Create();
        var post = await client.GetPostAsync(postId);

        return JsonSerializer.Serialize(post, JsonOptions);
    }

    [McpServerTool(Name = "create_post"), Description("Create a new blog post.")]
    public static async Task<string> CreatePost(
        BlogClientFactory factory,
        [Description("Post title")] string title,
        [Description("Post content (HTML)")] string content,
        [Description("Publish immediately (true) or save as draft (false)")] bool publish = false,
        [Description("Comma-separated category names")] string categories = "",
        [Description("Post excerpt/description")] string excerpt = "")
    {
        var client = factory.Create();
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
        [Description("The post ID to update")] string postId,
        [Description("New post title")] string title,
        [Description("New post content (HTML)")] string content,
        [Description("Publish the update (true) or keep as draft (false)")] bool publish = true,
        [Description("Comma-separated category names")] string categories = "",
        [Description("Post excerpt/description")] string excerpt = "")
    {
        var client = factory.Create();
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
        [Description("The post ID to delete")] string postId)
    {
        var client = factory.Create();
        var success = await client.DeletePostAsync(postId);
        return success ? $"Post {postId} deleted." : $"Failed to delete post {postId}.";
    }

    [McpServerTool(Name = "get_categories"), Description("List all available blog categories.")]
    public static async Task<string> GetCategories(BlogClientFactory factory)
    {
        var client = factory.Create();
        var categories = await client.GetCategoriesAsync();
        return JsonSerializer.Serialize(categories, JsonOptions);
    }

    [McpServerTool(Name = "set_post_categories"), Description("Assign categories to a blog post.")]
    public static async Task<string> SetPostCategories(
        BlogClientFactory factory,
        [Description("The post ID")] string postId,
        [Description("Comma-separated category IDs")] string categoryIds)
    {
        var client = factory.Create();
        var ids = categoryIds.Split(',', StringSplitOptions.TrimEntries);
        var success = await client.SetPostCategoriesAsync(postId, ids);
        return success ? $"Categories set on post {postId}." : $"Failed to set categories on post {postId}.";
    }

    [McpServerTool(Name = "upload_media"), Description("Upload a media file (image, etc.) to the blog. Provide base64-encoded file data.")]
    public static async Task<string> UploadMedia(
        BlogClientFactory factory,
        [Description("File name (e.g. photo.jpg)")] string fileName,
        [Description("MIME type (e.g. image/jpeg)")] string mimeType,
        [Description("Base64-encoded file content")] string base64Data)
    {
        var client = factory.Create();
        var data = Convert.FromBase64String(base64Data);
        var result = await client.UploadMediaAsync(fileName, mimeType, data);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [McpServerTool(Name = "get_user_info"), Description("Get information about the authenticated blog user.")]
    public static async Task<string> GetUserInfo(BlogClientFactory factory)
    {
        var client = factory.Create();
        var user = await client.GetUserInfoAsync();
        return JsonSerializer.Serialize(user, JsonOptions);
    }

    [McpServerTool(Name = "get_users_blogs"), Description("List blogs accessible to the authenticated user.")]
    public static async Task<string> GetUsersBlogs(BlogClientFactory factory)
    {
        var client = factory.Create();
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
