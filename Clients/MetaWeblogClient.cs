using BlogMcpServer.Configuration;
using BlogMcpServer.Models;
using BlogMcpServer.XmlRpc;

namespace BlogMcpServer.Clients;

/// <summary>
/// MetaWeblog API client. Maps to metaweblog.* XML-RPC methods.
/// </summary>
public class MetaWeblogClient : XmlRpcBlogClientBase, IBlogClient
{
    public string ApiName => "MetaWeblog";

    public MetaWeblogClient(XmlRpcClient rpc, BlogConfiguration config) : base(rpc, config) { }

    public async Task<List<BlogPost>> GetRecentPostsAsync(int count)
    {
        var result = await Rpc.InvokeArrayAsync("metaWeblog.getRecentPosts",
            Str(Config.BlogId), Str(Config.Username), Str(Config.Password), Int(count));

        return result.Select(v => MapToPost(v.AsStruct())).ToList();
    }

    public async Task<BlogPost> GetPostAsync(string postId)
    {
        var result = await Rpc.InvokeStructAsync("metaWeblog.getPost",
            Str(postId), Str(Config.Username), Str(Config.Password));

        return MapToPost(result);
    }

    public async Task<string> CreatePostAsync(BlogPost post, bool publish)
    {
        var postStruct = Struct(MapFromPost(post));
        var result = await Rpc.InvokeAsync("metaWeblog.newPost",
            Str(Config.BlogId), Str(Config.Username), Str(Config.Password), postStruct, Bool(publish));

        return result.AsString();
    }

    public async Task<bool> EditPostAsync(string postId, BlogPost post, bool publish)
    {
        var postStruct = Struct(MapFromPost(post));
        var result = await Rpc.InvokeAsync("metaWeblog.editPost",
            Str(postId), Str(Config.Username), Str(Config.Password), postStruct, Bool(publish));

        return result.AsBool();
    }

    public Task<bool> DeletePostAsync(string postId)
    {
        // MetaWeblog doesn't define delete; fall through to Blogger API
        throw new NotSupportedException("MetaWeblog API does not support deletePost. Use Blogger API.");
    }

    public async Task<List<BlogCategory>> GetCategoriesAsync()
    {
        var result = await Rpc.InvokeArrayAsync("metaWeblog.getCategories",
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

    public async Task<MediaUploadResult> UploadMediaAsync(string fileName, string mimeType, byte[] data)
    {
        var mediaStruct = Struct(new Dictionary<string, XmlRpcValue>
        {
            ["name"] = Str(fileName),
            ["type"] = Str(mimeType),
            ["bits"] = Base64(data),
        });

        var result = await Rpc.InvokeStructAsync("metaWeblog.newMediaObject",
            Str(Config.BlogId), Str(Config.Username), Str(Config.Password), mediaStruct);

        return new MediaUploadResult { Url = GetString(result, "url") };
    }

    public Task<BlogUser> GetUserInfoAsync()
    {
        throw new NotSupportedException("Use Blogger API for getUserInfo.");
    }

    public Task<List<BlogInfo>> GetUsersBlogsAsync()
    {
        throw new NotSupportedException("Use Blogger API for getUsersBlogs.");
    }
}
