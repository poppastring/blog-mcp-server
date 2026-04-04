using BlogMcpServer.Configuration;
using BlogMcpServer.Models;
using BlogMcpServer.XmlRpc;

namespace BlogMcpServer.Clients;

/// <summary>
/// Movable Type API client. Maps to mt.* XML-RPC methods.
/// Movable Type is additive on top of MetaWeblog/Blogger for categories and metadata.
/// </summary>
public class MovableTypeClient : XmlRpcBlogClientBase, IBlogClient
{
    public string ApiName => "Moveable Type";

    // MT doesn't define core post CRUD; delegate to a companion MetaWeblog or Blogger client
    private readonly IBlogClient? _companion;

    public MovableTypeClient(XmlRpcClient rpc, BlogProfile profile, IBlogClient? companion = null)
        : base(rpc, profile)
    {
        _companion = companion;
    }

    // Post operations delegate to companion client
    public Task<List<BlogPost>> GetRecentPostsAsync(int count)
        => EnsureCompanion().GetRecentPostsAsync(count);

    public Task<BlogPost> GetPostAsync(string postId)
        => EnsureCompanion().GetPostAsync(postId);

    public Task<string> CreatePostAsync(BlogPost post, bool publish)
        => EnsureCompanion().CreatePostAsync(post, publish);

    public Task<bool> EditPostAsync(string postId, BlogPost post, bool publish)
        => EnsureCompanion().EditPostAsync(postId, post, publish);

    public Task<bool> DeletePostAsync(string postId)
        => EnsureCompanion().DeletePostAsync(postId);

    // MT-specific: category management
    public async Task<List<BlogCategory>> GetCategoriesAsync()
    {
        var result = await Rpc.InvokeArrayAsync("mt.getCategoryList",
            Str(Profile.BlogId), Str(Profile.Username), Str(Profile.Password));

        return result.Select(v =>
        {
            var s = v.AsStruct();
            return new BlogCategory
            {
                CategoryId = GetString(s, "categoryId"),
                Title = GetString(s, "categoryName"),
            };
        }).ToList();
    }

    public async Task<List<BlogCategory>> GetPostCategoriesAsync(string postId)
    {
        var result = await Rpc.InvokeArrayAsync("mt.getPostCategories",
            Str(postId), Str(Profile.Username), Str(Profile.Password));

        return result.Select(v =>
        {
            var s = v.AsStruct();
            return new BlogCategory
            {
                CategoryId = GetString(s, "categoryId"),
                Title = GetString(s, "categoryName"),
                IsPrimary = s.TryGetValue("isPrimary", out var p) && p.AsBool(),
            };
        }).ToList();
    }

    public async Task<bool> SetPostCategoriesAsync(string postId, string[] categoryIds)
    {
        var cats = categoryIds.Select(id => new XmlRpcValue(
            new Dictionary<string, XmlRpcValue>
            {
                ["categoryId"] = Str(id),
            })).ToList();

        var result = await Rpc.InvokeAsync("mt.setPostCategories",
            Str(postId), Str(Profile.Username), Str(Profile.Password), new XmlRpcValue(cats));

        return result.AsBool();
    }

    // MT-specific: post titles (lightweight list)
    public async Task<List<PostTitle>> GetRecentPostTitlesAsync(int count)
    {
        var result = await Rpc.InvokeArrayAsync("mt.getRecentPostTitles",
            Str(Profile.BlogId), Str(Profile.Username), Str(Profile.Password), Int(count));

        return result.Select(v =>
        {
            var s = v.AsStruct();
            return new PostTitle
            {
                PostId = GetString(s, "postid"),
                Title = GetString(s, "title"),
                DateCreated = GetDateTime(s, "dateCreated"),
                UserId = GetString(s, "userid"),
            };
        }).ToList();
    }

    // MT-specific: supported methods
    public async Task<string[]> GetSupportedMethodsAsync()
    {
        var result = await Rpc.InvokeArrayAsync("mt.supportedMethods");
        return result.Select(v => v.AsString()).ToArray();
    }

    // Delegate media upload to companion
    public Task<MediaUploadResult> UploadMediaAsync(string fileName, string mimeType, byte[] data)
        => EnsureCompanion().UploadMediaAsync(fileName, mimeType, data);

    public Task<BlogUser> GetUserInfoAsync()
        => EnsureCompanion().GetUserInfoAsync();

    public Task<List<BlogInfo>> GetUsersBlogsAsync()
        => EnsureCompanion().GetUsersBlogsAsync();

    private IBlogClient EnsureCompanion()
        => _companion ?? throw new InvalidOperationException(
            "Movable Type API requires a companion MetaWeblog or Blogger client for post operations.");
}
