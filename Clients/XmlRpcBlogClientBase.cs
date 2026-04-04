using BlogMcpServer.Configuration;
using BlogMcpServer.Models;
using BlogMcpServer.XmlRpc;

namespace BlogMcpServer.Clients;

/// <summary>
/// XML-RPC client base class with shared helpers for credential passing.
/// </summary>
public abstract class XmlRpcBlogClientBase
{
    protected readonly XmlRpcClient Rpc;
    protected readonly BlogConfiguration Config;

    protected XmlRpcBlogClientBase(XmlRpcClient rpc, BlogConfiguration config)
    {
        Rpc = rpc;
        Config = config;
    }

    protected XmlRpcValue Str(string s) => new(s);
    protected XmlRpcValue Int(int i) => new(i);
    protected XmlRpcValue Bool(bool b) => new(b);
    protected XmlRpcValue Date(DateTime d) => new(d);
    protected XmlRpcValue Base64(byte[] b) => new(b);

    protected XmlRpcValue Struct(Dictionary<string, XmlRpcValue> members) => new(members);

    protected static string GetString(Dictionary<string, XmlRpcValue> s, string key)
        => s.TryGetValue(key, out var v) ? v.AsString() : string.Empty;

    protected static int GetInt(Dictionary<string, XmlRpcValue> s, string key)
        => s.TryGetValue(key, out var v) ? v.AsInt() : 0;

    protected static DateTime GetDateTime(Dictionary<string, XmlRpcValue> s, string key)
        => s.TryGetValue(key, out var v) ? v.AsDateTime() : DateTime.MinValue;

    protected static string[] GetStringArray(Dictionary<string, XmlRpcValue> s, string key)
    {
        if (!s.TryGetValue(key, out var v)) return [];
        try
        {
            return v.AsArray().Select(x => x.AsString()).ToArray();
        }
        catch
        {
            return [];
        }
    }

    protected BlogPost MapToPost(Dictionary<string, XmlRpcValue> s)
    {
        return new BlogPost
        {
            PostId = GetString(s, "postid"),
            Title = GetString(s, "title"),
            Content = GetString(s, "description"),
            Excerpt = GetString(s, "mt_excerpt"),
            Author = GetString(s, "userid"),
            DateCreated = GetDateTime(s, "dateCreated"),
            Categories = GetStringArray(s, "categories"),
            Link = GetString(s, "link"),
            Permalink = GetString(s, "permaLink"),
            AllowComments = GetString(s, "mt_allow_comments") != "0",
        };
    }

    protected Dictionary<string, XmlRpcValue> MapFromPost(BlogPost post)
    {
        var members = new Dictionary<string, XmlRpcValue>
        {
            ["title"] = Str(post.Title),
            ["description"] = Str(post.Content),
            ["dateCreated"] = Date(post.DateCreated == default ? DateTime.UtcNow : post.DateCreated),
        };

        if (!string.IsNullOrEmpty(post.Excerpt))
            members["mt_excerpt"] = Str(post.Excerpt);

        if (post.Categories.Length > 0)
            members["categories"] = new XmlRpcValue(post.Categories.Select(c => new XmlRpcValue(c)).ToList());

        members["mt_allow_comments"] = Str(post.AllowComments ? "1" : "0");

        return members;
    }
}
