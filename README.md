# Blog MCP Server

An MCP (Model Context Protocol) server that lets AI clients interact with blog engines supporting classic XML-RPC blogging APIs.

## Supported APIs

- **MetaWeblog** — post CRUD, categories, media upload
- **Blogger** — post CRUD, user info, blog listing
- **Movable Type** — category management, post titles, extends MetaWeblog/Blogger

## Auto-Discovery

Point the `discover_blog` tool at any blog URL and it will fetch the [RSD](http://archipelago.phrasewise.com/rsd) document to detect which APIs are available and where the XML-RPC endpoint lives.

## MCP Tools

| Tool | Description |
|------|-------------|
| `discover_blog` | Auto-detect APIs via RSD |
| `configure_blog` | Manual endpoint configuration |
| `list_posts` | Get recent posts |
| `get_post` | Get a single post by ID |
| `create_post` | Create a new post |
| `edit_post` | Update an existing post |
| `delete_post` | Delete a post |
| `get_categories` | List categories |
| `set_post_categories` | Assign categories to a post |
| `upload_media` | Upload images/files |
| `get_user_info` | Get authenticated user info |
| `get_users_blogs` | List user's blogs |

## Usage

### Build

```bash
dotnet build
```

### Configure in Copilot CLI

Add to your MCP configuration:

```json
{
  "mcpServers": {
    "blog": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\dev\\tools\\blog-mcp-server"]
    }
  }
}
```

Then use the `discover_blog` tool to auto-detect your blog's APIs, or `configure_blog` to set the endpoint manually.

## Built With

- .NET 10
- [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- Stdio transport (works with any MCP client)
