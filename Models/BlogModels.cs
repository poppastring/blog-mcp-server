namespace BlogMcpServer.Models;

public class BlogPost
{
    public string PostId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public string[] Categories { get; set; } = [];
    public string Link { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool AllowComments { get; set; } = true;
}

public class BlogCategory
{
    public string CategoryId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string RssUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class BlogUser
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class BlogInfo
{
    public string BlogId { get; set; } = string.Empty;
    public string BlogName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class MediaUploadResult
{
    public string Url { get; set; } = string.Empty;
}

public class PostTitle
{
    public string PostId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public string UserId { get; set; } = string.Empty;
}
