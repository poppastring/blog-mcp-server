using BlogMcpServer.Models;

namespace BlogMcpServer.Clients;

/// <summary>
/// Common abstraction over MetaWeblog, Blogger, and Movable Type APIs.
/// </summary>
public interface IBlogClient
{
    string ApiName { get; }

    // Post operations
    Task<List<BlogPost>> GetRecentPostsAsync(int count);
    Task<BlogPost> GetPostAsync(string postId);
    Task<string> CreatePostAsync(BlogPost post, bool publish);
    Task<bool> EditPostAsync(string postId, BlogPost post, bool publish);
    Task<bool> DeletePostAsync(string postId);

    // Category operations
    Task<List<BlogCategory>> GetCategoriesAsync();
    Task<List<BlogCategory>> GetPostCategoriesAsync(string postId);
    Task<bool> SetPostCategoriesAsync(string postId, string[] categoryIds);

    // Media
    Task<MediaUploadResult> UploadMediaAsync(string fileName, string mimeType, byte[] data);

    // User/blog info
    Task<BlogUser> GetUserInfoAsync();
    Task<List<BlogInfo>> GetUsersBlogsAsync();
}
