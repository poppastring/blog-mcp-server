namespace BlogMcpServer.Configuration;

public class BlogConfiguration
{
    public string BlogUrl { get; set; } = string.Empty;
    public string XmlRpcEndpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BlogId { get; set; } = string.Empty;
    public string PreferredApi { get; set; } = string.Empty; // MetaWeblog, Blogger, or Moveable Type
}
