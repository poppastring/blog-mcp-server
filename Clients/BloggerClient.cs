using BlogMcpServer.Configuration;
using BlogMcpServer.Models;
using BlogMcpServer.XmlRpc;

namespace BlogMcpServer.Clients;

/// <summary>
/// Blogger API client. Maps to blogger.* XML-RPC methods.
/// </summary>
public class BloggerClient : XmlRpcBlogClientBase, IBlogClient
{
    public string ApiName => "Blogger";
    private const string AppKey = ""; // Legacy field, not used by modern implementations

    public BloggerClient(XmlRpcClient rpc, BlogConfiguration config) : base(rpc, config) { }

    public async Task<List<BlogPost>> GetRecentPostsAsync(int count)
    {
        var result = await Rpc.InvokeArrayAsync("blogger.getRecentPosts",
            Str(AppKey), Str(Config.BlogId), Str(Config.Username), Str(Config.Password), Int(count));

        return result.Select(v =>
        {
            var s = v.AsStruct();
            var post = new BlogPost
            {
                PostId = GetString(s, "postid"),
                Author = GetString(s, "userid"),
                DateCreated = GetDateTime(s, "dateCreated"),
            };
            ParseBloggerContent(GetString(s, "content"), post);
            return post;
        }).ToList();
    }

    public async Task<BlogPost> GetPostAsync(string postId)
    {
        var result = await Rpc.InvokeStructAsync("blogger.getPost",
            Str(AppKey), Str(postId), Str(Config.Username), Str(Config.Password));

        var post = new BlogPost
        {
            PostId = GetString(result, "postid"),
            Author = GetString(result, "userid"),
            DateCreated = GetDateTime(result, "dateCreated"),
        };
        ParseBloggerContent(GetString(result, "content"), post);
        return post;
    }

    public async Task<string> CreatePostAsync(BlogPost post, bool publish)
    {
        var content = FormatBloggerContent(post);
        var result = await Rpc.InvokeAsync("blogger.newPost",
            Str(AppKey), Str(Config.BlogId), Str(Config.Username), Str(Config.Password),
            Str(content), Bool(publish));

        return result.AsString();
    }

    public async Task<bool> EditPostAsync(string postId, BlogPost post, bool publish)
    {
        var content = FormatBloggerContent(post);
        var result = await Rpc.InvokeAsync("blogger.editPost",
            Str(AppKey), Str(postId), Str(Config.Username), Str(Config.Password),
            Str(content), Bool(publish));

        return result.AsBool();
    }

    public async Task<bool> DeletePostAsync(string postId)
    {
        var result = await Rpc.InvokeAsync("blogger.deletePost",
            Str(AppKey), Str(postId), Str(Config.Username), Str(Config.Password), Bool(true));

        return result.AsBool();
    }

    public async Task<List<BlogCategory>> GetCategoriesAsync()
    {
        var result = await Rpc.InvokeArrayAsync("blogger.getCategories",
            Str(Config.BlogId), Str(Config.Username), Str(Config.Password));

        return result.Select(v =>
        {
            var s = v.AsStruct();
            return new BlogCategory
            {
                CategoryId = GetString(s, "categoryid"),
                Title = GetString(s, "title"),
                Description = GetString(s, "description"),
                HtmlUrl = GetString(s, "htmlUrl"),
                RssUrl = GetString(s, "rssUrl"),
            };
        }).ToList();
    }

    public Task<List<BlogCategory>> GetPostCategoriesAsync(string postId)
    {
        throw new NotSupportedException("Use Movable Type API for getPostCategories.");
    }

    public Task<bool> SetPostCategoriesAsync(string postId, string[] categoryIds)
    {
        throw new NotSupportedException("Use Movable Type API for setPostCategories.");
    }

    public Task<MediaUploadResult> UploadMediaAsync(string fileName, string mimeType, byte[] data)
    {
        throw new NotSupportedException("Use MetaWeblog API for media upload.");
    }

    public async Task<BlogUser> GetUserInfoAsync()
    {
        var result = await Rpc.InvokeStructAsync("blogger.getUserInfo",
            Str(AppKey), Str(Config.Username), Str(Config.Password));

        return new BlogUser
        {
            UserId = GetString(result, "userid"),
            Nickname = GetString(result, "nickname"),
            Email = GetString(result, "email"),
            Url = GetString(result, "url"),
            FirstName = GetString(result, "firstname"),
            LastName = GetString(result, "lastname"),
        };
    }

    public async Task<List<BlogInfo>> GetUsersBlogsAsync()
    {
        var result = await Rpc.InvokeArrayAsync("blogger.getUsersBlogs",
            Str(AppKey), Str(Config.Username), Str(Config.Password));

        return result.Select(v =>
        {
            var s = v.AsStruct();
            return new BlogInfo
            {
                BlogId = GetString(s, "blogid"),
                BlogName = GetString(s, "blogName"),
                Url = GetString(s, "url"),
            };
        }).ToList();
    }

    // Blogger API uses <title>Title</title>Content format
    private static void ParseBloggerContent(string content, BlogPost post)
    {
        const string titleOpen = "<title>";
        const string titleClose = "</title>";

        if (content.StartsWith(titleOpen, StringComparison.OrdinalIgnoreCase))
        {
            var endIdx = content.IndexOf(titleClose, StringComparison.OrdinalIgnoreCase);
            if (endIdx > 0)
            {
                post.Title = content[titleOpen.Length..endIdx];
                post.Content = content[(endIdx + titleClose.Length)..];
                return;
            }
        }

        post.Title = string.Empty;
        post.Content = content;
    }

    private static string FormatBloggerContent(BlogPost post)
    {
        return $"<title>{post.Title}</title>{post.Content}";
    }
}
